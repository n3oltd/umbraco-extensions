using AngleSharp.Html;
using HtmlAgilityPack;
using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Extensions;
using System;
using System.Text;

namespace N3O.Umbraco.Cloud.Platforms.Extensions;

public static class HtmlStringExtensions {
    public static RichTextContentReq ToRichTextContentReq(this string src, IPlatformsMediaUrlBuilder mediaUrlBuilder) {
        if (!src.HasValue()) {
            return null;
        }

        var req = new RichTextContentReq();
        req.Html = MakeUrlsAbsolute(src, mediaUrlBuilder);

        return req;
    }

    // Platforms renders this HTML away from the site, where root-relative URLs resolve against the wrong host.
    private static string MakeUrlsAbsolute(string html, IPlatformsMediaUrlBuilder mediaUrlBuilder) {
        var document = new HtmlDocument();
        document.LoadHtml(html);

        var nodes = document.DocumentNode.SelectNodes("//a[@href] | //img[@src or @srcset] | //source[@srcset]");

        if (nodes == null) {
            return html;
        }

        var changed = false;

        foreach (var node in nodes) {
            foreach (var attribute in node.Attributes) {
                var absoluteValue = GetAbsoluteValue(node.Name, attribute, mediaUrlBuilder);

                if (absoluteValue != attribute.Value) {
                    attribute.Value = absoluteValue;
                    changed = true;
                }
            }
        }

        return changed ? document.DocumentNode.OuterHtml : html;
    }

    private static string GetAbsoluteValue(string elementName,
                                           HtmlAttribute attribute,
                                           IPlatformsMediaUrlBuilder mediaUrlBuilder) {
        if ((elementName == "a" && attribute.Name == "href") || (elementName == "img" && attribute.Name == "src")) {
            return GetAbsoluteUrl(attribute.Value, mediaUrlBuilder);
        } else if ((elementName == "img" || elementName == "source") && attribute.Name == "srcset") {
            return GetAbsoluteSourceSet(attribute.Value, mediaUrlBuilder);
        } else {
            return attribute.Value;
        }
    }

    private static string GetAbsoluteSourceSet(string srcset, IPlatformsMediaUrlBuilder mediaUrlBuilder) {
        var sourceSet = new StringBuilder();
        var position = 0;

        foreach (var candidate in SourceSet.Parse(srcset)) {
            var urlPosition = srcset.IndexOf(candidate.Url, position, StringComparison.Ordinal);

            sourceSet.Append(srcset, position, urlPosition - position);
            sourceSet.Append(GetAbsoluteUrl(candidate.Url, mediaUrlBuilder));

            position = urlPosition + candidate.Url.Length;
        }

        sourceSet.Append(srcset, position, srcset.Length - position);

        return sourceSet.ToString();
    }

    private static string GetAbsoluteUrl(string url, IPlatformsMediaUrlBuilder mediaUrlBuilder) {
        if (url.StartsWith('/') && !url.StartsWith("//")) {
            return mediaUrlBuilder.Build(url);
        } else {
            return url;
        }
    }
}
