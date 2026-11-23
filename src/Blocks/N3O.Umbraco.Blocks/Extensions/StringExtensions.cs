using HtmlAgilityPack;
using Humanizer;
using System.IO;
using System.Linq;

namespace N3O.Umbraco.Blocks.Extensions;

public static class StringExtensions {
    public static string CleanUpMarkupForPreview(this string markup) {
        if (string.IsNullOrWhiteSpace(markup)) {
            return markup;
        }

        var content = new HtmlDocument();
        content.LoadHtml(markup);

        var links = content.DocumentNode.SelectNodes("//a");

        if (links != null) {
            foreach (var link in links) {
                if (!link.Attributes.Any(x => x.Name == "href" && x.Value.StartsWith("/media"))) {
                    link.SetAttributeValue("href", "javascript:;");
                }
            }
        }

        var formElements = content.DocumentNode.SelectNodes("//input | //textarea | //select | //button");
        
        if (formElements != null) {
            foreach (var formElement in formElements) {
                formElement.SetAttributeValue("disabled", "disabled");
            }
        }

        return content.DocumentNode.OuterHtml;
    }

    public static string ToBlockLayoutName(this string layout) {
        if (string.IsNullOrWhiteSpace(layout)) {
            return null;
        }

        var fileName = Path.GetFileNameWithoutExtension(layout);

        return fileName.Camelize();
    }
}