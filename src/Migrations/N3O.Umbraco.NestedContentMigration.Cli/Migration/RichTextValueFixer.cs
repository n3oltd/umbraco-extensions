using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace N3O.Umbraco.NestedContentMigration.Cli;

public static class RichTextValueFixer {
    private const int ExcerptLength = 120;

    private static readonly HashSet<string> RichTextEditorAliases =
        new(StringComparer.OrdinalIgnoreCase) { "Umbraco.RichText", "Umbraco.TinyMCE" };

    public static string Fix(string json,
                             IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>> editorAliases,
                             Func<string, string> fixMarkup,
                             RichTextFixResult result) {
        var token = TryParse(json, out var hasTrailingText);

        if (token == null) {
            result.Problems.Add(IsJson(json) ?
                                "the value holds a number outside the decimal range, so its rich text was NOT checked" :
                                "the value is not JSON, so its rich text was NOT checked");

            return null;
        } else if (hasTrailingText) {
            result.Problems.Add("the value has text after its JSON, so its rich text was NOT checked");

            return null;
        } else if (!FixToken(token, editorAliases, fixMarkup, result)) {
            return null;
        } else {
            return JsonConvert.SerializeObject(token);
        }
    }

    public static string FixPropertyValue(string value,
                                          string editorAlias,
                                          IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>> editorAliases,
                                          Func<string, string> fixMarkup,
                                          RichTextFixResult result) {
        if (RichTextEditorAliases.Contains(editorAlias)) {
            return FixRichTextValue(value, editorAliases, fixMarkup, result);
        } else {
            return Fix(value, editorAliases, fixMarkup, result);
        }
    }

    private static bool FixToken(JToken token,
                                 IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>> editorAliases,
                                 Func<string, string> fixMarkup,
                                 RichTextFixResult result) {
        var changed = false;

        if (token is JArray array) {
            foreach (var item in array) {
                changed |= FixToken(item, editorAliases, fixMarkup, result);
            }
        } else if (token is JObject obj) {
            var inlineEditor = obj["editorAlias"] is JValue { Type: JTokenType.String } editor ? (string) editor : null;
            var elementEditors = GetElementEditors(obj, editorAliases);

            foreach (var property in obj.Properties().ToList()) {
                string editorAlias = null;

                if (inlineEditor != null && property.Name == "value") {
                    editorAlias = inlineEditor;
                } else if (elementEditors != null) {
                    elementEditors.TryGetValue(property.Name, out editorAlias);
                }

                changed |= FixProperty(property, editorAlias, editorAliases, fixMarkup, result);
            }
        }

        return changed;
    }

    private static bool FixProperty(JProperty property,
                                    string editorAlias,
                                    IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>> editorAliases,
                                    Func<string, string> fixMarkup,
                                    RichTextFixResult result) {
        var isRichText = editorAlias != null && RichTextEditorAliases.Contains(editorAlias);

        if (property.Value is JValue { Type: JTokenType.String } stringValue) {
            var text = (string) stringValue;
            var fixedText = isRichText ?
                            FixRichTextValue(text, editorAliases, fixMarkup, result) :
                            FixNestedJson(text, editorAliases, fixMarkup, result);

            if (fixedText == null) {
                return false;
            }

            property.Value = fixedText;

            return true;
        }

        if (isRichText && property.Value is JObject richText) {
            return FixRichText(richText, editorAliases, fixMarkup, result);
        }

        return FixToken(property.Value, editorAliases, fixMarkup, result);
    }

    private static string FixRichTextValue(string text,
                                           IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>> editorAliases,
                                           Func<string, string> fixMarkup,
                                           RichTextFixResult result) {
        var token = TryParse(text, out var hasTrailingText);

        if (token is JObject && hasTrailingText) {
            result.Problems.Add($"rich text has text after its JSON, so it was NOT checked: {Excerpt(text)}");

            return null;
        } else if (token is JObject richText) {
            var changed = FixRichText(richText, editorAliases, fixMarkup, result);

            return changed ? JsonConvert.SerializeObject(richText) : null;
        } else if (token == null && IsJson(text)) {
            result.Problems.Add("rich text holds a number outside the decimal range, so it was NOT checked: " +
                                Excerpt(text));

            return null;
        } else {
            var fixedHtml = fixMarkup(text);

            return fixedHtml == text ? null : fixedHtml;
        }
    }

    private static bool FixRichText(JObject richText,
                                    IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>> editorAliases,
                                    Func<string, string> fixMarkup,
                                    RichTextFixResult result) {
        if (richText["markup"] is not JValue { Type: JTokenType.String } markup) {
            result.Problems.Add("rich text is JSON without a markup string, so it was NOT checked: " +
                                Excerpt(richText.ToString(Formatting.None)));

            return false;
        } else {
            var changed = FixToken(richText["blocks"], editorAliases, fixMarkup, result);
            var html = (string) markup;
            var fixedHtml = fixMarkup(html);

            if (fixedHtml != html) {
                richText["markup"] = fixedHtml;
                changed = true;
            }

            return changed;
        }
    }

    private static string FixNestedJson(string text,
                                        IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>> editorAliases,
                                        Func<string, string> fixMarkup,
                                        RichTextFixResult result) {
        var token = TryParse(text, out var hasTrailingText);

        if (token == null && IsJson(text)) {
            result.Problems.Add("a nested value holds a number outside the decimal range, so its rich text was NOT " +
                                $"checked: {Excerpt(text)}");

            return null;
        } else if (token != null && hasTrailingText && HoldsObject(token)) {
            result.Problems.Add("a nested value has text after its JSON, so its rich text was NOT checked: " +
                                Excerpt(text));

            return null;
        } else if (token == null || hasTrailingText || !FixToken(token, editorAliases, fixMarkup, result)) {
            return null;
        } else {
            return JsonConvert.SerializeObject(token);
        }
    }

    private static string Excerpt(string text) {
        return text.Substring(0, Math.Min(ExcerptLength, text.Length));
    }

    private static IReadOnlyDictionary<string, string> GetElementEditors(
            JObject obj,
            IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>> editorAliases) {
        if (obj.ContainsKey("values") ||
            !Guid.TryParse((string) obj["contentTypeKey"], out var contentTypeKey) ||
            !editorAliases.TryGetValue(contentTypeKey, out var editors)) {
            return null;
        }

        return editors;
    }

    private static bool HasMoreContent(JsonReader reader) {
        try {
            return reader.Read();
        } catch (JsonReaderException) {
            return true;
        }
    }

    private static bool HoldsObject(JToken token) {
        return token is JObject || (token is JArray array && array.Any(x => x is JObject));
    }

    private static bool IsJson(string text) {
        var trimmed = text?.TrimStart();

        if (string.IsNullOrEmpty(trimmed) || (trimmed[0] != '{' && trimmed[0] != '[')) {
            return false;
        }

        try {
            JToken.Parse(text);

            return true;
        } catch (JsonReaderException) {
            return false;
        }
    }

    // A rewrite must not reformat dates or round numbers elsewhere in the value.
    private static JToken TryParse(string text, out bool hasTrailingText) {
        var trimmed = text?.TrimStart();

        hasTrailingText = false;

        if (string.IsNullOrEmpty(trimmed) || (trimmed[0] != '{' && trimmed[0] != '[')) {
            return null;
        } else {
            try {
                using (var reader = new JsonTextReader(new StringReader(text))) {
                    reader.DateParseHandling = DateParseHandling.None;
                    reader.FloatParseHandling = FloatParseHandling.Decimal;

                    var token = JToken.ReadFrom(reader);

                    hasTrailingText = HasMoreContent(reader);

                    return token;
                }
            } catch (JsonReaderException) {
                return null;
            }
        }
    }
}
