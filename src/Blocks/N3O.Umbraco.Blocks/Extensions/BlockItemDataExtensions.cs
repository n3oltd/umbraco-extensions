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

// Aliased because Constants alone binds to N3O.Umbraco.Constants from inside this namespace, and qualifying it
// does not help either: Umbraco resolves to N3O.Umbraco.
using UdiEntityType = Umbraco.Cms.Core.Constants.UdiEntityType;

namespace N3O.Umbraco.Blocks.Extensions;

public static class BlockItemDataExtensions {
    // The backoffice posts block property values as parsed JSON, but property value converters read the stored
    // form, which is a string. Umbraco's JsonObjectConverter decides what "parsed" means: an object becomes a
    // JsonObject, an array of objects a JsonArray, an array of same typed scalars a List<T>, and an empty array
    // null.
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

        // MultipleTextStringValueConverter splits on newlines rather than reading JSON. The lines arrive as a
        // List<string> at the top level and as a JsonArray inside a nested block's raw JSON.
        if (editorAlias == Aliases.MultipleTextstring) {
            if (value is IEnumerable<string> lines) {
                return string.Join("\r\n", lines);
            }

            return value is JsonArray nodes ? string.Join("\r\n", nodes.Select(x => x?.ToString())) : value;
        }

        // The picker posts entity references but stores comma separated udis, which is the translation
        // MultiNodeTreePickerPropertyValueEditor.FromEditor performs on save.
        if (editorAlias == Aliases.MultiNodeTreePicker) {
            return value is JsonArray references
                       ? string.Join(",", references.Select(GetUdi).Where(x => x.HasValue()))
                       : value;
        }

        // A nested block editor's own property values are posted in the editor shape too, so they need the same
        // treatment before the outer value is written back out as a string.
        if (value is JsonObject blockValue && blockValue.ContainsKey("contentData")) {
            FormatNestedBlockData(blockValue);
        }

        if (value is JsonNode node and (JsonObject or JsonArray)) {
            return node.ToJsonString();
        }

        // Every editor that posts an array of scalars, such as a checkbox list or a tag picker, is a List<T>
        // rather than a JsonNode, and its converter reads the stored JSON form.
        if (value is IEnumerable and not string) {
            return JsonSerializer.Serialize(value);
        }

        return value;
    }

    // Nested values carry their own editorAlias, which is what identifies them this far down: the property types
    // resolved for the outer blocks only describe the outer blocks.
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
