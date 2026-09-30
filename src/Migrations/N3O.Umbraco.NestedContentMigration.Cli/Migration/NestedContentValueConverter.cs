using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.NestedContentMigration.Cli;

// Converts a stored Nested Content property value (a JSON array) into a Block List property value.
//
// Nested Content (stored):
//   [{"key":"<guid>","ncContentTypeAlias":"<alias>","name":"x","<prop>":<value>, ...}, ...]
//
// Block List (Umbraco 13 / udi-based — the only shape this tool writes):
//   {"layout":{"Umbraco.BlockList":[{"contentUdi":"umb://element/<guidN>"}]},
//    "contentData":[{"contentTypeKey":"<guid>","udi":"umb://element/<guidN>","<prop>":<value>}],
//    "settingsData":[]}
//
// The migration is always run on the Umbraco 13 database; the later 13->17 upgrade converts these udi-based
// values to the v14+ key-based shape via Umbraco's own migrations, so this tool never writes that shape.
public static class NestedContentValueConverter {
    private static readonly string[] DroppedProperties = { "key", "ncContentTypeAlias", "name" };

    // Property names that carry Block List identity on the contentData entry — a source element property
    // with one of these names would clobber the block identity, so it is skipped (and counted).
    // "key" is deliberately absent: it is Nested Content's own item key, so DroppedProperties (checked first)
    // already removes it, and listing it here as well would only look like a collision that can never happen.
    private static readonly string[] ReservedProperties = { "udi", "contentTypeKey", "contentTypeAlias" };

    // Converts a Nested Content value into a Block List value. Result.Json is null when the input is not a
    // Nested Content array (not valid JSON, or already a Block List / other shape) — the caller must then
    // leave the stored value untouched rather than overwrite it. The counters report what happened.
    public static ConversionResult Convert(string nestedJson,
                                           IReadOnlyDictionary<string, Guid> contentTypeKeys) {
        var result = new ConversionResult();

        JToken parsed;
        try {
            parsed = JToken.Parse(nestedJson);
        } catch {
            return result;
        }

        // Only a Nested Content array is convertible. Anything else (an already-migrated Block List object,
        // or any other shape) is left untouched — overwriting it would destroy data on a re-run.
        if (parsed is not JArray items) {
            return result;
        }

        var layoutItems = new JArray();
        var contentData = new JArray();

        foreach (var item in items.OfType<JObject>()) {
            var keyStr = (string) item["key"];
            var alias = (string) item["ncContentTypeAlias"];

            if (!Guid.TryParse(keyStr, out var keyGuid)) {
                keyGuid = Guid.NewGuid();
                result.GeneratedKeys++;
            }

            if (alias == null || !contentTypeKeys.TryGetValue(alias, out var contentTypeKey)) {
                if (alias != null) {
                    result.SkippedAliases.Add(alias);
                }

                continue;
            }

            var udi = "umb://element/" + keyGuid.ToString("N");

            layoutItems.Add(new JObject { ["contentUdi"] = udi });

            var contentEntry = new JObject {
                ["contentTypeKey"] = contentTypeKey,
                ["udi"] = udi
            };

            CopyElementProperties(item, contentEntry, result, contentTypeKeys);

            contentData.Add(contentEntry);

            result.Blocks++;
        }

        result.Json = SerializeBlockListValue(layoutItems, contentData);

        return result;
    }

    private static void CopyElementProperties(JObject source,
                                              JObject target,
                                              ConversionResult result,
                                              IReadOnlyDictionary<string, Guid> contentTypeKeys) {
        foreach (var property in source.Properties()) {
            if (DroppedProperties.Contains(property.Name)) {
                continue;
            }

            if (ReservedProperties.Contains(property.Name)) {
                // Would overwrite the block's identity fields — skip it rather than corrupt the block.
                result.PropertyCollisionNames.Add(property.Name);

                continue;
            }

            target[property.Name] = ConvertNestedContentProperty(property.Name,
                                                                 property.Value,
                                                                 result,
                                                                 contentTypeKeys);
        }
    }

    // An element property whose own value is Nested Content is converted recursively, to any depth. Step 1
    // of the migration flips every in-use Nested Content data type to Block List — including the ones on
    // element types — so leaving the inner value as a Nested Content array would leave the data type and
    // its stored value disagreeing, and the inner content would not render after the 13→17 upgrade (where
    // Umbraco.NestedContent no longer exists).
    //
    // The inner value's representation is preserved: Umbraco stores a nested complex-editor value as a
    // *serialized JSON string* inside the parent's contentData entry (verified against real v13 data —
    // "links":"[{...}]"), so a string in yields a string out, and a live array yields a live object.
    // Public so PerplexContentBlocksValueConverter reuses this exact implementation for the property values
    // inside a Perplex block, rather than carrying a second copy of the same recursion.
    public static JToken ConvertNestedContentProperty(string propertyName,
                                                      JToken value,
                                                      ConversionResult result,
                                                      IReadOnlyDictionary<string, Guid> contentTypeKeys) {
        if (value is JArray array && IsNestedContentArray(array)) {
            var inner = Convert(array.ToString(Formatting.None), contentTypeKeys);

            if (inner.Json == null) {
                result.NestedContentPropertyNames.Add(propertyName);

                return value;
            }

            Merge(inner, result, propertyName);

            return JToken.Parse(inner.Json);
        }

        if (TryGetNestedContentString(value, out var nestedJson)) {
            var inner = Convert(nestedJson, contentTypeKeys);

            if (inner.Json == null) {
                result.NestedContentPropertyNames.Add(propertyName);

                return value;
            }

            Merge(inner, result, propertyName);

            return inner.Json;
        }

        return value;
    }

    // Folds a recursive conversion's counters into the parent so the run summary counts nested blocks,
    // dropped aliases, generated keys and collisions exactly once, wherever in the tree they occurred.
    private static void Merge(ConversionResult inner, ConversionResult parent, string propertyName) {
        parent.NestedContentConvertedNames.Add(propertyName);
        parent.Blocks += inner.Blocks;
        parent.GeneratedKeys += inner.GeneratedKeys;
        parent.SkippedAliases.AddRange(inner.SkippedAliases);
        parent.PropertyCollisionNames.AddRange(inner.PropertyCollisionNames);
        parent.NestedContentPropertyNames.AddRange(inner.NestedContentPropertyNames);
        parent.NestedContentConvertedNames.AddRange(inner.NestedContentConvertedNames);
    }

    // The common real-world shape: a nested Nested Content property's value is stored as a serialized JSON
    // string (a JValue), not a live array. Yields the inner JSON when the string really is an NC array.
    private static bool TryGetNestedContentString(JToken value, out string nestedJson) {
        nestedJson = null;

        if (value is not JValue { Type: JTokenType.String } stringValue
            || stringValue.Value is not string text
            || !text.Contains("ncContentTypeAlias")) {
            return false;
        }

        try {
            if (JToken.Parse(text) is JArray inner && IsNestedContentArray(inner)) {
                nestedJson = text;

                return true;
            }
        } catch {
            return false;
        }

        return false;
    }

    private static bool IsNestedContentArray(JArray array) {
        return array.OfType<JObject>().Any(o => o["ncContentTypeAlias"] != null);
    }

    private static string SerializeBlockListValue(JArray layoutItems, JArray contentData) {
        var value = new JObject {
            ["layout"] = new JObject {
                ["Umbraco.BlockList"] = layoutItems
            },
            ["contentData"] = contentData,
            ["settingsData"] = new JArray()
        };

        return JsonConvert.SerializeObject(value);
    }
}
