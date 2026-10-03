using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace N3O.Umbraco.NestedContentMigration.Cli;

public static class PerplexRichTextFixer {
    private const int ExcerptLength = 120;

    private static readonly HashSet<string> RichTextEditorAliases =
        new(StringComparer.OrdinalIgnoreCase) { "Umbraco.RichText", "Umbraco.TinyMCE" };

    public static string Fix(string json,
                             IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>> editorAliases,
                             Func<int, LocalLinkTarget> findNodeTarget,
                             RichTextFixResult result) {
        var token = TryParse(json);

        if (token == null) {
            result.Problems.Add(IsJson(json) ?
                                "the value holds a number outside the decimal range, so its rich text was NOT checked" :
                                "the value is not JSON, so its rich text was NOT checked");

            return null;
        } else if (!FixToken(token, editorAliases, findNodeTarget, result)) {
            return null;
        } else {
            return JsonConvert.SerializeObject(token);
        }
    }

    // An {editorAlias, alias, value} entry names its own editor; a v13 Block List item's properties sit beside
    // contentTypeKey and take their editors from the element type.
    private static bool FixToken(JToken token,
                                 IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>> editorAliases,
                                 Func<int, LocalLinkTarget> findNodeTarget,
                                 RichTextFixResult result) {
        var changed = false;

        if (token is JArray array) {
            foreach (var item in array) {
                changed |= FixToken(item, editorAliases, findNodeTarget, result);
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

                changed |= FixProperty(property, editorAlias, editorAliases, findNodeTarget, result);
            }
        }

        return changed;
    }

    private static bool FixProperty(JProperty property,
                                    string editorAlias,
                                    IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>> editorAliases,
                                    Func<int, LocalLinkTarget> findNodeTarget,
                                    RichTextFixResult result) {
        var isRichText = editorAlias != null && RichTextEditorAliases.Contains(editorAlias);

        if (property.Value is JValue { Type: JTokenType.String } stringValue) {
            var text = (string) stringValue;
            var fixedText = isRichText ?
                            FixRichTextValue(text, editorAliases, findNodeTarget, result) :
                            FixNestedJson(text, editorAliases, findNodeTarget, result);

            if (fixedText == null) {
                return false;
            }

            property.Value = fixedText;

            return true;
        }

        if (isRichText && property.Value is JObject richText) {
            return FixRichText(richText, editorAliases, findNodeTarget, result);
        }

        return FixToken(property.Value, editorAliases, findNodeTarget, result);
    }

    // A rich text value is either the raw HTML or, once it holds blocks, {markup, blocks}.
    private static string FixRichTextValue(string text,
                                           IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>> editorAliases,
                                           Func<int, LocalLinkTarget> findNodeTarget,
                                           RichTextFixResult result) {
        var token = TryParse(text);

        if (token is JObject richText) {
            var changed = FixRichText(richText, editorAliases, findNodeTarget, result);

            return changed ? JsonConvert.SerializeObject(richText) : null;
        } else if (token == null && IsJson(text)) {
            result.Problems.Add("rich text holds a number outside the decimal range, so it was NOT checked: " +
                                Excerpt(text));

            return null;
        } else {
            var fixedHtml = RichTextMarkupFixer.Fix(text, findNodeTarget, result);

            return fixedHtml == text ? null : fixedHtml;
        }
    }

    private static bool FixRichText(JObject richText,
                                    IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>> editorAliases,
                                    Func<int, LocalLinkTarget> findNodeTarget,
                                    RichTextFixResult result) {
        var changed = FixToken(richText["blocks"], editorAliases, findNodeTarget, result);

        if (richText["markup"] is JValue { Type: JTokenType.String } markup) {
            var html = (string) markup;
            var fixedHtml = RichTextMarkupFixer.Fix(html, findNodeTarget, result);

            if (fixedHtml != html) {
                richText["markup"] = fixedHtml;
                changed = true;
            }
        }

        return changed;
    }

    // Block editor values nested in a block are stored as serialised JSON strings.
    private static string FixNestedJson(string text,
                                        IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>> editorAliases,
                                        Func<int, LocalLinkTarget> findNodeTarget,
                                        RichTextFixResult result) {
        var token = TryParse(text);

        if (token == null && IsJson(text)) {
            result.Problems.Add("a nested value holds a number outside the decimal range, so its rich text was NOT " +
                                $"checked: {Excerpt(text)}");

            return null;
        } else if (token == null || !FixToken(token, editorAliases, findNodeTarget, result)) {
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

    // No date parsing and decimal floats: a rewrite must not reformat dates or round numbers elsewhere in the value.
    private static JToken TryParse(string text) {
        var trimmed = text?.TrimStart();

        if (string.IsNullOrEmpty(trimmed) || (trimmed[0] != '{' && trimmed[0] != '[')) {
            return null;
        }

        try {
            using (var reader = new JsonTextReader(new StringReader(text))) {
                reader.DateParseHandling = DateParseHandling.None;
                reader.FloatParseHandling = FloatParseHandling.Decimal;

                return JToken.ReadFrom(reader);
            }
        } catch (JsonReaderException) {
            return null;
        }
    }
}
