using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.MediaEditorMigration.Cli;

// Converts Cropper/Uploader values that are stored INSIDE another editor's value — a Block List or Block Grid
// block, or a Perplex ContentBlocks block — rather than directly in umbracoPropertyData.
//
// The main pass rewrites only the umbracoPropertyData rows whose own data type is Cropper/Uploader, but it
// flips the DATA TYPE for every property bound to it, including element-type properties used inside blocks.
// A nested value left in the old N3O shape under a data type that now names a native editor kills the page,
// because that editor's value converter is handed the old object.
//
// After the 13->17 upgrade every block editor stores its element properties in the same shape as Perplex —
// {contentTypeKey, key, values:[{editorAlias, culture, segment, alias, value}]} — so one uniform rule covers
// Block List, Block Grid and Perplex, keyed off the stale editorAlias, with no per-editor SQL.
public sealed class NestedMediaMigrator {
    private const string CropperAlias = "N3O.Umbraco.Cropper";
    private const string UploaderAlias = "N3O.Umbraco.Uploader";
    private const string InlineCropperAlias = "Umbraco.ImageCropper";
    private const string InlineUploaderAlias = "Umbraco.UploadField";
    private const string MediaPickerAlias = "Umbraco.MediaPicker3";

    private readonly SqlConnection _cn;
    private readonly SqlTransaction _tx;
    private readonly MediaNodeFactory _factory;
    private readonly bool _verbose;
    private readonly bool _includeCropper;
    private readonly bool _includeUploader;
    private readonly IReadOnlyDictionary<(Guid ContentTypeKey, string Alias), NestedMediaTarget> _targets;

    // factory is null under --target inline, which creates no media nodes.
    public NestedMediaMigrator(SqlConnection cn,
                               SqlTransaction tx,
                               MediaNodeFactory factory,
                               bool verbose,
                               bool includeCropper,
                               bool includeUploader,
                               IReadOnlyDictionary<(Guid, string), NestedMediaTarget> targets) {
        _cn = cn;
        _tx = tx;
        _factory = factory;
        _verbose = verbose;
        _includeCropper = includeCropper;
        _includeUploader = includeUploader;
        _targets = targets;
    }

    // Failures go on totals.ValuesFailed rather than a return value: the caller decides whether to abort once
    // every pass has run.
    public void Run(RunTotals totals) {
        // Target exactly the rows that still mention a retired editor anywhere in their JSON. That is the
        // stale nested editorAlias, so it finds Block List, Block Grid and Perplex values (and values nested
        // inside those) without enumerating container editors.
        var rows = Db.Query(_cn,
                            _tx,
                            "SELECT pd.id, pd.textValue, pt.Alias, cv.nodeId, n.text " +
                            "FROM umbracoPropertyData pd " +
                            "INNER JOIN cmsPropertyType pt ON pt.id = pd.propertyTypeId " +
                            "LEFT JOIN umbracoContentVersion cv ON cv.id = pd.versionId " +
                            "LEFT JOIN umbracoNode n ON n.id = cv.nodeId " +
                            "WHERE pd.textValue IS NOT NULL AND pd.textValue <> '' " +
                            "AND (CHARINDEX('contentTypeKey', pd.textValue) > 0 " +
                            "OR CHARINDEX(@cropper, pd.textValue) > 0 OR CHARINDEX(@uploader, pd.textValue) > 0)",
                            r => new NestedRow {
                                Id = r.GetInt32(0),
                                TextValue = r.GetString(1),
                                PropertyAlias = r.IsDBNull(2) ? null : r.GetString(2),
                                NodeId = r.IsDBNull(3) ? (int?) null : r.GetInt32(3),
                                NodeName = r.IsDBNull(4) ? null : r.GetString(4)
                            },
                            ("@cropper", CropperAlias),
                            ("@uploader", UploaderAlias));

        if (rows.Count == 0) {
            Log.Info("No block values found — nothing to migrate inside blocks.");

            return;
        }

        Log.Info($"Found {rows.Count} block value(s) to inspect for nested Cropper/Uploader data.");

        // Every row is attempted even after one fails, so a dry run reports everything, not just the first.
        foreach (var row in rows) {
            ConvertRow(row, totals);
        }
    }

    private void ConvertRow(NestedRow row, RunTotals totals) {
        var header = $"nested value id {row.Id} | node {row.NodeDescription} | property '{row.PropertyAlias}'";
        var issues = new List<string>();

        try {
            JToken parsed;

            try {
                parsed = JToken.Parse(row.TextValue);
            } catch {
                issues.Add("NOT MIGRATED — value mentions a retired editor but is not valid JSON; left untouched.");
                Log.Item(header, issues);

                return;
            }

            var context = new WalkContext(totals, issues);
            var result = Walk(parsed, null, context);

            if (context.Converted > 0 || context.AliasesFixed > 0) {
                Db.Execute(_cn,
                           _tx,
                           "UPDATE umbracoPropertyData SET textValue = @value WHERE id = @id",
                           ("@value", JsonConvert.SerializeObject(result)),
                           ("@id", row.Id));

                totals.NestedValuesConverted += context.Converted;
                totals.NestedAliasesFixed += context.AliasesFixed;

                Log.Verbose(_verbose,
                            $"Nested value {row.Id} (node {row.NodeDescription}, '{row.PropertyAlias}'): " +
                            $"{context.Converted} media value(s) converted, {context.AliasesFixed} alias(es) fixed.");
            }

            if (issues.Count > 0) {
                Log.Item(header, issues);
            }
        } catch (Exception ex) {
            issues.Add($"FAILED to convert — {ex.Message}");
            Log.Item(header, issues);
            totals.ValuesFailed++;
        }
    }

    // Returns the token to use in place of the one passed in. contentTypeKey is the nearest enclosing element
    // content-type key, needed to resolve a property's crop definitions.
    private JToken Walk(JToken token, Guid? contentTypeKey, WalkContext context) {
        if (token is JObject obj) {
            var elementKey = TryGetGuid(obj["contentTypeKey"]) ?? contentTypeKey;
            var editorAlias = obj["editorAlias"]?.Type == JTokenType.String ? (string) obj["editorAlias"] : null;

            // A block property entry for a retired editor: convert it and stop — its value must not be walked
            // as if it were a container.
            // Only convert editors that are in --editor scope: an out-of-scope data type is left as
            // Cropper/Uploader, so rewriting its nested values would break them.
            if (obj["alias"] != null
                && ((editorAlias == CropperAlias && _includeCropper)
                    || (editorAlias == UploaderAlias && _includeUploader))) {
                ConvertEntry(obj, elementKey, editorAlias == CropperAlias, context);

                return obj;
            }

            // Legacy (Umbraco 13 udi) block entry: {contentTypeKey, udi, <alias>: <value>, ...} with no
            // values[] array and no editorAlias, so the property's editor can only be resolved through the
            // captured (element content-type key, alias) map. This shape survives inside a Perplex value
            // because Umbraco's 13->17 upgrade does not traverse another editor's value.
            var isLegacyEntry = obj["contentTypeKey"] != null && obj["values"] is not JArray;

            foreach (var property in obj.Properties().ToList()) {
                if (isLegacyEntry
                    && elementKey.HasValue
                    && _targets.TryGetValue((elementKey.Value, property.Name), out var legacyTarget)
                    && IsInScope(legacyTarget.IsCropper)) {
                    ConvertLegacyProperty(obj, property, elementKey.Value, legacyTarget, context);

                    continue;
                }

                property.Value = Walk(property.Value, elementKey, context);
            }

            return obj;
        }

        if (token is JArray array) {
            for (var i = 0; i < array.Count; i++) {
                array[i] = Walk(array[i], contentTypeKey, context);
            }

            return array;
        }

        // A complex editor's value nested inside another is stored as a serialized JSON string, so descend
        // into strings too and re-serialize only if something below actually changed.
        if (token is JValue { Type: JTokenType.String } value && value.Value is string text) {
            var trimmed = text.TrimStart();

            if (trimmed.Length == 0 || (trimmed[0] != '{' && trimmed[0] != '[')) {
                return token;
            }

            JToken inner;

            try {
                inner = JToken.Parse(text);
            } catch {
                return token;
            }

            var before = context.Converted + context.AliasesFixed;
            var walked = Walk(inner, contentTypeKey, context);

            if (context.Converted + context.AliasesFixed == before) {
                return token;
            }

            return JsonConvert.SerializeObject(walked);
        }

        return token;
    }

    private bool IsInScope(bool isCropper) {
        return isCropper ? _includeCropper : _includeUploader;
    }

    // Converts a legacy alias-keyed block property in place. Same conversion as the values-array shape, but
    // the property name is the alias and there is no editorAlias to correct.
    private void ConvertLegacyProperty(JObject entry,
                                       JProperty property,
                                       Guid contentTypeKey,
                                       NestedMediaTarget target,
                                       WalkContext context) {
        var shim = new JObject {
            ["alias"] = property.Name,
            ["value"] = property.Value.DeepClone()
        };

        var before = context.Converted;

        ConvertEntry(shim, contentTypeKey, target.IsCropper, context, setEditorAlias: false);

        if (context.Converted > before) {
            property.Value = shim["value"];
        }
    }

    private void ConvertEntry(JObject entry,
                              Guid? contentTypeKey,
                              bool isCropper,
                              WalkContext context,
                              bool setEditorAlias = true) {
        var alias = (string) entry["alias"];
        var raw = entry["value"];
        var nativeAlias = _factory != null
                              ? MediaPickerAlias
                              : isCropper ? InlineCropperAlias : InlineUploaderAlias;

        // No value stored: there is nothing to convert, but the editorAlias must still stop naming a retired
        // editor or the native value editor is handed the wrong shape the moment a value is added.
        if (raw == null || raw.Type == JTokenType.Null) {
            if (setEditorAlias) {
                entry["editorAlias"] = nativeAlias;
                context.AliasesFixed++;
            }

            return;
        }

        var json = raw.Type == JTokenType.String ? (string) raw : raw.ToString(Formatting.None);

        var file = isCropper ? SourceParsers.ParseCropper(json) : SourceParsers.ParseUploader(json);

        // Not a shape this tool can rebuild, so the value is left for a human — but the editorAlias is still
        // corrected for the same reason as the no-value branch above. Counted as unchanged, not failed: one
        // unreadable value should not roll back the whole migration.
        if (file == null) {
            context.Totals.ValuesUnchanged++;
            context.Issues.Add($"'{alias}': unrecognised {(isCropper ? "Cropper" : "Uploader")} value, left " +
                               $"untouched — convert by hand (editorAlias still corrected to '{nativeAlias}')");

            if (setEditorAlias) {
                entry["editorAlias"] = nativeAlias;
                context.AliasesFixed++;
            }

            return;
        }

        var cropDefinitions = new List<CropDefinition>();

        if (contentTypeKey.HasValue && _targets.TryGetValue((contentTypeKey.Value, alias), out var target)) {
            cropDefinitions = target.CropDefinitions;
        } else if (isCropper) {
            context.Issues.Add($"'{alias}': no data type match (element key missing or property removed), so " +
                               "its crops were dropped");
        }

        CropOutcome crops;

        if (_factory != null) {
            var mediaKey = _factory.GetOrCreate(file);
            var (nativeJson, outcome) = NativeValueBuilder.BuildPickerValue(mediaKey, file, cropDefinitions);

            // A nested complex editor value is stored as a serialized JSON string, matching how Umbraco writes
            // every other nested editor value inside a block.
            entry["value"] = nativeJson;
            crops = outcome;
        } else if (isCropper) {
            var (nativeJson, outcome) = NativeValueBuilder.BuildImageCropperValue(file, cropDefinitions);

            entry["value"] = nativeJson;
            crops = outcome;
        } else {
            // Umbraco.UploadField stores the file path as a plain string, not as JSON.
            entry["value"] = file.Src;
            crops = new CropOutcome();
        }

        if (setEditorAlias) {
            entry["editorAlias"] = nativeAlias;
        }

        context.Converted++;

        if (crops.WithoutCoordinates.Count > 0) {
            context.Totals.CropsWithoutCoordinates += crops.WithoutCoordinates.Count;
            context.Issues.Add($"'{alias}': no coordinates for crop(s) " +
                               $"{string.Join(", ", crops.WithoutCoordinates)} (source dimensions missing)");
        }

        if (crops.DroppedRectangles > 0) {
            context.Totals.CropRectanglesDropped += crops.DroppedRectangles;
            context.Issues.Add($"'{alias}': {crops.DroppedRectangles} crop rectangle(s) DROPPED — more " +
                               "rectangles stored than the data type defines crops for");
        }

        // Carried-over alt text is counted only — see the equivalent note in Migrator.ConvertValue.
        if (!string.IsNullOrWhiteSpace(file.AltText)) {
            if (_factory == null && !isCropper) {
                context.Totals.AltTextDropped++;
                context.Issues.Add($"'{alias}': alt text '{file.AltText}' DROPPED — Umbraco.UploadField stores " +
                                   "a bare path");
            } else {
                context.Totals.AltTextPreserved++;
            }
        }
    }

    private static Guid? TryGetGuid(JToken token) {
        if (token?.Type == JTokenType.String && Guid.TryParse((string) token, out var guid)) {
            return guid;
        }

        return null;
    }

    private sealed class WalkContext {
        public WalkContext(RunTotals totals, List<string> issues) {
            Totals = totals;
            Issues = issues;
        }

        public RunTotals Totals { get; }
        public List<string> Issues { get; }
        public int Converted { get; set; }
        public int AliasesFixed { get; set; }
    }

    private sealed class NestedRow {
        public int Id { get; set; }
        public string TextValue { get; set; }
        public string PropertyAlias { get; set; }
        public int? NodeId { get; set; }
        public string NodeName { get; set; }

        public string NodeDescription => NodeId.HasValue ? $"{NodeId} \"{NodeName}\"" : "(unknown)";
    }
}
