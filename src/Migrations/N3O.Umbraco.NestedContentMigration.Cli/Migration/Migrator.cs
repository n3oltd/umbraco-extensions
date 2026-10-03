using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace N3O.Umbraco.NestedContentMigration.Cli;

public sealed class Migrator {
    private const int NoCommandTimeout = 0;

    private static readonly Regex NestedNamePattern =
        new(@"^Nested\s+(?<body>.*?)\s*(?<suffix>\([^)]*\))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private const string CacheSerializerKey = "Umbraco.Web.PublishedCache.NuCache.Serializer";

    private static readonly Guid DocumentObjectType = new("C66BA18E-EAF3-4CFF-8A22-41B16D66A972");
    private static readonly Guid MediaObjectType = new("B796F64C-1F99-4FFB-B886-4BF4BC011A9C");

    private const int MaxInClauseParameters = 2000;

    private const int RichTextPageSize = 500;

    private readonly CliOptions _options;

    public Migrator(CliOptions options) {
        _options = options;
    }

    public bool Run() {
        using var connection = new SqlConnection(_options.ConnectionString);
        connection.Open();

        Log.Info($"Connected to '{connection.Database}' on '{connection.DataSource}'.");
        Log.Info(_options.DryRun ? "Mode: DRY RUN (no changes will be committed)." : "Mode: APPLY (changes WILL be committed).");

        var hasUiAliasColumn = HasPropertyEditorUiAliasColumn(connection);

        Log.Info($"Detected schema: {(hasUiAliasColumn ? "Umbraco 14+" : "Umbraco 13")} " +
                 $"(umbracoDataType.propertyEditorUiAlias {(hasUiAliasColumn ? "present" : "absent")}).");

        if (hasUiAliasColumn) {
            Log.Error("This database is on the Umbraco 14+ schema — this tool only migrates Umbraco 13 databases.");
            Log.Error("Run it before upgrading Umbraco; the Umbraco 13→17 upgrade converts the Block List values itself.");

            return false;
        }

        using var transaction = connection.BeginTransaction();

        var succeeded = Migrate(connection, transaction);

        if (succeeded) {
            var cleared = Execute(connection,
                                  transaction,
                                  "DELETE FROM umbracoKeyValue WHERE [key] = @key",
                                  ("@key", CacheSerializerKey));

            if (cleared > 0) {
                Log.Info(_options.DryRun
                             ? "WOULD clear the published-cache serializer marker, making Umbraco rebuild the " +
                               "cache on next start (rolled back with the rest of this dry run)."
                             : "Cleared the published-cache serializer marker; Umbraco will rebuild the cache " +
                               "on next start.");
            } else {
                Log.Warn($"No '{CacheSerializerKey}' row found, so the published cache was NOT invalidated. " +
                         "Rebuild the published cache manually or the site will keep serving pre-migration content.");
            }
        }

        if (!succeeded) {
            transaction.Rollback();
            Log.Error("Migration aborted — all changes rolled back.");

            return false;
        }

        if (_options.DryRun) {
            transaction.Rollback();
            Log.Success("DRY RUN complete — all changes rolled back. Re-run with --apply to commit.");
        } else {
            transaction.Commit();
            Log.Success("Migration committed.");
        }

        return true;
    }

    private bool Migrate(SqlConnection cn, SqlTransaction tx) {
        if (!MigrateNestedContent(cn, tx)) {
            return false;
        }

        if (_options.IncludePerplex && !MigratePerplex(cn, tx)) {
            return false;
        }

        if (_options.IncludeEmbeds && !MigrateEmbeds(cn, tx)) {
            return false;
        }

        return true;
    }

    private bool MigrateNestedContent(SqlConnection cn, SqlTransaction tx) {
        var dataTypes = Query(cn, tx,
            "SELECT dt.nodeId, dt.[config], n.text FROM umbracoDataType dt " +
            "INNER JOIN umbracoNode n ON n.id = dt.nodeId " +
            "WHERE dt.propertyEditorAlias = 'Umbraco.NestedContent'",
            r => new DataTypeRow {
                Id = r.GetInt32(0),
                Config = r.IsDBNull(1) ? null : r.GetString(1),
                Name = r.IsDBNull(2) ? null : r.GetString(2)
            });

        if (dataTypes.Count == 0) {
            Log.Info("No Nested Content data types found — nothing to migrate.");

            return true;
        }

        Log.Info($"Found {dataTypes.Count} Nested Content data type(s).");

        var usedDataTypeIds = new HashSet<int>(QueryIn(cn, tx,
            "SELECT DISTINCT dataTypeId FROM cmsPropertyType WHERE dataTypeId IN ({0})",
            "u",
            dataTypes.Select(d => (object) d.Id),
            r => r.GetInt32(0)));

        var unassigned = dataTypes.Where(dt => !usedDataTypeIds.Contains(dt.Id)).ToList();

        if (unassigned.Count > 0) {
            Log.Info($"{unassigned.Count} data type(s) have no content property but are converted anyway " +
                     "(mostly Perplex v3 per-block ones) — see the README.");

            foreach (var dt in unassigned) {
                Log.Verbose(_options.Verbose, $"Data type {dt.Id} has no content property assigned (config only).");
            }
        }

        var aliasMap = dataTypes.ToDictionary(dt => dt.Id, dt => ParseElementAliases(dt.Config, dt.Id));

        var allAliases = aliasMap.Values.SelectMany(x => x).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var contentTypeKeys = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        if (allAliases.Count > 0) {
            var rows = QueryIn(cn, tx,
                "SELECT ct.alias, n.uniqueId FROM cmsContentType ct " +
                "INNER JOIN umbracoNode n ON n.id = ct.nodeId " +
                "WHERE ct.isElement = 1 AND ct.alias IN ({0})",
                "a",
                allAliases.Cast<object>(),
                r => new { Alias = r.GetString(0), Key = r.GetGuid(1) });

            foreach (var row in rows) {
                contentTypeKeys[row.Alias] = row.Key;
            }
        }

        foreach (var alias in allAliases.Where(a => !contentTypeKeys.ContainsKey(a))) {
            Log.Warn($"Element content type alias '{alias}' was not found — blocks of this type will be skipped.");
        }

        var renamed = 0;

        foreach (var dt in dataTypes) {
            var config = BuildBlockListConfig(aliasMap[dt.Id], contentTypeKeys, dt.Config, dt.Id);

            Execute(cn, tx,
                    "UPDATE umbracoDataType SET propertyEditorAlias = 'Umbraco.BlockList', [config] = @config WHERE nodeId = @nodeId",
                    ("@config", config), ("@nodeId", dt.Id));

            var newName = BuildBlockListName(dt.Name);

            if (newName != null) {
                Execute(cn, tx, "UPDATE umbracoNode SET text = @text WHERE id = @nodeId",
                        ("@text", newName), ("@nodeId", dt.Id));

                renamed++;
                Log.Verbose(_options.Verbose, $"Data type {dt.Id} renamed '{dt.Name}' → '{newName}'.");
            }

            Log.Verbose(_options.Verbose, $"Data type {dt.Id} → Block List ({string.Join(", ", aliasMap[dt.Id])}).");
        }


        var dataTypeIds = dataTypes.Select(d => d.Id).Cast<object>().ToList();

        var propertyTypes = QueryIn(cn, tx,
            "SELECT id, dataTypeId FROM cmsPropertyType WHERE dataTypeId IN ({0})",
            "d",
            dataTypeIds,
            r => new { Id = r.GetInt32(0), DataTypeId = r.GetInt32(1) });

        if (propertyTypes.Count == 0) {
            Log.Info("No properties reference the migrated data types — no values to convert.");

            return true;
        }

        var propertyTypeIds = propertyTypes.Select(p => (object) p.Id).ToList();

        var values = QueryIn(cn, tx,
            "SELECT pd.id, pd.propertyTypeId, pd.textValue, pt.Alias, cv.nodeId, n.text " +
            "FROM umbracoPropertyData pd " +
            "INNER JOIN cmsPropertyType pt ON pt.id = pd.propertyTypeId " +
            "LEFT JOIN umbracoContentVersion cv ON cv.id = pd.versionId " +
            "LEFT JOIN umbracoNode n ON n.id = cv.nodeId " +
            "WHERE pd.propertyTypeId IN ({0}) AND pd.textValue IS NOT NULL AND pd.textValue <> ''",
            "p",
            propertyTypeIds,
            r => new PropertyDataRow {
                Id = r.GetInt32(0),
                PropertyTypeId = r.GetInt32(1),
                TextValue = r.GetString(2),
                PropertyAlias = r.IsDBNull(3) ? null : r.GetString(3),
                NodeId = r.IsDBNull(4) ? (int?) null : r.GetInt32(4),
                NodeName = r.IsDBNull(5) ? null : r.GetString(5)
            });

        Log.Info($"Found {values.Count} property value(s) to convert.");

        var converted = 0;
        var unchanged = 0;
        var failed = 0;
        var totalBlocks = 0;
        var totalSkipped = 0;
        var totalGeneratedKeys = 0;
        var totalNestedContent = 0;
        var totalNestedConverted = 0;
        var totalCollisions = 0;

        foreach (var pv in values) {
            var issues = new List<string>();

            try {
                var result = NestedContentValueConverter.Convert(pv.TextValue, contentTypeKeys);

                if (result.Json == null) {
                    unchanged++;
                    issues.Add("NOT MIGRATED — not a Nested Content array (already migrated, empty or an " +
                               "unrecognised shape); left untouched");
                } else {
                    Execute(cn, tx, "UPDATE umbracoPropertyData SET textValue = @value WHERE id = @id",
                            ("@value", result.Json), ("@id", pv.Id));

                    converted++;
                    totalBlocks += result.Blocks;
                    totalSkipped += result.SkippedAliases.Count;
                    totalGeneratedKeys += result.GeneratedKeys;
                    totalNestedContent += result.NestedContentPropertyNames.Count;
                    totalNestedConverted += result.NestedContentConvertedNames.Count;
                    totalCollisions += result.PropertyCollisionNames.Count;

                    if (result.SkippedAliases.Count > 0) {
                        issues.Add($"{result.SkippedAliases.Count} block(s) DROPPED, element type not found: " +
                                   $"{string.Join(", ", result.SkippedAliases.Distinct())}");
                    }

                    if (result.NestedContentPropertyNames.Count > 0) {
                        issues.Add("nested NC copied verbatim, convert by hand: " +
                                   $"{string.Join(", ", result.NestedContentPropertyNames.Distinct())}");
                    }

                    if (result.PropertyCollisionNames.Count > 0) {
                        issues.Add("propertie(s) DROPPED, name clashes with a reserved Block List field: " +
                                   $"{string.Join(", ", result.PropertyCollisionNames.Distinct())}");
                    }

                    if (result.GeneratedKeys > 0) {
                        issues.Add($"{result.GeneratedKeys} block(s) had no valid key; new GUIDs generated");
                    }

                    Log.Verbose(_options.Verbose, $"Property value {pv.Id} (node {pv.NodeDescription}, '{pv.PropertyAlias}'): {result.Blocks} block(s) converted.");
                }
            } catch (Exception ex) {
                failed++;
                issues.Add($"FAILED to convert — {ex.Message}");
            }

            if (issues.Count > 0) {
                Log.Item($"value id {pv.Id} | node {pv.NodeDescription} | property '{pv.PropertyAlias}'", issues);
            }
        }

        Log.Info($"Data types  : {dataTypes.Count} converted, {renamed} renamed");
        Log.Info($"Values      : {converted} converted, {unchanged} unchanged, {failed} failed");
        Log.Info($"Blocks      : {totalBlocks} converted" +
                 (totalSkipped > 0 ? $", {totalSkipped} skipped (unmatched element type)" : ""));
        Log.Info($"Nested NC   : {totalNestedConverted} converted recursively" +
                 (totalNestedContent > 0 ? $", {totalNestedContent} left verbatim — convert by hand" : ""));

        if (totalCollisions > 0 || totalGeneratedKeys > 0) {
            Log.Info($"Also        : {totalCollisions} reserved-name collision(s) skipped, " +
                     $"{totalGeneratedKeys} key(s) generated");
        }

        if (unchanged + failed + totalSkipped + totalNestedContent + totalCollisions > 0) {
            Log.Info($"[REVIEW] items are in {Log.FilePath}");
        }

        if (failed > 0) {
            Log.Error($"{failed} property value(s) failed to convert — aborting so nothing is left half-migrated.");

            return false;
        }

        return true;
    }

    private bool MigratePerplex(SqlConnection cn, SqlTransaction tx) {
        Log.Info("Perplex ContentBlocks v3 -> v4 (--include-perplex):");

        var dataTypeIds = Query(cn, tx,
            "SELECT nodeId FROM umbracoDataType WHERE propertyEditorAlias = 'Perplex.ContentBlocks'",
            r => (object) r.GetInt32(0));

        if (dataTypeIds.Count == 0) {
            Log.Info("No Perplex.ContentBlocks data types found — nothing to convert.");

            return true;
        }

        Log.Info($"Found {dataTypeIds.Count} Perplex.ContentBlocks data type(s).");

        var contentTypeKeys = BuildElementTypeKeys(cn, tx, out var nodeIdByKey);
        var editorAliases = BuildEditorAliases(cn, tx, nodeIdByKey);

        var propertyTypeIds = QueryIn(cn, tx,
            "SELECT id FROM cmsPropertyType WHERE dataTypeId IN ({0})",
            "d",
            dataTypeIds,
            r => (object) r.GetInt32(0));

        if (propertyTypeIds.Count == 0) {
            Log.Info("No properties reference the Perplex data types — no values to convert.");

            return true;
        }

        var values = QueryIn(cn, tx,
            "SELECT pd.id, pd.propertyTypeId, pd.textValue, pt.Alias, cv.nodeId, n.text " +
            "FROM umbracoPropertyData pd " +
            "INNER JOIN cmsPropertyType pt ON pt.id = pd.propertyTypeId " +
            "LEFT JOIN umbracoContentVersion cv ON cv.id = pd.versionId " +
            "LEFT JOIN umbracoNode n ON n.id = cv.nodeId " +
            "WHERE pd.propertyTypeId IN ({0}) AND pd.textValue IS NOT NULL AND pd.textValue <> ''",
            "p",
            propertyTypeIds,
            r => new PropertyDataRow {
                Id = r.GetInt32(0),
                PropertyTypeId = r.GetInt32(1),
                TextValue = r.GetString(2),
                PropertyAlias = r.IsDBNull(3) ? null : r.GetString(3),
                NodeId = r.IsDBNull(4) ? (int?) null : r.GetInt32(4),
                NodeName = r.IsDBNull(5) ? null : r.GetString(5)
            });

        Log.Info($"Found {values.Count} Perplex property value(s) to inspect.");

        var converted = 0;
        var richTextOnly = 0;
        var unchanged = 0;
        var failed = 0;
        var totalBlocks = 0;
        var totalDropped = 0;
        var totalGeneratedKeys = 0;
        var totalOrphaned = 0;
        var variantValues = 0;
        var totalNestedConvertedInBlocks = 0;
        var totalNestedBlocks = 0;
        var totalNestedVerbatim = 0;
        var totalEmbedsWrapped = 0;
        var totalButtonsWrapped = 0;
        var totalLinksConverted = 0;
        var totalLinksUnconverted = 0;
        var nodeTargets = new Dictionary<int, LocalLinkTarget>();

        LocalLinkTarget FindNodeTarget(int nodeId) {
            if (!nodeTargets.TryGetValue(nodeId, out var target)) {
                target = Query(cn, tx,
                    "SELECT uniqueId, nodeObjectType FROM umbracoNode " +
                    "WHERE id = @id AND nodeObjectType IN (@document, @media)",
                    r => new LocalLinkTarget(r.GetGuid(0), r.GetGuid(1) == DocumentObjectType ? "document" : "media"),
                    ("@id", nodeId),
                    ("@document", DocumentObjectType),
                    ("@media", MediaObjectType)).SingleOrDefault();

                nodeTargets[nodeId] = target;
            }

            return target;
        }

        foreach (var pv in values) {
            var issues = new List<string>();

            try {
                var result = PerplexContentBlocksValueConverter.Convert(pv.TextValue, contentTypeKeys, editorAliases);
                var richText = new RichTextFixResult();
                var fixedJson = RichTextValueFixer.Fix(result.Json ?? pv.TextValue,
                                                       editorAliases,
                                                       html => RichTextMarkupFixer.Fix(html, FindNodeTarget, richText),
                                                       richText);
                var newJson = fixedJson ?? result.Json;

                if (newJson != null) {
                    Execute(cn, tx, "UPDATE umbracoPropertyData SET textValue = @value WHERE id = @id",
                            ("@value", newJson), ("@id", pv.Id));
                }

                totalEmbedsWrapped += richText.EmbedsWrapped.Count;
                totalButtonsWrapped += richText.ButtonsWrapped.Count;
                totalLinksConverted += richText.LinksConverted;
                totalLinksUnconverted += richText.UnconvertedLinks.Count;

                if (fixedJson != null) {
                    Log.Info($"Rich text   : perplex value id {pv.Id} | node {pv.NodeDescription} | property " +
                             $"'{pv.PropertyAlias}' — {DescribeRichTextChanges(richText)}");
                }

                AddRichTextIssues(issues, richText);

                if (result.Json == null && fixedJson == null) {
                    unchanged++;
                    issues.Add("NOT CONVERTED — not a Perplex v3 value (already v4, empty or an unrecognised " +
                               "shape); left untouched");
                } else if (result.Json == null) {
                    richTextOnly++;
                    issues.Add("NOT CONVERTED — not a Perplex v3 value (already v4 or an unrecognised shape); left " +
                               "untouched apart from its rich text");
                } else {
                    converted++;
                    totalBlocks += result.Blocks;
                    totalDropped += result.SkippedAliases.Count;
                    totalGeneratedKeys += result.GeneratedKeys;
                    totalOrphaned += result.OrphanedProperties.Distinct().Count();
                    totalNestedConvertedInBlocks += result.NestedContentConverted;
                    totalNestedBlocks += result.NestedContentBlocks;
                    totalNestedVerbatim += result.NestedContentLeftVerbatim.Count;

                    if (result.NestedContentLeftVerbatim.Count > 0) {
                        issues.Add($"nested NC copied verbatim, convert by hand: " +
                                   $"{string.Join(", ", result.NestedContentLeftVerbatim.Distinct())}");
                    }

                    if (result.SkippedAliases.Count > 0) {
                        issues.Add($"{result.SkippedAliases.Count} block(s) DROPPED, element type not found: " +
                                   $"{string.Join(", ", result.SkippedAliases.Distinct())}");
                    }

                    if (result.OrphanedProperties.Count > 0) {
                        issues.Add($"orphaned propertie(s) DROPPED (removed from the element type): " +
                                   $"{string.Join(", ", result.OrphanedProperties.Distinct())}");
                    }

                    if (result.HadVariants) {
                        variantValues++;
                        issues.Add("Perplex v3 'variants' (culture/segment content) NOT carried across");
                    }

                    if (result.GeneratedKeys > 0) {
                        issues.Add($"{result.GeneratedKeys} block(s) had no valid key; new GUIDs generated");
                    }

                    Log.Verbose(_options.Verbose, $"Perplex value {pv.Id} (node {pv.NodeDescription}, '{pv.PropertyAlias}'): {result.Blocks} block(s) → v4.");
                }
            } catch (Exception ex) {
                failed++;
                issues.Add($"FAILED to convert — {ex.Message}");
            }

            if (issues.Count > 0) {
                Log.Item($"perplex value id {pv.Id} | node {pv.NodeDescription} | property '{pv.PropertyAlias}'", issues);
            }
        }

        Log.Info($"Perplex     : {converted} value(s) converted, {richTextOnly} rewritten for their rich text " +
                 $"only, {unchanged} unchanged, {failed} failed; {totalBlocks} block(s)");
        Log.Info($"Nested NC   : {totalNestedConvertedInBlocks} propertie(s) in {totalNestedBlocks} block(s)" +
                 (totalNestedVerbatim > 0 ? $", {totalNestedVerbatim} left verbatim — convert by hand" : ""));
        Log.Info($"Rich text   : {totalEmbedsWrapped} embed(s) wrapped ({totalButtonsWrapped} button(s)), " +
                 $"{totalLinksConverted} local link(s) converted" +
                 (totalLinksUnconverted > 0 ? $", {totalLinksUnconverted} left as they were" : ""));

        if (totalDropped + totalOrphaned + variantValues + totalGeneratedKeys > 0) {
            Log.Info($"Dropped     : {totalDropped} block(s) (unmatched element type), {totalOrphaned} orphaned " +
                     $"propertie(s), {variantValues} value(s) with v3 variants; {totalGeneratedKeys} key(s) generated");
        }

        if (failed > 0) {
            Log.Error($"{failed} Perplex value(s) failed to convert — aborting so nothing is left half-migrated.");

            return false;
        }

        return true;
    }

    private bool MigrateEmbeds(SqlConnection cn, SqlTransaction tx) {
        Log.Info("Embeds in rich text (--include-embeds):");

        BuildElementTypeKeys(cn, tx, out var nodeIdByKey);

        var editorAliases = BuildEditorAliases(cn, tx, nodeIdByKey);
        var checkedValues = 0;
        var changed = 0;
        var unchanged = 0;
        var failed = 0;
        var totalEmbedsWrapped = 0;
        var totalButtonsWrapped = 0;
        var lastId = 0;
        List<PropertyDataRow> page;

        do {
            page = Query(cn, tx,
                "SELECT TOP (@pageSize) pd.id, pd.propertyTypeId, pd.textValue, pt.Alias, cv.nodeId, n.text, " +
                "dt.propertyEditorAlias " +
                "FROM umbracoPropertyData pd " +
                "INNER JOIN cmsPropertyType pt ON pt.id = pd.propertyTypeId " +
                "INNER JOIN umbracoDataType dt ON dt.nodeId = pt.dataTypeId " +
                "LEFT JOIN umbracoContentVersion cv ON cv.id = pd.versionId " +
                "LEFT JOIN umbracoNode n ON n.id = cv.nodeId " +
                "WHERE dt.propertyEditorAlias IN " +
                "('Umbraco.TinyMCE', 'Umbraco.RichText', 'Umbraco.BlockList', 'Umbraco.BlockGrid') " +
                "AND pd.textValue IS NOT NULL AND pd.textValue <> '' AND pd.id > @lastId " +
                "ORDER BY pd.id",
                ReadRichTextValue,
                ("@pageSize", RichTextPageSize),
                ("@lastId", lastId));

            foreach (var pv in page) {
                var issues = new List<string>();
                var richText = new RichTextFixResult();

                try {
                    var wrapEmbeds = (string html) => RichTextMarkupFixer.WrapEmbeds(html, richText);
                    var fixedValue = RichTextValueFixer.FixPropertyValue(pv.TextValue,
                                                                         pv.EditorAlias,
                                                                         editorAliases,
                                                                         wrapEmbeds,
                                                                         richText);

                    if (fixedValue == null) {
                        unchanged++;
                    } else {
                        Execute(cn, tx, "UPDATE umbracoPropertyData SET textValue = @value WHERE id = @id",
                                ("@value", fixedValue), ("@id", pv.Id));

                        changed++;
                        totalEmbedsWrapped += richText.EmbedsWrapped.Count;
                        totalButtonsWrapped += richText.ButtonsWrapped.Count;

                        Log.Info($"Embeds      : value id {pv.Id} | node {pv.NodeDescription} | property " +
                                 $"'{pv.PropertyAlias}' — {DescribeRichTextChanges(richText)}");
                    }

                    AddRichTextIssues(issues, richText);
                } catch (Exception ex) {
                    failed++;
                    issues.Add($"FAILED to wrap embeds — {ex.Message}");
                }

                if (issues.Count > 0) {
                    Log.Item($"value id {pv.Id} | node {pv.NodeDescription} | property '{pv.PropertyAlias}'", issues);
                }

                checkedValues++;
                lastId = pv.Id;
            }
        } while (page.Count == RichTextPageSize);

        Log.Info($"Embeds      : {checkedValues} rich text and block value(s) checked, {changed} changed, " +
                 $"{unchanged} unchanged, {failed} failed; {totalEmbedsWrapped} embed(s) wrapped " +
                 $"({totalButtonsWrapped} button(s))");

        if (failed > 0) {
            Log.Error($"{failed} value(s) failed in the embed pass — aborting so nothing is left half-migrated.");

            return false;
        }

        return true;
    }

    private static Dictionary<string, Guid> BuildElementTypeKeys(SqlConnection cn, SqlTransaction tx,
                                                                 out Dictionary<Guid, int> nodeIdByKey) {
        var keys = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var ids = new Dictionary<Guid, int>();

        var rows = Query(cn, tx,
            "SELECT ct.alias, n.uniqueId, ct.nodeId FROM cmsContentType ct " +
            "INNER JOIN umbracoNode n ON n.id = ct.nodeId WHERE ct.isElement = 1",
            r => new { Alias = r.GetString(0), Key = r.GetGuid(1), NodeId = r.GetInt32(2) });

        foreach (var row in rows) {
            keys[row.Alias] = row.Key;
            ids[row.Key] = row.NodeId;
        }

        nodeIdByKey = ids;

        return keys;
    }

    // media-migrate's LoadEditorAliases is a deliberate copy of this walk; change the two together.
    private static IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>> BuildEditorAliases(
            SqlConnection cn, SqlTransaction tx, IReadOnlyDictionary<Guid, int> nodeIdByKey) {
        var propRows = Query(cn, tx,
            "SELECT pt.contentTypeId, pt.Alias, dt.propertyEditorAlias FROM cmsPropertyType pt " +
            "INNER JOIN umbracoDataType dt ON dt.nodeId = pt.dataTypeId",
            r => new { ContentTypeId = r.GetInt32(0), Alias = r.GetString(1), Editor = r.IsDBNull(2) ? null : r.GetString(2) });

        var ownProps = new Dictionary<int, Dictionary<string, string>>();

        foreach (var row in propRows) {
            if (!ownProps.TryGetValue(row.ContentTypeId, out var map)) {
                map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                ownProps[row.ContentTypeId] = map;
            }

            map[row.Alias] = row.Editor;
        }

        var parents = new Dictionary<int, List<int>>();

        var compRows = Query(cn, tx,
            "SELECT parentContentTypeId, childContentTypeId FROM cmsContentType2ContentType",
            r => new { Parent = r.GetInt32(0), Child = r.GetInt32(1) });

        foreach (var row in compRows) {
            if (!parents.TryGetValue(row.Child, out var list)) {
                list = new List<int>();
                parents[row.Child] = list;
            }

            list.Add(row.Parent);
        }

        var effective = new Dictionary<int, Dictionary<string, string>>();

        Dictionary<string, string> Resolve(int contentTypeId, HashSet<int> visiting) {
            if (effective.TryGetValue(contentTypeId, out var cached)) {
                return cached;
            }

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (parents.TryGetValue(contentTypeId, out var ps)) {
                foreach (var parent in ps) {
                    if (visiting.Add(parent)) {
                        foreach (var kv in Resolve(parent, visiting)) {
                            map[kv.Key] = kv.Value;
                        }

                        visiting.Remove(parent);
                    }
                }
            }

            if (ownProps.TryGetValue(contentTypeId, out var own)) {
                foreach (var kv in own) {
                    map[kv.Key] = kv.Value;
                }
            }

            effective[contentTypeId] = map;

            return map;
        }

        var byKey = new Dictionary<Guid, IReadOnlyDictionary<string, string>>();

        foreach (var kv in nodeIdByKey) {
            byKey[kv.Key] = Resolve(kv.Value, new HashSet<int> { kv.Value });
        }

        return byKey;
    }

    private static void AddRichTextIssues(List<string> issues, RichTextFixResult richText) {
        if (richText.ButtonsWrapped.Count > 0) {
            issues.Add($"{richText.ButtonsWrapped.Count} <button>(s) kept as embeds, so their labels can only be " +
                       $"edited in the source view: {string.Join(" | ", richText.ButtonsWrapped)}");
        }

        if (richText.UnconvertedLinks.Count > 0) {
            issues.Add($"{richText.UnconvertedLinks.Count} local link(s) NOT converted, the editor cannot resolve " +
                       $"them: {string.Join(", ", richText.UnconvertedLinks.Distinct())}");
        }

        issues.AddRange(richText.Problems);
    }

    private static string DescribeRichTextChanges(RichTextFixResult richText) {
        var changes = new List<string>();

        if (richText.EmbedsWrapped.Count > 0) {
            var tags = richText.EmbedsWrapped.GroupBy(x => x).Select(x => $"<{x.Key}> × {x.Count()}");

            changes.Add($"wrapped {string.Join(", ", tags)}");
        }

        if (richText.LinksConverted > 0) {
            changes.Add($"converted {richText.LinksConverted} local link(s)");
        }

        return string.Join("; ", changes);
    }

    private static PropertyDataRow ReadRichTextValue(SqlDataReader reader) {
        var row = new PropertyDataRow();
        row.Id = reader.GetInt32(0);
        row.PropertyTypeId = reader.GetInt32(1);
        row.TextValue = reader.GetString(2);
        row.PropertyAlias = reader.IsDBNull(3) ? null : reader.GetString(3);
        row.NodeId = reader.IsDBNull(4) ? (int?) null : reader.GetInt32(4);
        row.NodeName = reader.IsDBNull(5) ? null : reader.GetString(5);
        row.EditorAlias = reader.GetString(6);

        return row;
    }

    private static List<string> ParseElementAliases(string config, int dataTypeId) {
        var aliases = new List<string>();

        if (string.IsNullOrWhiteSpace(config)) {
            return aliases;
        }

        try {
            var obj = JObject.Parse(config);

            if (obj["contentTypes"] is JArray contentTypes) {
                foreach (var contentType in contentTypes) {
                    var alias = (string) contentType["ncAlias"];

                    if (alias != null) {
                        aliases.Add(alias);
                    }
                }
            }
        } catch {
            Log.Warn($"Data type {dataTypeId} has unparseable config — no element types resolved; its Block List will have no blocks.");
        }

        return aliases;
    }

    private static (int? Min, int? Max) ParseItemLimits(string config, int dataTypeId) {
        if (string.IsNullOrWhiteSpace(config)) {
            return (null, null);
        }

        try {
            var obj = JObject.Parse(config);

            return ((int?) obj["minItems"], (int?) obj["maxItems"]);
        } catch {
            Log.Warn($"Data type {dataTypeId} has unparseable config — its min/max item limits were not carried " +
                     "over, so the Block List will accept any number of blocks.");

            return (null, null);
        }
    }

    private static string BuildBlockListConfig(List<string> aliases, IReadOnlyDictionary<string, Guid> contentTypeKeys,
                                              string nestedContentConfig, int dataTypeId) {
        var blocks = new JArray();

        foreach (var alias in aliases) {
            if (!contentTypeKeys.TryGetValue(alias, out var key)) {
                continue;
            }

            blocks.Add(new JObject {
                ["contentElementTypeKey"] = key,
                ["settingsElementTypeKey"] = null,
                ["label"] = null,
                ["editorSize"] = "medium",
                ["forceHideContentEditorInOverlay"] = false
            });
        }

        var (min, max) = ParseItemLimits(nestedContentConfig, dataTypeId);

        var config = new JObject {
            ["blocks"] = blocks,
            ["validationLimit"] = new JObject {
                ["min"] = min.HasValue ? new JValue(min.Value) : JValue.CreateNull(),
                ["max"] = max.HasValue ? new JValue(max.Value) : JValue.CreateNull()
            },
            ["useInlineEditingAsDefault"] = true,
            ["useLiveEditing"] = false,
            ["useSingleBlockMode"] = false
        };

        return JsonConvert.SerializeObject(config);
    }

    private static string BuildBlockListName(string name) {
        if (string.IsNullOrWhiteSpace(name)) {
            return null;
        }

        var match = NestedNamePattern.Match(name.Trim());

        if (!match.Success) {
            return null;
        }

        var body = match.Groups["body"].Value.Trim();

        if (body.Length == 0) {
            return null;
        }

        var suffix = match.Groups["suffix"].Success ? match.Groups["suffix"].Value.Trim() : null;

        return string.IsNullOrWhiteSpace(suffix) ? $"{body} Block List" : $"{body} Block List {suffix}";
    }

    private static bool HasPropertyEditorUiAliasColumn(SqlConnection cn) {
        using var cmd = new SqlCommand(
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'umbracoDataType' AND COLUMN_NAME = 'propertyEditorUiAlias'",
            cn);

        return System.Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    private static List<T> Query<T>(SqlConnection cn, SqlTransaction tx, string sql, Func<SqlDataReader, T> map,
                                    params (string Name, object Value)[] parameters) {
        using var cmd = new SqlCommand(sql, cn, tx) { CommandTimeout = NoCommandTimeout };

        foreach (var parameter in parameters) {
            AddParameter(cmd, parameter.Name, parameter.Value);
        }

        var results = new List<T>();

        using var reader = cmd.ExecuteReader();

        while (reader.Read()) {
            results.Add(map(reader));
        }

        return results;
    }

    private static int Execute(SqlConnection cn, SqlTransaction tx, string sql,
                               params (string Name, object Value)[] parameters) {
        using var cmd = new SqlCommand(sql, cn, tx) { CommandTimeout = NoCommandTimeout };

        foreach (var parameter in parameters) {
            AddParameter(cmd, parameter.Name, parameter.Value);
        }

        return cmd.ExecuteNonQuery();
    }

    private static void AddParameter(SqlCommand cmd, string name, object value) {
        var parameter = new SqlParameter(name, ToSqlDbType(value));

        if (value is string) {
            parameter.Size = -1;
        }

        parameter.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(parameter);
    }

    private static SqlDbType ToSqlDbType(object value) {
        return value switch {
            int => SqlDbType.Int,
            long => SqlDbType.BigInt,
            bool => SqlDbType.Bit,
            Guid => SqlDbType.UniqueIdentifier,
            _ => SqlDbType.NVarChar
        };
    }

    private static List<T> QueryIn<T>(SqlConnection cn, SqlTransaction tx, string sqlFormat, string prefix,
                                      IEnumerable<object> values, Func<SqlDataReader, T> map) {
        var results = new List<T>();

        foreach (var batch in Batch(values)) {
            var (clause, parameters) = BuildInClause(prefix, batch);

            results.AddRange(Query(cn, tx, string.Format(sqlFormat, clause), map, parameters));
        }

        return results;
    }

    private static IEnumerable<List<object>> Batch(IEnumerable<object> values) {
        var batch = new List<object>(MaxInClauseParameters);

        foreach (var value in values) {
            batch.Add(value);

            if (batch.Count == MaxInClauseParameters) {
                yield return batch;

                batch = new List<object>(MaxInClauseParameters);
            }
        }

        if (batch.Count > 0) {
            yield return batch;
        }
    }

    private static (string Clause, (string, object)[] Parameters) BuildInClause(string prefix, IEnumerable<object> values) {
        var list = values.ToList();

        if (list.Count > MaxInClauseParameters) {
            throw new InvalidOperationException(
                $"Cannot build an IN clause with {list.Count} parameter(s) — SQL Server caps a command at 2100. " +
                $"Use QueryIn, which batches.");
        }

        var names = new string[list.Count];
        var parameters = new (string, object)[list.Count];

        for (var i = 0; i < list.Count; i++) {
            var name = $"@{prefix}{i}";

            names[i] = name;
            parameters[i] = (name, list[i]);
        }

        return (string.Join(",", names), parameters);
    }

    private sealed class DataTypeRow {
        public int Id { get; set; }
        public string Config { get; set; }
        public string Name { get; set; }
    }

    private sealed class PropertyDataRow {
        public int Id { get; set; }
        public int PropertyTypeId { get; set; }
        public string TextValue { get; set; }
        public int? NodeId { get; set; }
        public string NodeName { get; set; }
        public string PropertyAlias { get; set; }
        public string EditorAlias { get; set; }

        public string NodeDescription => NodeId.HasValue ? $"{NodeId} \"{NodeName}\"" : "(unknown)";
    }
}
