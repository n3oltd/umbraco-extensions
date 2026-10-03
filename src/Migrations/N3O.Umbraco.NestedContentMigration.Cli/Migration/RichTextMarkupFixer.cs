using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace N3O.Umbraco.NestedContentMigration.Cli;

public static class RichTextMarkupFixer {
    // Umbraco's Tiptap Embedded Media node keeps whatever is inside an element with this class verbatim. Umbraco
    // registers it as inline, so a span holder is valid in the <p> and <div> parents embeds sit in.
    private const string EmbedHolderClass = "umb-embed-holder";
    private const string EmbedDialogClass = "embeditem";
    private const int ExcerptLength = 120;

    private static readonly string[] EmbedDialogAttributes =
        ["data-embed-constrain", "data-embed-height", "data-embed-url", "data-embed-width"];

    private static readonly HashSet<string> EmbedTags =
        new(StringComparer.InvariantCultureIgnoreCase) {
            "audio", "button", "canvas", "embed", "iframe", "object", "video"
        };

    private static readonly HashSet<string> LinkEntityTypes =
        new(StringComparer.OrdinalIgnoreCase) { "document", "media" };

    private static readonly Regex LegacyLocalLinkPattern =
        new(@"^(?<lead>/?)\{localLink:umb://(?<type>[a-z]+)/(?<id>[0-9a-f-]{32,36})\}(?<tail>.*)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private static readonly Regex EndTagPattern =
        new(@"\G</(?<name>[^\s/>]+)\s*>", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex StartTagPattern =
        new(@"\G<(?<name>[^\s/>]+)" +
            @"(?:(?:[\s/]+|(?<=[""']))[^\s/>=""']+(?:\s*=\s*(?:""[^""]*""|'[^']*'|[^\s>]+))?)*[\s/]*>",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string Fix(string html, RichTextFixResult result) {
        if (string.IsNullOrEmpty(html)) {
            return html;
        }

        var document = new HtmlDocument();
        document.LoadHtml(html);

        var edits = new List<Edit>();

        AddEmbedEdits(document, html, edits, result);
        AddLocalLinkEdits(document, html, edits, result);

        if (edits.Count == 0) {
            return html;
        }

        EnsureNoOverlap(edits);

        var fixedHtml = html;

        foreach (var edit in edits.OrderByDescending(x => x.Index).ThenByDescending(x => x.Sequence)) {
            fixedHtml = fixedHtml.Remove(edit.Index, edit.Length).Insert(edit.Index, edit.Text);
        }

        return fixedHtml;
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

                result.EmbedsWrapped++;
            }
        }
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

    // Mirrors Umbraco's own V15 local link migration, which never sees links held inside Perplex values: the UDI
    // becomes a key, a data-anchor is appended unless the href already has a fragment, and a type attribute
    // follows the href.
    private static void AddLocalLinkEdits(HtmlDocument document,
                                          string html,
                                          List<Edit> edits,
                                          RichTextFixResult result) {
        foreach (var anchor in document.DocumentNode.Descendants("a")) {
            var href = anchor.Attributes["href"];

            if (href == null) {
                continue;
            }

            var match = LegacyLocalLinkPattern.Match(href.Value);

            if (!match.Success) {
                continue;
            }

            var entityType = match.Groups["type"].Value.ToLowerInvariant();

            if (!LinkEntityTypes.Contains(entityType) || !Guid.TryParse(match.Groups["id"].Value, out var key)) {
                result.UnconvertedLinks.Add(href.Value);

                continue;
            }

            var tail = match.Groups["tail"].Value;
            var dataAnchor = anchor.GetAttributeValue("data-anchor", null);

            if (!string.IsNullOrEmpty(dataAnchor) && !tail.Contains(dataAnchor) && !tail.Contains('#')) {
                tail += dataAnchor;
            }

            var hrefEnd = href.ValueStartIndex + href.ValueLength;
            var localLink = $"{match.Groups["lead"].Value}{{localLink:{key:D}}}{tail}";

            AddEdit(edits, href.ValueStartIndex, href.ValueLength, localLink);

            if (anchor.Attributes["type"] == null) {
                var afterQuote = hrefEnd < html.Length && html[hrefEnd] is '"' or '\'' ? hrefEnd + 1 : hrefEnd;

                AddEdit(edits, afterQuote, 0, $" type=\"{entityType}\"");
            }

            result.LinksConverted++;
        }
    }

    private static void AddEdit(List<Edit> edits, int index, int length, string text) {
        var edit = new Edit();
        edit.Index = index;
        edit.Length = length;
        edit.Sequence = edits.Count;
        edit.Text = text;

        edits.Add(edit);
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

    private static bool IsEmbedOrHolder(HtmlNode node) {
        return EmbedTags.Contains(node.Name) || node.HasClass(EmbedHolderClass);
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
