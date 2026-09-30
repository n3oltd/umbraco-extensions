using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;

namespace N3O.Umbraco.MediaEditorMigration.Cli;

public sealed class Migrator {
    private const string CropperAlias = "N3O.Umbraco.Cropper";
    private const string UploaderAlias = "N3O.Umbraco.Uploader";

    private const string InlineCropperAlias = "Umbraco.ImageCropper";
    private const string InlineCropperUiAlias = "Umb.PropertyEditorUi.ImageCropper";
    private const string InlineUploaderAlias = "Umbraco.UploadField";
    private const string InlineUploaderUiAlias = "Umb.PropertyEditorUi.UploadField";
    private const string MediaPickerAlias = "Umbraco.MediaPicker3";
    private const string MediaPickerUiAlias = "Umb.PropertyEditorUi.MediaPicker";

    private const string CacheSerializerKey = "Umbraco.Web.PublishedCache.NuCache.Serializer";

    private readonly CliOptions _options;

    public Migrator(CliOptions options) {
        _options = options;
    }

    private bool Inline => _options.Target == MigrationTarget.Inline;

    public bool Run() {
        using var connection = new SqlConnection(_options.ConnectionString);
        connection.Open();

        Log.Info($"Connected to '{connection.Database}' on '{connection.DataSource}'.");
        Log.Info(_options.DryRun ? "Mode: DRY RUN (no changes will be committed)." : "Mode: APPLY (changes WILL be committed).");
        Log.Info(Inline
                     ? $"Editor scope: {_options.Editor}. Target: inline — Cropper → {InlineCropperAlias}, " +
                       $"Uploader → {InlineUploaderAlias}. No media nodes are created."
                     : $"Editor scope: {_options.Editor}. Target: mediapicker — both → {MediaPickerAlias}. " +
                       $"New media nodes go under parent node id {_options.MediaParentId}.");

        using var transaction = connection.BeginTransaction();

        if (!Db.ColumnExists(connection, transaction, "umbracoDataType", "propertyEditorUiAlias")
            || (!Inline && !TableExists(connection, transaction, "umbracoMediaVersion"))) {
            Log.Error("This database is not on the Umbraco 14+/17 schema (umbracoDataType.propertyEditorUiAlias " +
                      "or umbracoMediaVersion missing). This tool only migrates Umbraco 17 databases.");
            transaction.Rollback();

            return false;
        }

        MediaNodeFactory factory = null;

        if (!Inline) {
            try {
                var mediaTypes = MediaTypes.Resolve(connection, transaction);

                factory = new MediaNodeFactory(connection,
                                               transaction,
                                               mediaTypes,
                                               _options.MediaParentId,
                                               _options.Verbose);
            } catch (Exception ex) {
                Log.Error(ex.Message);
                transaction.Rollback();

                return false;
            }
        }

        var totals = new RunTotals();

        var nestedTargets = BuildNestedTargets(connection, transaction);

        if (_options.Editor is EditorScope.Both or EditorScope.Cropper) {
            MigrateEditor(connection, transaction, factory, totals, CropperAlias, isCropper: true);
        }

        if (_options.Editor is EditorScope.Both or EditorScope.Uploader) {
            MigrateEditor(connection, transaction, factory, totals, UploaderAlias, isCropper: false);
        }

        var nested = new NestedMediaMigrator(connection,
                                             transaction,
                                             factory,
                                             _options.Verbose,
                                             _options.Editor is EditorScope.Both or EditorScope.Cropper,
                                             _options.Editor is EditorScope.Both or EditorScope.Uploader,
                                             nestedTargets);

        nested.Run(totals);

        new NestedBlockShapeNormalizer(connection, transaction, _options.Verbose).Run(totals);

        totals.MediaNodesCreated = factory?.Created ?? 0;

        totals.PublishedCacheInvalidated = InvalidatePublishedCache(connection, transaction) > 0;

        Report(totals);

        if (totals.ValuesFailed > 0) {
            Log.Error($"{totals.ValuesFailed} value(s) failed to convert — see the [REVIEW] entries above.");
            transaction.Rollback();
            Log.Error("Migration aborted — all changes rolled back.");

            return false;
        }

        if (_options.DryRun) {
            transaction.Rollback();
            Log.Success("DRY RUN complete — all changes rolled back. Re-run with --apply to commit.");
        } else {
            transaction.Commit();
            Log.Success("Migration committed. The published cache will rebuild itself on the next site start " +
                        "(the cache-serializer marker was cleared). Delete the on-disk NuCache.*.db first, and " +
                        "rebuild the Examine indexes afterwards.");
        }

        return true;
    }

    private Dictionary<(Guid, string), NestedMediaTarget> BuildNestedTargets(SqlConnection cn, SqlTransaction tx) {
        var rows = Db.Query(cn,
                            tx,
                            "SELECT LOWER(CONVERT(NVARCHAR(50), n.uniqueId)), pt.Alias, dt.propertyEditorAlias, " +
                            "dt.[config], dt.nodeId " +
                            "FROM cmsPropertyType pt " +
                            "INNER JOIN cmsContentType ct ON ct.nodeId = pt.contentTypeId " +
                            "INNER JOIN umbracoNode n ON n.id = ct.nodeId " +
                            "INNER JOIN umbracoDataType dt ON dt.nodeId = pt.dataTypeId " +
                            "WHERE dt.propertyEditorAlias IN (@cropper, @uploader)",
                            r => new {
                                ContentTypeKey = r.GetString(0),
                                Alias = r.GetString(1),
                                Editor = r.GetString(2),
                                Config = r.IsDBNull(3) ? null : r.GetString(3),
                                DataTypeId = r.GetInt32(4)
                            },
                            ("@cropper", CropperAlias),
                            ("@uploader", UploaderAlias));

        var targets = new Dictionary<(Guid, string), NestedMediaTarget>();

        foreach (var row in rows) {
            if (!Guid.TryParse(row.ContentTypeKey, out var contentTypeKey)) {
                continue;
            }

            var isCropper = row.Editor == CropperAlias;

            targets[(contentTypeKey, row.Alias)] = new NestedMediaTarget {
                IsCropper = isCropper,
                CropDefinitions = isCropper
                    ? ParseCropDefinitions(row.Config, row.DataTypeId)
                    : new List<CropDefinition>()
            };
        }

        Log.Verbose(_options.Verbose, $"Captured {targets.Count} element propertie(s) bound to a retired media editor.");

        return targets;
    }

    private int InvalidatePublishedCache(SqlConnection cn, SqlTransaction tx) {
        var rows = Db.Execute(cn,
                              tx,
                              "DELETE FROM umbracoKeyValue WHERE [key] = @key",
                              ("@key", CacheSerializerKey));

        if (rows > 0) {
            Log.Info(_options.DryRun
                         ? "WOULD clear the published-cache serializer marker, making Umbraco rebuild the cache " +
                           "on next start (rolled back with the rest of this dry run)."
                         : "Cleared the published-cache serializer marker; Umbraco will rebuild the cache on " +
                           "next start.");
        } else {
            Log.Warn($"No '{CacheSerializerKey}' row found, so the published cache was NOT invalidated. Rebuild " +
                     "the database cache manually or the site will keep serving pre-migration values.");
        }

        return rows;
    }

    private void MigrateEditor(SqlConnection cn, SqlTransaction tx, MediaNodeFactory factory, RunTotals totals,
                               string editorAlias, bool isCropper) {
        var dataTypes = Db.Query(cn, tx,
            "SELECT nodeId, [config] FROM umbracoDataType WHERE propertyEditorAlias = @alias",
            r => new DataTypeRow { Id = r.GetInt32(0), Config = r.IsDBNull(1) ? null : r.GetString(1) },
            ("@alias", editorAlias));

        if (dataTypes.Count == 0) {
            Log.Info($"No '{editorAlias}' data types found — nothing to migrate for this editor.");

            return;
        }

        Log.Info($"Found {dataTypes.Count} '{editorAlias}' data type(s).");

        foreach (var dataType in dataTypes) {
            MigrateDataType(cn, tx, factory, totals, dataType, isCropper);
        }
    }

    private void MigrateDataType(SqlConnection cn, SqlTransaction tx, MediaNodeFactory factory, RunTotals totals,
                                 DataTypeRow dataType, bool isCropper) {
        var cropDefinitions = isCropper ? ParseCropDefinitions(dataType.Config, dataType.Id) : new List<CropDefinition>();

        var propertyTypeIds = Db.Query(cn, tx,
            "SELECT id FROM cmsPropertyType WHERE dataTypeId = @dataTypeId",
            r => r.GetInt32(0),
            ("@dataTypeId", dataType.Id)).Cast<object>().ToList();

        if (propertyTypeIds.Count > 0) {
            var values = Db.QueryIn(cn, tx,
                "SELECT pd.id, pd.textValue, pt.Alias, cv.nodeId, n.text " +
                "FROM umbracoPropertyData pd " +
                "INNER JOIN cmsPropertyType pt ON pt.id = pd.propertyTypeId " +
                "LEFT JOIN umbracoContentVersion cv ON cv.id = pd.versionId " +
                "LEFT JOIN umbracoNode n ON n.id = cv.nodeId " +
                "WHERE pd.propertyTypeId IN ({0}) AND pd.textValue IS NOT NULL AND pd.textValue <> ''",
                "p",
                propertyTypeIds,
                r => new PropertyDataRow {
                    Id = r.GetInt32(0),
                    TextValue = r.GetString(1),
                    PropertyAlias = r.IsDBNull(2) ? null : r.GetString(2),
                    NodeId = r.IsDBNull(3) ? (int?) null : r.GetInt32(3),
                    NodeName = r.IsDBNull(4) ? null : r.GetString(4)
                });

            foreach (var value in values) {
                ConvertValue(cn, tx, factory, totals, value, cropDefinitions, isCropper);
            }
        }

        string editor;
        string ui;
        string config;

        if (!Inline) {
            editor = MediaPickerAlias;
            ui = MediaPickerUiAlias;
            config = NativeValueBuilder.BuildMediaPickerConfig(cropDefinitions, enableLocalFocalPoint: isCropper);
        } else if (isCropper) {
            editor = InlineCropperAlias;
            ui = InlineCropperUiAlias;
            config = NativeValueBuilder.BuildImageCropperConfig(cropDefinitions);
        } else {
            editor = InlineUploaderAlias;
            ui = InlineUploaderUiAlias;
            config = NativeValueBuilder.BuildUploadFieldConfig(ParseAllowedExtensions(dataType.Config, dataType.Id));
        }

        Db.Execute(cn, tx,
            "UPDATE umbracoDataType SET propertyEditorAlias = @editor, propertyEditorUiAlias = @ui, [config] = @config " +
            "WHERE nodeId = @nodeId",
            ("@editor", editor), ("@ui", ui), ("@config", config), ("@nodeId", dataType.Id));

        totals.DataTypesConverted++;
        Log.Verbose(_options.Verbose,
                    $"Data type {dataType.Id} → {editor} ({cropDefinitions.Count} crop definition(s)).");
    }

    private void ConvertValue(SqlConnection cn, SqlTransaction tx, MediaNodeFactory factory, RunTotals totals,
                              PropertyDataRow value, List<CropDefinition> cropDefinitions, bool isCropper) {
        var header = $"value id {value.Id} | node {value.NodeDescription} | property '{value.PropertyAlias}'";
        var issues = new List<string>();

        try {
            var file = isCropper ? SourceParsers.ParseCropper(value.TextValue) : SourceParsers.ParseUploader(value.TextValue);

            if (file == null) {
                totals.ValuesUnchanged++;
                issues.Add("NOT MIGRATED — not a recognised Cropper/Uploader value (already migrated, empty or " +
                           "an unexpected shape); left untouched");
                Log.Item(header, issues);

                return;
            }

            if (string.IsNullOrWhiteSpace(file.Src)) {
                totals.ValuesFailed++;
                issues.Add("FAILED — no file path (src/urlPath) in the stored value");
                Log.Item(header, issues);

                return;
            }

            string native;
            CropOutcome crops;

            if (!Inline) {
                var mediaKey = factory.GetOrCreate(file);
                (native, crops) = NativeValueBuilder.BuildPickerValue(mediaKey, file, cropDefinitions);
            } else if (isCropper) {
                (native, crops) = NativeValueBuilder.BuildImageCropperValue(file, cropDefinitions);
            } else {
                native = file.Src;
                crops = new CropOutcome();
            }

            Db.Execute(cn, tx, "UPDATE umbracoPropertyData SET textValue = @value WHERE id = @id",
                       ("@value", native), ("@id", value.Id));

            totals.ValuesConverted++;

            if (crops.WithoutCoordinates.Count > 0) {
                totals.CropsWithoutCoordinates += crops.WithoutCoordinates.Count;
                issues.Add($"no coordinates for crop(s) {string.Join(", ", crops.WithoutCoordinates)} " +
                           "(source image dimensions missing); falls back to " +
                           $"{(Inline ? "a centre crop" : "the focal point")}");
            }

            if (crops.DroppedRectangles > 0) {
                totals.CropRectanglesDropped += crops.DroppedRectangles;
                issues.Add($"{crops.DroppedRectangles} crop rectangle(s) DROPPED — more rectangles stored than " +
                           $"the data type defines crops for ({cropDefinitions.Count})");
            }

            if (file.AltText != null) {
                if (Inline && !isCropper) {
                    totals.AltTextDropped++;
                    issues.Add($"alt text '{file.AltText}' DROPPED — Umbraco.UploadField stores a bare path");
                } else {
                    totals.AltTextPreserved++;
                }
            }
        } catch (Exception ex) {
            totals.ValuesFailed++;
            issues.Add($"FAILED to convert — {ex.Message}");
            Log.Item(header, issues);

            return;
        }

        if (issues.Count > 0) {
            Log.Item(header, issues);
        }
    }

    private static List<CropDefinition> ParseCropDefinitions(string config, int dataTypeId) {
        var definitions = new List<CropDefinition>();

        if (string.IsNullOrWhiteSpace(config)) {
            return definitions;
        }

        try {
            var obj = JObject.Parse(config);

            if (obj["cropDefinitions"] is JArray crops) {
                foreach (var crop in crops.OfType<JObject>()) {
                    var alias = (string) crop["alias"];

                    if (string.IsNullOrWhiteSpace(alias)) {
                        continue;
                    }

                    definitions.Add(new CropDefinition {
                        Alias = alias,
                        Width = (int?) crop["width"] ?? 0,
                        Height = (int?) crop["height"] ?? 0
                    });
                }
            }
        } catch {
            Log.Warn($"Cropper data type {dataTypeId} has unparseable config — no crop definitions resolved; " +
                     "its values will carry no crops.");
        }

        return definitions;
    }

    private static string ParseAllowedExtensions(string config, int dataTypeId) {
        if (string.IsNullOrWhiteSpace(config)) {
            return null;
        }

        try {
            return (string) JObject.Parse(config)["allowedExtensions"];
        } catch {
            Log.Warn($"Uploader data type {dataTypeId} has unparseable config — its allowed file extensions " +
                     "could not be carried over, so the native upload field will accept any file type.");

            return null;
        }
    }

    private static bool TableExists(SqlConnection cn, SqlTransaction tx, string table) {
        return Db.Scalar<int>(cn, tx,
                              "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @t",
                              ("@t", table)) > 0;
    }

    private void Report(RunTotals totals) {
        Log.Info($"Data types  : {totals.DataTypesConverted} converted");
        Log.Info($"Values      : {totals.ValuesConverted} converted, {totals.ValuesUnchanged} unchanged, " +
                 $"{totals.ValuesFailed} failed");
        Log.Info($"In blocks   : {totals.NestedValuesConverted} converted, {totals.NestedAliasesFixed} " +
                 $"alias(es) fixed, {totals.LegacyBlockShapesNormalized} legacy shape(s) normalised");

        if (!Inline) {
            Log.Info($"Media nodes : {totals.MediaNodesCreated} created");
        }

        Log.Info($"Alt text    : {totals.AltTextPreserved} kept" +
                 (totals.AltTextDropped > 0 ? $", {totals.AltTextDropped} DROPPED" : ""));

        if (totals.CropsWithoutCoordinates > 0 || totals.CropRectanglesDropped > 0) {
            Log.Info($"Crops       : {totals.CropsWithoutCoordinates} without coordinates, " +
                     $"{totals.CropRectanglesDropped} rectangle(s) DROPPED");
        }

        if (!totals.PublishedCacheInvalidated) {
            Log.Warn("Published cache NOT invalidated — rebuild it manually.");
        }

        var needsReview = totals.ValuesUnchanged + totals.ValuesFailed + totals.AltTextDropped
                          + totals.CropsWithoutCoordinates + totals.CropRectanglesDropped;

        if (needsReview > 0) {
            Log.Info($"[REVIEW] items are in {Log.FilePath}");
        }
    }

    private sealed class DataTypeRow {
        public int Id { get; set; }
        public string Config { get; set; }
    }

    private sealed class PropertyDataRow {
        public int Id { get; set; }
        public string TextValue { get; set; }
        public string PropertyAlias { get; set; }
        public int? NodeId { get; set; }
        public string NodeName { get; set; }

        public string NodeDescription => NodeId.HasValue ? $"{NodeId} \"{NodeName}\"" : "(unknown)";
    }
}
