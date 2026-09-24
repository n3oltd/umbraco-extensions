using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using N3O.Umbraco.Video.Models;
using System.Globalization;
using Umbraco.Extensions;

namespace N3O.Umbraco.Video.Extensions;

public static class TagHelperOutputExtensions {
    public static void RenderVideoEmbed(this TagHelperOutput output, VideoEmbed videoEmbed) {
        var iframeTag = new TagBuilder("iframe");

        foreach (var attribute in output.Attributes) {
            iframeTag.Attributes[attribute.Name] = attribute.Value?.ToString();
        }

        iframeTag.Attributes["src"] = videoEmbed.Url;

        if (!iframeTag.Attributes.ContainsKey("frameborder")) {
            iframeTag.Attributes["frameborder"] = "0";
        }

        if (!iframeTag.Attributes.ContainsKey("allowfullscreen")) {
            iframeTag.Attributes["allowfullscreen"] = "true";
        }

        if (!iframeTag.Attributes.ContainsKey("style")) {
            iframeTag.Attributes["style"] = "position: absolute; top: 0; left: 0; width: 100%; height: 100%; z-index: 1;";
        }

        var paddingBottomPercentage = videoEmbed.AspectRatioHeight * 100m / videoEmbed.AspectRatioWidth;
        var paddingBottom = paddingBottomPercentage.ToString("0.####", CultureInfo.InvariantCulture);
        var ratioStyle = $"position: relative; width: 100%; height: 0; padding-bottom: {paddingBottom}%;";

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.Clear();

        if (videoEmbed.MaxWidth.HasValue) {
            var ratioTag = new TagBuilder("div");
            ratioTag.Attributes["style"] = ratioStyle;
            ratioTag.InnerHtml.AppendHtml(iframeTag);

            output.Attributes.Add("style", $"max-width: {videoEmbed.MaxWidth.Value}px; margin: 0 auto;");
            output.Content.SetHtmlContent(ratioTag.ToHtmlString());
        } else {
            output.Attributes.Add("style", ratioStyle);
            output.Content.SetHtmlContent(iframeTag.ToHtmlString());
        }
    }
}
