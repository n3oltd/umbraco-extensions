using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.Blocks;
using static Umbraco.Cms.Core.Constants.PropertyEditors;
using UdiEntityType = Umbraco.Cms.Core.Constants.UdiEntityType;

namespace N3O.Umbraco.Blocks.Extensions;

public static class BlockItemDataExtensions {
    // The backoffice posts block property values as parsed JSON, but property value converters read the stored
    // form, which is a string. Umbraco's serializer is System.Text.Json from v14, so those values arrive as
    // JsonNode: the Newtonsoft types this used to test for no longer occur and every structured value was
    // reaching the converters unchanged.
    public static void FormatBlockData(this List<BlockItemData> blockData) {
        foreach (var contentData in blockData.OrEmpty()) {
            foreach (var value in contentData.Values.OrEmpty()) {
                value.Value = FormatValue(value.PropertyType?.PropertyEditorAlias, value.Value);
            }
        }
    }

    private static object FormatValue(string editorAlias, object value) {
        if (editorAlias == Aliases.ContentPicker) {
            return Guid.TryParse(value?.ToString(), out var contentKey)
                       ? Udi.Create(UdiEntityType.Document, contentKey).UriValue.ToString()
                       : value;
        }

        // MultipleTextStringValueConverter splits on newlines rather than reading JSON.
        if (editorAlias == Aliases.MultipleTextstring) {
            return value is JsonArray lines ? string.Join("\r\n", lines.Select(GetLine)) : value;
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

        return value is JsonNode node and (JsonObject or JsonArray) ? node.ToJsonString() : value;
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
            Guid.TryParse(unique?.ToString(), out var key)) {
            return Udi.Create(entityType?.ToString(), key).UriValue.ToString();
        }

        return null;
    }

    // A line was stored as { value: "..." } before v14 and is a plain string now.
    private static string GetLine(JsonNode line) {
        if (line is JsonObject wrapper) {
            return wrapper.TryGetPropertyValue("value", out var text) ? text?.ToString() : null;
        }

        return line?.ToString();
    }
}
