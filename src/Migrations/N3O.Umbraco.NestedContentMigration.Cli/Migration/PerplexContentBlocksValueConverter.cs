using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.NestedContentMigration.Cli;

// Converts a stored Perplex.ContentBlocks property value from the v3 shape (each block's content is a
// NestedContent array) to the v4 shape (each block's content is an Umbraco Block Editor element:
// {contentTypeKey, key, values:[{editorAlias, alias, value}]}). Perplex v4 ships no content migration, so
// existing v13 content must be converted for it to render after a 13→17 + Perplex-4 upgrade.
//
// v3 (stored):  {"version":3,"header":<block>|null,"blocks":[<block>,...]}
//   block:      {"id","definitionId","layoutId","content":[{NC item}],"variants":...}
// v4 (written): {"version":4,"header":<block>|null,"blocks":[<block>,...]}
//   block:      {"id","definitionId","layoutId","presetId":null,"isDisabled":false,
//                "content":{"contentTypeKey","udi":null,"key","values":[{editorAlias,culture:null,segment:null,alias,value}]}}
//
// Result.Json is null when the input is not a convertible v3 Perplex value (already v4, not valid JSON, or an
// unrecognised shape) — the caller then leaves the stored value untouched (so the pass is re-runnable).
public static class PerplexContentBlocksValueConverter {
    // NestedContent item fields that are not block content and must not become v4 values: the standard NC
    // meta fields, plus "PropType" — an N3O/Perplex block meta field (a discriminator, stored null) that is
    // not a content property and has no place in the v4 Block Editor element.
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

        // Only the v3 shape is convertible. v4 (already migrated) or anything else is left untouched.
        if ((int?) root["version"] != 3) {
            return result;
        }

        // Counters for any Nested Content found inside block property values; folded into result below.
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

    // Converts one Perplex block (v3) to v4. Returns null when the block's content element type can't be
    // resolved (block dropped, alias recorded) so the caller can omit it rather than write a broken block.
    private static JObject ConvertBlock(JObject block,
                                        IReadOnlyDictionary<string, Guid> contentTypeKeys,
                                        IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>> editorAliases,
                                        PerplexConversionResult result,
                                        ConversionResult nested) {
        // v3 content is a NestedContent array; a Perplex block holds a single element (the first item). Umbraco
        // stores it either as a live array or (less often) as a serialized JSON string.
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

            // Resolve the property's editor alias from the element type (compositions included). If it can't
            // be resolved the property is not on the element type — an orphaned value from a removed property:
            // drop it (v4 ignores unknown-alias values; a null editorAlias can break the v4 reader) and flag it.
            if (propEditors == null || !propEditors.TryGetValue(property.Name, out var editorAlias) || editorAlias == null) {
                result.OrphanedProperties.Add($"{alias}.{property.Name}");

                continue;
            }

            // A block property whose own value is Nested Content must be converted too — its data type was
            // flipped to Block List by step 1, so copying the NC array verbatim leaves the two disagreeing.
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
