using N3O.Umbraco.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.Blocks;
using static Umbraco.Cms.Core.Constants.PropertyEditors;
using UdiEntityType = Umbraco.Cms.Core.Constants.UdiEntityType;

namespace N3O.Umbraco.Blocks.Extensions;

public static class BlockItemDataExtensions {
    public static void FormatBlockData(this List<BlockItemData> blockData) {
        foreach (var contentData in blockData.OrEmpty()) {
            foreach (var value in contentData.Values.OrEmpty()) {
                value.Value = FormatValue(value.PropertyType?.PropertyEditorAlias, value.Value);
            }
        }
    }

    private static object FormatValue(string editorAlias, object value) {
        if (editorAlias == Aliases.ContentPicker) {
            return Guid.TryParse(value?.ToString(), out var contentKey) && contentKey != Guid.Empty
                       ? Udi.Create(UdiEntityType.Document, contentKey).UriValue.ToString()
                       : value;
        }

        if (editorAlias == Aliases.MultipleTextstring) {
            if (value is IEnumerable<string> lines) {
                return string.Join("\r\n", lines);
            }

            return value is JsonArray nodes ? string.Join("\r\n", nodes.Select(x => x?.ToString())) : value;
        }

        if (editorAlias == Aliases.MultiNodeTreePicker) {
            return value is JsonArray references
                       ? string.Join(",", references.Select(GetUdi).Where(x => x.HasValue()))
                       : value;
        }

        if (value is JsonObject blockValue && blockValue.ContainsKey("contentData")) {
            FormatNestedBlockData(blockValue);
        }

        if (value is JsonNode node and (JsonObject or JsonArray)) {
            return node.ToJsonString();
        }

        if (value is IEnumerable and not string) {
            return JsonSerializer.Serialize(value);
        }

        return value;
    }

    private static void FormatNestedBlockData(JsonObject blockValue) {
        var elements = new[] { "contentData", "settingsData" }.SelectMany(x => blockValue[x] as JsonArray ?? [])
                                                              .OfType<JsonObject>();

        foreach (var element in elements) {
            foreach (var value in (element["values"] as JsonArray ?? []).OfType<JsonObject>()) {
                var current = value["value"];
                var formatted = FormatValue(value["editorAlias"]?.ToString(), current);

                if (!ReferenceEquals(formatted, current)) {
                    value["value"] = formatted as string;
                }
            }
        }
    }

    private static string GetUdi(JsonNode reference) {
        if (reference is JsonObject entity &&
            entity.TryGetPropertyValue("type", out var entityType) &&
            entity.TryGetPropertyValue("unique", out var unique) &&
            Guid.TryParse(unique?.ToString(), out var key) &&
            key != Guid.Empty) {
            // Parsed rather than created because Udi.Create throws on an entity type Umbraco does not know, and
            // this pass runs once for the whole grid, so one unconvertible reference would otherwise replace
            // every block on the page with an error banner.
            return UdiParser.TryParse($"umb://{entityType}/{key:N}", out var udi) ? udi.ToString() : null;
        }

        return null;
    }
}
