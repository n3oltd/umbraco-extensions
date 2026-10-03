using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace N3O.Umbraco.NestedContentMigration.Cli;

public static class RichTextMarkupFixer {
    private const string EmbedDialogClass = "embeditem";
    private const string EmbedHolderClass = "umb-embed-holder";
    private const int ExcerptLength = 120;

    private static readonly string[] EmbedDialogAttributes =
        ["data-embed-constrain", "data-embed-height", "data-embed-url", "data-embed-width"];

    private static readonly HashSet<string> EmbedTags =
        new(StringComparer.InvariantCultureIgnoreCase) {
            "audio", "button", "canvas", "embed", "iframe", "object", "video"
        };

    private static readonly Regex EndTagPattern =
        new(@"\G</(?<name>[^\s/>]+)\s*>", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly HashSet<string> LinkEntityTypes =
        new(StringComparer.InvariantCultureIgnoreCase) { "document", "media" };

    private static readonly Regex LocalLinkPattern =
        new(@"^(?<lead>/?)(?<open>\{|%7B)localLink:(?<id>[^}%]*)(?:\}|%7D)(?<tail>.*)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private static readonly Regex StartTagPattern =
        new(@"\G<(?<name>[^\s/>]+)" +
            @"(?:(?:[\s/]+|(?<=[""']))[^\s/>=""']+(?:\s*=\s*(?:""[^""]*""|'[^']*'|[^\s>]+))?)*[\s/]*>",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex UdiPattern =
        new(@"^umb://(?<type>[a-z-]+)/(?<key>[0-9a-f-]+)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static string Fix(string html, Func<int, LocalLinkTarget> findNodeTarget, RichTextFixResult result) {
        if (string.IsNullOrEmpty(html)) {
            return html;
        }

        var document = new HtmlDocument();
        document.LoadHtml(html);

        var edits = new List<Edit>();

        AddEmbedEdits(document, html, edits, result);
        AddLocalLinkEdits(document, html, findNodeTarget, edits, result);
        FlagRichTextBlocks(document, result);

        return ApplyEdits(html, edits);
    }

    public static string WrapEmbeds(string html, RichTextFixResult result) {
        if (string.IsNullOrEmpty(html)) {
            return html;
        }

        var document = new HtmlDocument();
        document.LoadHtml(html);

        var edits = new List<Edit>();

        AddEmbedEdits(document, html, edits, result);

        return ApplyEdits(html, edits);
    }

    private static void AddEdit(List<Edit> edits, int index, int length, string text) {
        var edit = new Edit();
        edit.Index = index;
        edit.Length = length;
        edit.Sequence = edits.Count;
        edit.Text = text;

        edits.Add(edit);
    }

    private static void AddEmbedEdits(HtmlDocument document,
                                      string html,
                                      List<Edit> edits,
                                      RichTextFixResult result) {
        var embeds = document.DocumentNode
                             .Descendants()
                             .Where(x => EmbedTags.Contains(x.Name))
                             .Where(x => !x.Ancestors().Any(IsEmbedOrHolder))
                             .ToList();

        foreach (var embed in embeds) {
            var end = FindEnd(embed, html);

            if (end == null) {
                result.Problems.Add($"<{embed.Name}> has no end tag, so it was NOT wrapped and the v17 editor will " +
                                    $"drop it: {Excerpt(html, embed.OuterStartIndex)}");
            } else {
                AddEdit(edits, embed.OuterStartIndex, 0, GetHolderStartTag(embed));
                AddEdit(edits, end.Value, 0, "</span>");

                result.EmbedsWrapped.Add(embed.Name);

                if (embed.Name == "button") {
                    result.ButtonsWrapped.Add(html.Substring(embed.OuterStartIndex, end.Value - embed.OuterStartIndex));
                }
            }
        }
    }

    private static void AddLocalLinkEdits(HtmlDocument document,
                                          string html,
                                          Func<int, LocalLinkTarget> findNodeTarget,
                                          List<Edit> edits,
                                          RichTextFixResult result) {
        foreach (var element in document.DocumentNode.Descendants().Where(x => x.Attributes["href"] != null)) {
            var href = element.Attributes["href"];
            var match = LocalLinkPattern.Match(href.Value);

            if (!match.Success || IsKeyLink(element, match)) {
                continue;
            }

            var target = FindLinkTarget(match.Groups["id"].Value, findNodeTarget);
            var type = element.GetAttributeValue("type", null);

            if (target == null) {
                result.UnconvertedLinks.Add(href.Value);
            } else if (type != null && !type.Equals(target.EntityType, StringComparison.InvariantCultureIgnoreCase)) {
                result.UnconvertedLinks.Add($"{href.Value} (it already has type=\"{type}\")");
            } else {
                var tail = match.Groups["tail"].Value;
                var dataAnchor = element.GetAttributeValue("data-anchor", null);

                if (!string.IsNullOrEmpty(dataAnchor) && !tail.Contains(dataAnchor) && !tail.Contains('#')) {
                    tail += dataAnchor;
                }

                var hrefEnd = href.ValueStartIndex + href.ValueLength;
                var localLink = $"{match.Groups["lead"].Value}{{localLink:{target.Key:D}}}{tail}";

                AddEdit(edits, href.ValueStartIndex, href.ValueLength, localLink);

                if (type == null) {
                    var afterQuote = hrefEnd < html.Length && html[hrefEnd] is '"' or '\'' ? hrefEnd + 1 : hrefEnd;

                    AddEdit(edits, afterQuote, 0, $" type=\"{target.EntityType}\"");
                }

                result.LinksConverted++;
            }
        }
    }

    private static string ApplyEdits(string html, List<Edit> edits) {
        EnsureNoOverlap(edits);

        var fixedHtml = html;

        foreach (var edit in edits.OrderByDescending(x => x.Index).ThenByDescending(x => x.Sequence)) {
            fixedHtml = fixedHtml.Remove(edit.Index, edit.Length).Insert(edit.Index, edit.Text);
        }

        return fixedHtml;
    }

    private static void EnsureNoOverlap(IReadOnlyList<Edit> edits) {
        foreach (var replacement in edits.Where(x => x.Length > 0)) {
            var replacementEnd = replacement.Index + replacement.Length;
            var overlapping = edits.FirstOrDefault(x => x != replacement &&
                                                        x.Index < replacementEnd &&
                                                        x.Index + Math.Max(x.Length, 1) > replacement.Index);

            if (overlapping != null) {
                throw new Exception($"Two edits to the rich text overlap, at {replacement.Index} and " +
                                    $"{overlapping.Index}");
            }
        }
    }

    private static string Excerpt(string html, int start) {
        return html.Substring(start, Math.Min(ExcerptLength, html.Length - start));
    }

    private static int? FindEnd(HtmlNode embed, string html) {
        var startTag = StartTagPattern.Match(html, embed.OuterStartIndex);

        if (!IsTagOf(startTag, embed.Name)) {
            throw new Exception($"<{embed.Name}> does not start where the parser placed it, at " +
                                $"{embed.OuterStartIndex}: {Excerpt(html, embed.OuterStartIndex)}");
        }

        if (HtmlNode.IsEmptyElement(embed.Name)) {
            var endTag = EndTagPattern.Match(html, startTag.Index + startTag.Length);

            return IsTagOf(endTag, embed.Name) ? endTag.Index + endTag.Length : startTag.Index + startTag.Length;
        } else if (embed.EndNode == null || embed.EndNode == embed || embed.EndNode.OuterStartIndex < 0) {
            return null;
        } else {
            var endTag = EndTagPattern.Match(html, embed.EndNode.OuterStartIndex);

            if (!IsTagOf(endTag, embed.Name) || endTag.Length != embed.EndNode.OuterLength) {
                throw new Exception($"<{embed.Name}> does not end where the parser placed it, at " +
                                    $"{embed.EndNode.OuterStartIndex}: {Excerpt(html, embed.OuterStartIndex)}");
            }

            return endTag.Index + endTag.Length;
        }
    }

    private static LocalLinkTarget FindLinkTarget(string id, Func<int, LocalLinkTarget> findNodeTarget) {
        var udi = UdiPattern.Match(id);

        if (udi.Success) {
            var entityType = udi.Groups["type"].Value.ToLowerInvariant();
            var isKey = Guid.TryParse(udi.Groups["key"].Value, out var key);

            return isKey && LinkEntityTypes.Contains(entityType) ? new LocalLinkTarget(key, entityType) : null;
        } else if (int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var nodeId)) {
            return findNodeTarget(nodeId);
        } else {
            return null;
        }
    }

    private static void FlagRichTextBlocks(HtmlDocument document, RichTextFixResult result) {
        if (document.DocumentNode.Descendants().Any(x => x.Name is "umb-rte-block" or "umb-rte-block-inline")) {
            result.Problems.Add("rich text holds blocks (<umb-rte-block>), which this pass does NOT convert to the " +
                                "v17 form, so the v17 editor cannot load them; convert them by hand");
        }
    }

    // A span, not a div: the holder sits inside <p>, which a browser closes before any block element.
    private static string GetHolderStartTag(HtmlNode embed) {
        var startTag = new StringBuilder($"<span class=\"{EmbedHolderClass}\"");
        var embedDialog = embed.Ancestors().FirstOrDefault(x => x.HasClass(EmbedDialogClass));

        if (embedDialog != null) {
            foreach (var name in EmbedDialogAttributes) {
                var attribute = embedDialog.Attributes[name];

                if (attribute != null) {
                    startTag.Append($" {name}=\"{attribute.Value.Replace("\"", "&quot;")}\"");
                }
            }
        }

        startTag.Append('>');

        return startTag.ToString();
    }

    private static bool IsEmbedOrHolder(HtmlNode node) {
        return EmbedTags.Contains(node.Name) || node.HasClass(EmbedHolderClass);
    }

    private static bool IsKeyLink(HtmlNode element, Match localLink) {
        var type = element.GetAttributeValue("type", null);

        return localLink.Groups["open"].Value == "{" &&
               Guid.TryParse(localLink.Groups["id"].Value, out _) &&
               type != null &&
               LinkEntityTypes.Contains(type);
    }

    private static bool IsTagOf(Match tag, string name) {
        return tag.Success && tag.Groups["name"].Value.Equals(name, StringComparison.InvariantCultureIgnoreCase);
    }

    private class Edit {
        public int Index { get; set; }
        public int Length { get; set; }
        public int Sequence { get; set; }
        public string Text { get; set; }
    }
}
