using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace N3O.Umbraco.NestedContentMigration.Cli;

public static class RichTextMarkupFixer {
    // Umbraco's Tiptap Embedded Media node keeps whatever is inside an element with this class verbatim. Umbraco
    // registers it as inline, so a span holder is valid in the <p> and <div> parents embeds sit in.
    private const string EmbedHolderClass = "umb-embed-holder";

    private static readonly HashSet<string> EmbedTags =
        new(StringComparer.OrdinalIgnoreCase) { "button", "iframe", "object" };

    private static readonly HashSet<string> LinkEntityTypes =
        new(StringComparer.OrdinalIgnoreCase) { "document", "media" };

    private static readonly Regex LegacyLocalLinkPattern =
        new(@"^(?<lead>/?)\{localLink:umb://(?<type>[a-z]+)/(?<id>[0-9a-f-]{32,36})\}(?<tail>.*)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public static string Fix(string html, RichTextFixResult result) {
        if (string.IsNullOrEmpty(html)) {
            return html;
        }

        var document = new HtmlDocument();
        document.LoadHtml(html);

        var edits = new List<Edit>();

        AddEmbedEdits(document, edits, result);
        AddLocalLinkEdits(document, html, edits, result);

        if (edits.Count == 0) {
            return html;
        }

        var fixedHtml = html;

        foreach (var edit in edits.OrderByDescending(x => x.Index).ThenByDescending(x => x.Sequence)) {
            fixedHtml = fixedHtml.Remove(edit.Index, edit.Length).Insert(edit.Index, edit.Text);
        }

        return fixedHtml;
    }

    private static void AddEmbedEdits(HtmlDocument document, List<Edit> edits, RichTextFixResult result) {
        var embeds = document.DocumentNode
                             .Descendants()
                             .Where(x => EmbedTags.Contains(x.Name))
                             .Where(x => !x.Ancestors().Any(IsEmbedOrHolder))
                             .ToList();

        foreach (var embed in embeds) {
            AddEdit(edits, embed.OuterStartIndex, 0, $"<span class=\"{EmbedHolderClass}\">");
            AddEdit(edits, embed.OuterStartIndex + embed.OuterLength, 0, "</span>");

            result.EmbedsWrapped++;
        }
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

    private static bool IsEmbedOrHolder(HtmlNode node) {
        return EmbedTags.Contains(node.Name) || node.HasClass(EmbedHolderClass);
    }

    private class Edit {
        public int Index { get; set; }
        public int Length { get; set; }
        public int Sequence { get; set; }
        public string Text { get; set; }
    }
}
