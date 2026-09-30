using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.MediaEditorMigration.Cli;

// Rewrites Block List / Block Grid values still in the Umbraco 13 "udi" shape into the v14+ key-based shape.
//
// Umbraco's own 13->17 upgrade never traverses INTO another editor's value, so a Block List nested inside a
// Perplex ContentBlocks value keeps the legacy shape permanently: alias-keyed properties, lower-case "layout",
// "udi" instead of "key".
//
// Runs last, after the media passes: each entry needs an "editorAlias" read from the LIVE umbracoDataType rows,
// so it picks up the native aliases those passes just wrote. The capitalised "Layout", the retained contentUdi
// and the "expose" array are what Umbraco itself produces, reproduced exactly.
//
// Values and layout items are cloned then adjusted, never rebuilt from a fixed key list, so members not modelled
// here survive — notably a Block GRID item's "columnSpan"/"rowSpan" and its "areas" tree.
public sealed class NestedBlockShapeNormalizer {
    private static readonly HashSet<string> EntryReservedKeys =
        new(StringComparer.OrdinalIgnoreCase) { "contentTypeKey", "udi", "key", "values" };

    private readonly SqlConnection _cn;
    private readonly SqlTransaction _tx;
    private readonly bool _verbose;
    private readonly Dictionary<(Guid, string), string> _editorAliases = new();

    public NestedBlockShapeNormalizer(SqlConnection cn, SqlTransaction tx, bool verbose) {
        _cn = cn;
        _tx = tx;
        _verbose = verbose;
    }

    // Failures go on totals.ValuesFailed rather than a return value: the caller decides whether to abort once
    // every pass has run.
    public void Run(RunTotals totals) {
        LoadEditorAliases();

        var rows = Db.Query(_cn,
                            _tx,
                            "SELECT pd.id, pd.textValue, pt.Alias, cv.nodeId, n.text " +
                            "FROM umbracoPropertyData pd " +
                            "INNER JOIN cmsPropertyType pt ON pt.id = pd.propertyTypeId " +
                            "LEFT JOIN umbracoContentVersion cv ON cv.id = pd.versionId " +
                            "LEFT JOIN umbracoNode n ON n.id = cv.nodeId " +
                            "WHERE pd.textValue IS NOT NULL AND pd.textValue <> '' " +
                            "AND CHARINDEX('contentData', pd.textValue) > 0",
                            r => new ShapeRow {
                                Id = r.GetInt32(0),
                                TextValue = r.GetString(1),
                                PropertyAlias = r.IsDBNull(2) ? null : r.GetString(2),
                                NodeId = r.IsDBNull(3) ? (int?) null : r.GetInt32(3),
                                NodeName = r.IsDBNull(4) ? null : r.GetString(4)
                            });

        if (rows.Count == 0) {
            Log.Info("No block values found — nothing to normalise.");

            return;
        }

        // Every row is attempted even after one fails, so a dry run reports everything, not just the first.
        foreach (var row in rows) {
            NormalizeRow(row, totals);
        }
    }

    // (element content-type key, property alias) -> the property's CURRENT editor alias, resolved ACROSS
    // COMPOSITIONS. Element types used in blocks compose heavily (a block item's "linkText" usually comes from a
    // shared "Link" type), and reading cmsPropertyType alone made every inherited property unresolvable, writing
    // "editorAlias": null onto real values so Umbraco stopped rendering them — 6,926 values on one site.
    //
    // nc-migrate has its own copy of this walk (BuildEditorAliases). Deliberate: each tool is a standalone exe
    // with no reference to the other or to any N3O package, so sharing would mean a common library. Keep in step.
    private void LoadEditorAliases() {
        var propertyRows = Db.Query(_cn,
                                    _tx,
                                    "SELECT pt.contentTypeId, pt.Alias, dt.propertyEditorAlias " +
                                    "FROM cmsPropertyType pt " +
                                    "INNER JOIN umbracoDataType dt ON dt.nodeId = pt.dataTypeId",
                                    r => (ContentTypeId: r.GetInt32(0),
                                          Alias: r.GetString(1),
                                          Editor: r.IsDBNull(2) ? null : r.GetString(2)));

        var ownProperties = new Dictionary<int, Dictionary<string, string>>();

        foreach (var row in propertyRows) {
            if (!ownProperties.TryGetValue(row.ContentTypeId, out var map)) {
                map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                ownProperties[row.ContentTypeId] = map;
            }

            map[row.Alias] = row.Editor;
        }

        var parents = new Dictionary<int, List<int>>();

        var compositionRows = Db.Query(_cn,
                                       _tx,
                                       "SELECT parentContentTypeId, childContentTypeId FROM cmsContentType2ContentType",
                                       r => (Parent: r.GetInt32(0), Child: r.GetInt32(1)));

        foreach (var row in compositionRows) {
            if (!parents.TryGetValue(row.Child, out var list)) {
                list = new List<int>();
                parents[row.Child] = list;
            }

            list.Add(row.Parent);
        }

        var keysByContentTypeId = Db.Query(_cn,
                                           _tx,
                                           "SELECT ct.nodeId, n.uniqueId FROM cmsContentType ct " +
                                           "INNER JOIN umbracoNode n ON n.id = ct.nodeId",
                                           r => (ContentTypeId: r.GetInt32(0), Key: r.GetGuid(1)));

        var effective = new Dictionary<int, Dictionary<string, string>>();

        foreach (var row in keysByContentTypeId) {
            var resolved = ResolveProperties(row.ContentTypeId, ownProperties, parents, effective, new HashSet<int>());

            foreach (var property in resolved) {
                _editorAliases[(row.Key, property.Key)] = property.Value;
            }
        }

        Log.Verbose(_verbose, $"Loaded {_editorAliases.Count} property editor alias(es) for block normalisation " +
                              $"(across {effective.Count} content type(s), compositions resolved).");
    }

    // Inherited properties first, then own, so a locally declared alias wins over an inherited one.
    private static Dictionary<string, string> ResolveProperties(
            int contentTypeId,
            IReadOnlyDictionary<int, Dictionary<string, string>> ownProperties,
            IReadOnlyDictionary<int, List<int>> parents,
            Dictionary<int, Dictionary<string, string>> cache,
            HashSet<int> visiting) {
        if (cache.TryGetValue(contentTypeId, out var cached)) {
            return cached;
        }

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (parents.TryGetValue(contentTypeId, out var parentIds)) {
            foreach (var parentId in parentIds) {
                // visiting guards against a composition cycle, which Umbraco does not allow but which a
                // hand-edited database could still contain — without it this would recurse until the stack blew.
                if (!visiting.Add(parentId)) {
                    continue;
                }

                foreach (var property in ResolveProperties(parentId, ownProperties, parents, cache, visiting)) {
                    map[property.Key] = property.Value;
                }

                visiting.Remove(parentId);
            }
        }

        if (ownProperties.TryGetValue(contentTypeId, out var own)) {
            foreach (var property in own) {
                map[property.Key] = property.Value;
            }
        }

        cache[contentTypeId] = map;

        return map;
    }

    private void NormalizeRow(ShapeRow row, RunTotals totals) {
        var header = $"block value id {row.Id} | node {row.NodeDescription} | property '{row.PropertyAlias}'";
        var context = new WalkContext();

        try {
            JToken parsed;

            try {
                parsed = JToken.Parse(row.TextValue);
            } catch {
                Log.Item(header,
                         new List<string> {
                             "NOT NORMALISED — value contains \"contentData\" but is not valid JSON; left untouched."
                         });

                return;
            }

            var result = Walk(parsed, context);

            if (context.Converted > 0) {
                Db.Execute(_cn,
                           _tx,
                           "UPDATE umbracoPropertyData SET textValue = @value WHERE id = @id",
                           ("@value", JsonConvert.SerializeObject(result)),
                           ("@id", row.Id));

                totals.LegacyBlockShapesNormalized += context.Converted;

                Log.Verbose(_verbose,
                            $"Block value {row.Id} (node {row.NodeDescription}, '{row.PropertyAlias}'): " +
                            $"{context.Converted} legacy block value(s) normalised.");
            }

            if (context.Issues.Count > 0) {
                Log.Item(header, context.Issues);
            }
        } catch (Exception ex) {
            Log.Item(header, new List<string> { $"FAILED to normalise — {ex.Message}" });
            totals.ValuesFailed++;
        }
    }

    private JToken Walk(JToken token, WalkContext context) {
        if (token is JObject obj) {
            if (IsLegacyBlockValue(obj)) {
                context.Converted++;

                return Normalize(obj, context);
            }

            foreach (var property in obj.Properties().ToList()) {
                property.Value = Walk(property.Value, context);
            }

            return obj;
        }

        if (token is JArray array) {
            for (var i = 0; i < array.Count; i++) {
                array[i] = Walk(array[i], context);
            }

            return array;
        }

        if (token is JValue { Type: JTokenType.String } value && value.Value is string text) {
            var trimmed = text.TrimStart();

            if (trimmed.Length == 0 || trimmed[0] != '{' || !text.Contains("contentData")) {
                return token;
            }

            JToken inner;

            try {
                inner = JToken.Parse(text);
            } catch {
                return token;
            }

            var before = context.Converted;
            var walked = Walk(inner, context);

            return context.Converted == before ? token : JsonConvert.SerializeObject(walked);
        }

        return token;
    }

    // Legacy means: a block value whose contentData entries carry "udi" and no "values" array. A value already
    // upgraded by Umbraco has "key" + "values", so it is left untouched.
    private static bool IsLegacyBlockValue(JObject obj) {
        if (obj["contentData"] is not JArray contentData) {
            return false;
        }

        return contentData.OfType<JObject>().Any(x => x["udi"] != null && x["values"] is not JArray);
    }

    private JObject Normalize(JObject legacy, WalkContext context) {
        var contentKeys = new List<Guid>();

        var contentData = NormalizeEntries(legacy["contentData"] as JArray, contentKeys, context);
        var settingsData = NormalizeEntries(legacy["settingsData"] as JArray, new List<Guid>(), context);

        // "expose" lists the CONTENT entries a variant surfaces (BlockValue.Expose is a list of
        // BlockItemVariation keyed by ContentKey), so settings keys have no place in it.
        var expose = new JArray();

        foreach (var key in contentKeys) {
            expose.Add(new JObject {
                ["contentKey"] = key.ToString(),
                ["culture"] = JValue.CreateNull(),
                ["segment"] = JValue.CreateNull()
            });
        }

        // Start from the original so any member this normaliser does not know about survives, then overwrite
        // the four it rewrites. The legacy shape's lower-case "layout" is replaced by the capitalised "Layout"
        // Umbraco writes, so the old key is removed rather than left behind as a stale duplicate.
        var result = (JObject) legacy.DeepClone();

        result.Remove("layout");

        result["contentData"] = contentData;
        result["settingsData"] = settingsData;
        result["expose"] = expose;
        result["Layout"] = NormalizeLayout(legacy["layout"] ?? legacy["Layout"]);

        return result;
    }

    private JArray NormalizeEntries(JArray entries, List<Guid> keys, WalkContext context) {
        var output = new JArray();

        if (entries == null) {
            return output;
        }

        foreach (var entry in entries.OfType<JObject>()) {
            var key = GuidFromUdi(entry["udi"]) ?? TryGuid(entry["key"]) ?? Guid.NewGuid();
            keys.Add(key);

            var contentTypeKey = TryGuid(entry["contentTypeKey"]);
            var values = new JArray();

            // Already-upgraded entries can appear alongside legacy ones; carry their values across untouched.
            if (entry["values"] is JArray existing) {
                foreach (var item in existing) {
                    values.Add(Walk(item, context));
                }
            }

            foreach (var property in entry.Properties()) {
                if (EntryReservedKeys.Contains(property.Name)) {
                    continue;
                }

                string editorAlias = null;

                if (contentTypeKey.HasValue
                    && _editorAliases.TryGetValue((contentTypeKey.Value, property.Name), out var resolved)) {
                    editorAlias = resolved;
                }

                // Start from a clone so the walk below cannot mutate the entry being read.
                var propertyValue = Walk(property.Value.DeepClone(), context);

                // Umbraco resolves a block property's value editor from its editorAlias, so an unresolvable one
                // is not a cosmetic gap — that property stops rendering. With compositions resolved (see
                // LoadEditorAliases) the only aliases left unresolved are genuinely gone from the element type,
                // i.e. a property deleted after the value was saved.
                //
                // Only reported when the property actually HOLDS a value. A deleted property almost always
                // leaves a null behind on every block that ever had it, and there is nothing for anyone to do
                // about an inert null — reporting those buried the real ones 3:1 on one production site.
                if (editorAlias == null && propertyValue != null && propertyValue.Type != JTokenType.Null) {
                    context.Issues.Add($"'{property.Name}': has a value but no such property on element type " +
                                       $"{contentTypeKey?.ToString() ?? "unknown"} (compositions included), so " +
                                       "editorAlias is null and Umbraco will not render it");
                }

                values.Add(new JObject {
                    ["editorAlias"] = editorAlias == null ? JValue.CreateNull() : new JValue(editorAlias),
                    ["culture"] = JValue.CreateNull(),
                    ["segment"] = JValue.CreateNull(),
                    ["alias"] = property.Name,
                    ["value"] = propertyValue
                });
            }

            output.Add(new JObject {
                ["contentTypeKey"] = contentTypeKey?.ToString() ?? (string) entry["contentTypeKey"],
                ["key"] = key.ToString(),
                ["values"] = values
            });
        }

        return output;
    }

    // Keeps contentUdi (Umbraco's own upgrade does) and adds the key-based fields alongside it.
    //
    // Each item is CLONED and then adjusted rather than rebuilt from a fixed key list: a Block GRID layout item
    // also carries "columnSpan", "rowSpan" and an "areas" array that holds a whole nested item tree, and
    // rebuilding would silently discard all of it. Nested area items are the same shape, so they are recursed
    // into and get the same key-based fields.
    private static JObject NormalizeLayout(JToken layout) {
        var output = new JObject();

        if (layout is not JObject layoutObj) {
            return output;
        }

        foreach (var editor in layoutObj.Properties()) {
            output[editor.Name] = NormalizeLayoutItems(editor.Value);
        }

        return output;
    }

    private static JArray NormalizeLayoutItems(JToken items) {
        var output = new JArray();

        if (items is not JArray entries) {
            return output;
        }

        foreach (var item in entries.OfType<JObject>()) {
            var contentKey = GuidFromUdi(item["contentUdi"]) ?? TryGuid(item["contentKey"]);
            var settingsKey = GuidFromUdi(item["settingsUdi"]) ?? TryGuid(item["settingsKey"]);

            var normalized = (JObject) item.DeepClone();

            normalized["contentUdi"] = item["contentUdi"]?.DeepClone()
                                       ?? (contentKey.HasValue
                                           ? new JValue($"umb://element/{contentKey.Value:N}")
                                           : JValue.CreateNull());
            normalized["settingsUdi"] = item["settingsUdi"]?.DeepClone() ?? JValue.CreateNull();
            normalized["contentKey"] = contentKey.HasValue
                                          ? new JValue(contentKey.Value.ToString())
                                          : JValue.CreateNull();
            normalized["settingsKey"] = settingsKey.HasValue
                                            ? new JValue(settingsKey.Value.ToString())
                                            : JValue.CreateNull();

            // Block Grid: each area holds its own items, in the same layout-item shape.
            if (item["areas"] is JArray areas) {
                var normalizedAreas = new JArray();

                foreach (var area in areas.OfType<JObject>()) {
                    var normalizedArea = (JObject) area.DeepClone();

                    normalizedArea["items"] = NormalizeLayoutItems(area["items"]);
                    normalizedAreas.Add(normalizedArea);
                }

                normalized["areas"] = normalizedAreas;
            }

            output.Add(normalized);
        }

        return output;
    }

    private static Guid? GuidFromUdi(JToken token) {
        if (token?.Type != JTokenType.String) {
            return null;
        }

        var text = (string) token;
        var separator = text.LastIndexOf('/');

        if (separator < 0 || separator == text.Length - 1) {
            return null;
        }

        return Guid.TryParse(text.Substring(separator + 1), out var guid) ? guid : null;
    }

    private static Guid? TryGuid(JToken token) {
        if (token?.Type == JTokenType.String && Guid.TryParse((string) token, out var guid)) {
            return guid;
        }

        return null;
    }

    private sealed class WalkContext {
        public int Converted { get; set; }

        // Anything about this value that needs a human, reported as one [REVIEW] block per property value.
        public List<string> Issues { get; } = new();
    }

    private sealed class ShapeRow {
        public int Id { get; set; }
        public string TextValue { get; set; }
        public string PropertyAlias { get; set; }
        public int? NodeId { get; set; }
        public string NodeName { get; set; }

        public string NodeDescription => NodeId.HasValue ? $"{NodeId} \"{NodeName}\"" : "(unknown)";
    }
}
