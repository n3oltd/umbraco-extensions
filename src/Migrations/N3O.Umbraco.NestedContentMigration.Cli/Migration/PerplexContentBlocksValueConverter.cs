using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.NestedContentMigration.Cli;

public static class PerplexContentBlocksValueConverter {
    private static readonly HashSet<string> DroppedProperties =
        new(StringComparer.OrdinalIgnoreCase) { "key", "ncContentTypeAlias", "name", "PropType" };

    public static PerplexConversionResult Convert(string perplexJson,
                                                  IReadOnlyDictionary<string, Guid> contentTypeKeys,
                                                  IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>> editorAliases) {
        var result = new PerplexConversionResult();

        JToken parsed;
        try {
            parsed = JToken.Parse(perplexJson);
        } catch {
            return result;
        }

        if (parsed is not JObject root) {
            return result;
        }

        if ((int?) root["version"] != 3) {
            return result;
        }

        var nested = new ConversionResult();

        var output = new JObject {
            ["version"] = 4,
            ["header"] = root["header"] is JObject header
                ? (JToken) ConvertBlock(header, contentTypeKeys, editorAliases, result, nested) ?? JValue.CreateNull()
                : JValue.CreateNull()
        };

        var outBlocks = new JArray();

        if (root["blocks"] is JArray blocks) {
            foreach (var block in blocks.OfType<JObject>()) {
                var converted = ConvertBlock(block, contentTypeKeys, editorAliases, result, nested);

                if (converted != null) {
                    outBlocks.Add(converted);
                }
            }
        }

        output["blocks"] = outBlocks;

        result.NestedContentConverted = nested.NestedContentConvertedNames.Count;
        result.NestedContentBlocks = nested.Blocks;
        result.NestedContentLeftVerbatim.AddRange(nested.NestedContentPropertyNames);
        result.SkippedAliases.AddRange(nested.SkippedAliases);
        result.GeneratedKeys += nested.GeneratedKeys;

        result.Json = JsonConvert.SerializeObject(output);

        return result;
    }

    private static JObject ConvertBlock(JObject block,
                                        IReadOnlyDictionary<string, Guid> contentTypeKeys,
                                        IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>> editorAliases,
                                        PerplexConversionResult result,
                                        ConversionResult nested) {
        JObject item = null;
        var content = block["content"];

        if (content is JArray array) {
            item = array.OfType<JObject>().FirstOrDefault();
        } else if (content is JValue { Type: JTokenType.String } stringValue && stringValue.Value is string text) {
            try {
                if (JToken.Parse(text) is JArray inner) {
                    item = inner.OfType<JObject>().FirstOrDefault();
                }
            } catch {
                item = null;
            }
        }

        if (item == null) {
            return null;
        }

        var alias = (string) item["ncContentTypeAlias"];

        if (alias == null || !contentTypeKeys.TryGetValue(alias, out var contentTypeKey)) {
            if (alias != null) {
                result.SkippedAliases.Add(alias);
            }

            return null;
        }

        if (!Guid.TryParse((string) item["key"], out var itemKey)) {
            itemKey = Guid.NewGuid();
            result.GeneratedKeys++;
        }

        if (block["variants"] is JArray { Count: > 0 }) {
            result.HadVariants = true;
        }

        editorAliases.TryGetValue(contentTypeKey, out var propEditors);

        var values = new JArray();

        foreach (var property in item.Properties()) {
            if (DroppedProperties.Contains(property.Name)) {
                continue;
            }

            if (propEditors == null || !propEditors.TryGetValue(property.Name, out var editorAlias) || editorAlias == null) {
                result.OrphanedProperties.Add($"{alias}.{property.Name}");

                continue;
            }

            var value = NestedContentValueConverter.ConvertNestedContentProperty(property.Name,
                                                                                property.Value.DeepClone(),
                                                                                nested,
                                                                                contentTypeKeys);

            values.Add(new JObject {
                ["editorAlias"] = editorAlias,
                ["culture"] = JValue.CreateNull(),
                ["segment"] = JValue.CreateNull(),
                ["alias"] = property.Name,
                ["value"] = value
            });
        }

        result.Blocks++;

        return new JObject {
            ["id"] = block["id"]?.DeepClone() ?? JValue.CreateNull(),
            ["definitionId"] = block["definitionId"]?.DeepClone() ?? JValue.CreateNull(),
            ["layoutId"] = block["layoutId"]?.DeepClone() ?? JValue.CreateNull(),
            ["presetId"] = block["presetId"]?.DeepClone() ?? JValue.CreateNull(),
            ["isDisabled"] = block["isDisabled"]?.DeepClone() ?? (JToken) false,
            ["content"] = new JObject {
                ["contentTypeKey"] = contentTypeKey,
                ["udi"] = JValue.CreateNull(),
                ["key"] = itemKey,
                ["values"] = values
            }
        };
    }
}
