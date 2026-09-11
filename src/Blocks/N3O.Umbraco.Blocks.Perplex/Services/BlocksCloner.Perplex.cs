using N3O.Umbraco.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using ContentBlocksConstants = Perplex.ContentBlocks.Constants;

namespace N3O.Umbraco.Blocks.Perplex;

public class PerplexBlocksCloner : IBlocksCloner {
    public bool CanClone(string propertyEditorAlias) {
        return ContentBlocksConstants.PropertyEditor.Alias.EqualsInvariant(propertyEditorAlias);
    }

    public string Clone(string value) {
        if (!value.HasValue()) {
            return value;
        }

        var json = JObject.Parse(value);

        CloneBlock(json["header"] as JObject);

        foreach (var block in GetObjects(json["blocks"])) {
            CloneBlock(block);
        }

        return json.ToString(Formatting.None);
    }

    private void CloneBlock(JObject block) {
        if (block == null) {
            return;
        }

        block["id"] = Guid.NewGuid();

        foreach (var item in GetObjects(block["content"])) {
            CloneNestedContent(item);
        }

        foreach (var variant in GetObjects(block["variants"])) {
            CloneBlock(variant);
        }
    }

    private void CloneNestedContent(JObject item) {
        if (item?["key"] == null) {
            return;
        }

        item["key"] = Guid.NewGuid();

        foreach (var property in item.Properties().ToList()) {
            if (property.Value is not JValue { Type: JTokenType.String } value) {
                continue;
            }

            var text = (string) value.Value;

            if (!text.HasValue() || !text.TrimStart().StartsWith("[")) {
                continue;
            }

            if (TryParseArray(text, out var nested)) {
                foreach (var nestedItem in GetObjects(nested)) {
                    CloneNestedContent(nestedItem);
                }

                property.Value = nested.ToString(Formatting.None);
            }
        }
    }

    private JObject[] GetObjects(JToken token) {
        return (token as JArray)?.OfType<JObject>().ToArray() ?? Array.Empty<JObject>();
    }

    private bool TryParseArray(string text, out JArray array) {
        try {
            array = JArray.Parse(text);

            return true;
        } catch (JsonException) {
            array = null;

            return false;
        }
    }
}
