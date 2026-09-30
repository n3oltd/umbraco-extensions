using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.NestedContentMigration.Cli;

public static class NestedContentValueConverter {
    private static readonly string[] DroppedProperties = { "key", "ncContentTypeAlias", "name" };

    private static readonly string[] ReservedProperties = { "udi", "contentTypeKey", "contentTypeAlias" };

    public static ConversionResult Convert(string nestedJson,
                                           IReadOnlyDictionary<string, Guid> contentTypeKeys) {
        var result = new ConversionResult();

        JToken parsed;
        try {
            parsed = JToken.Parse(nestedJson);
        } catch {
            return result;
        }

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
                result.PropertyCollisionNames.Add(property.Name);

                continue;
            }

            target[property.Name] = ConvertNestedContentProperty(property.Name,
                                                                 property.Value,
                                                                 result,
                                                                 contentTypeKeys);
        }
    }

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

    private static void Merge(ConversionResult inner, ConversionResult parent, string propertyName) {
        parent.NestedContentConvertedNames.Add(propertyName);
        parent.Blocks += inner.Blocks;
        parent.GeneratedKeys += inner.GeneratedKeys;
        parent.SkippedAliases.AddRange(inner.SkippedAliases);
        parent.PropertyCollisionNames.AddRange(inner.PropertyCollisionNames);
        parent.NestedContentPropertyNames.AddRange(inner.NestedContentPropertyNames);
        parent.NestedContentConvertedNames.AddRange(inner.NestedContentConvertedNames);
    }

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
