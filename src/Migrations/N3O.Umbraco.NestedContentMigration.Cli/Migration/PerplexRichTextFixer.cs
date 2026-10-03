using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace N3O.Umbraco.NestedContentMigration.Cli;

public static class PerplexRichTextFixer {
    private static readonly HashSet<string> RichTextEditorAliases =
        new(StringComparer.OrdinalIgnoreCase) { "Umbraco.RichText", "Umbraco.TinyMCE" };

    public static string Fix(string json,
                             IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>> editorAliases,
                             Func<int, LocalLinkTarget> findNodeTarget,
                             RichTextFixResult result) {
        var token = TryParse(json);

        if (token == null || !FixToken(token, editorAliases, findNodeTarget, result)) {
            return null;
        }

        return JsonConvert.SerializeObject(token);
    }

    // A property's editor is named inline in the v4 / v17 shape ({editorAlias, alias, value}); in a v13 Block List
    // item the properties are keys beside contentTypeKey, so the editor comes from the element type.
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
                            FixRichTextValue(text, findNodeTarget, result) :
                            FixNestedJson(text, editorAliases, findNodeTarget, result);

            if (fixedText == null) {
                return false;
            }

            property.Value = fixedText;

            return true;
        }

        if (isRichText && property.Value is JObject richText) {
            return FixMarkup(richText, findNodeTarget, result);
        }

        return FixToken(property.Value, editorAliases, findNodeTarget, result);
    }

    // A rich text value is either the raw HTML or, once it holds blocks, {markup, blocks}.
    private static string FixRichTextValue(string text,
                                           Func<int, LocalLinkTarget> findNodeTarget,
                                           RichTextFixResult result) {
        if (TryParse(text) is JObject richText) {
            return FixMarkup(richText, findNodeTarget, result) ? JsonConvert.SerializeObject(richText) : null;
        }

        var fixedHtml = RichTextMarkupFixer.Fix(text, findNodeTarget, result);

        return fixedHtml == text ? null : fixedHtml;
    }

    private static bool FixMarkup(JObject richText,
                                  Func<int, LocalLinkTarget> findNodeTarget,
                                  RichTextFixResult result) {
        if (richText["markup"] is not JValue { Type: JTokenType.String } markup) {
            return false;
        }

        var html = (string) markup;
        var fixedHtml = RichTextMarkupFixer.Fix(html, findNodeTarget, result);

        if (fixedHtml == html) {
            return false;
        }

        richText["markup"] = fixedHtml;

        return true;
    }

    // Block editor values nested in a block are stored as serialised JSON strings.
    private static string FixNestedJson(string text,
                                        IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>> editorAliases,
                                        Func<int, LocalLinkTarget> findNodeTarget,
                                        RichTextFixResult result) {
        var token = TryParse(text);

        if (token == null || !FixToken(token, editorAliases, findNodeTarget, result)) {
            return null;
        }

        return JsonConvert.SerializeObject(token);
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

    // Dates and decimals are kept as written, so a value only changes where markup was fixed.
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
