using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.MediaEditorMigration.Cli;

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

        foreach (var row in rows) {
            NormalizeRow(row, totals);
        }
    }

    // nc-migrate's BuildEditorAliases is a deliberate copy of this walk; change the two together.
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

        var expose = new JArray();

        foreach (var key in contentKeys) {
            expose.Add(new JObject {
                ["contentKey"] = key.ToString(),
                ["culture"] = JValue.CreateNull(),
                ["segment"] = JValue.CreateNull()
            });
        }

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

                var propertyValue = Walk(property.Value.DeepClone(), context);

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
