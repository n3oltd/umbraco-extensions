using N3O.Umbraco.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using Umbraco.Extensions;
using ContentBlocksConstants = Perplex.ContentBlocks.Constants;

namespace N3O.Umbraco.Blocks.Perplex;

public class PerplexBlocksCloner : IBlocksCloner {
    public bool CanClone(string propertyEditorAlias) {
        return ContentBlocksConstants.PropertyEditor.Alias.EqualsInvariant(propertyEditorAlias);
    }

    public string Clone(string value) {
        return Rewrite(value, () => Guid.NewGuid());
    }

    public string StripIdentifiers(string value) {
        var index = 0;

        return Rewrite(value, () => index++);
    }

    private JObject[] GetObjects(JToken token) {
        return (token as JArray)?.OfType<JObject>().ToArray() ?? Array.Empty<JObject>();
    }

    // key is a reserved identifier only in nested content
    private bool IsNestedContent(JArray array) {
        return array.First is JObject first && first["key"] != null && first["ncContentTypeAlias"] != null;
    }

    private JObject ParseObject(string json) {
        if (!json.HasValue() || !json.DetectIsJson()) {
            return null;
        }

        try {
            return JObject.Parse(json);
        } catch (JsonException) {
            return null;
        }
    }

    private string Rewrite(string value, Func<JToken> getIdentifier) {
        var json = ParseObject(value);

        if (json == null) {
            return value;
        }

        RewriteBlock(json["header"] as JObject, getIdentifier);

        foreach (var block in GetObjects(json["blocks"])) {
            RewriteBlock(block, getIdentifier);
        }

        return json.ToString(Formatting.None);
    }

    private void RewriteBlock(JObject block, Func<JToken> getIdentifier) {
        if (block == null) {
            return;
        }

        block["id"] = getIdentifier();

        foreach (var item in GetObjects(block["content"])) {
            RewriteNestedContent(item, getIdentifier);
        }

        foreach (var variant in GetObjects(block["variants"])) {
            RewriteBlock(variant, getIdentifier);
        }
    }

    private void RewriteNestedContent(JObject item, Func<JToken> getIdentifier) {
        if (item?["key"] == null) {
            return;
        }

        item["key"] = getIdentifier();

        foreach (var property in item.Properties().ToList()) {
            if (property.Value is not JValue { Type: JTokenType.String } value) {
                continue;
            }

            var text = (string) value.Value;

            if (!text.HasValue() || !text.TrimStart().StartsWith("[")) {
                continue;
            }

            if (TryParseArray(text, out var nested) && IsNestedContent(nested)) {
                foreach (var nestedItem in GetObjects(nested)) {
                    RewriteNestedContent(nestedItem, getIdentifier);
                }

                property.Value = nested.ToString(Formatting.None);
            }
        }
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
