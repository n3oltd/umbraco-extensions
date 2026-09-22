using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using N3O.Umbraco.Constants;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Video.Extensions;
using Umbraco.Extensions;

namespace N3O.Umbraco.Video.TagHelpers;

[HtmlTargetElement($"{Prefixes.TagHelpers}video-embed")]
public class VideoEmbedTagHelper : TagHelper {
    private const string WrapperStyle = "position: relative; width: 100%; height: 0; padding-bottom: 56.25%;";
    private const string InnerStyle = "position: absolute; top: 0; left: 0; width: 100%; height: 100%; z-index: 1;";

    [HtmlAttributeName("url")]
    public string Url { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output) {
        var embedUrl = Url.GetYouTubeEmbedUrl()
                       ?? Url.GetVimeoEmbedUrl()
                       ?? Url.GetFacebookEmbedUrl()
                       ?? Url.GetDailymotionEmbedUrl()
                       ?? Url.GetTikTokEmbedUrl()
                       ?? Url.GetInstagramEmbedUrl()
                       ?? Url.GetLoomEmbedUrl()
                       ?? Url.GetStreamableEmbedUrl();

        if (embedUrl.HasValue()) {
            RenderIframe(output, embedUrl);

            return;
        }

        if (Url.IsDirectVideoFileUrl()) {
            RenderVideo(output, Url);

            return;
        }

        output.SuppressOutput();
    }

    private static void RenderIframe(TagHelperOutput output, string src) {
        var iframe = new TagBuilder("iframe");

        foreach (var attribute in output.Attributes) {
            iframe.Attributes[attribute.Name] = attribute.Value?.ToString();
        }

        iframe.Attributes["src"] = src;

        if (!iframe.Attributes.ContainsKey("frameborder")) {
            iframe.Attributes["frameborder"] = "0";
        }

        if (!iframe.Attributes.ContainsKey("allowfullscreen")) {
            iframe.Attributes["allowfullscreen"] = "true";
        }

        if (!iframe.Attributes.ContainsKey("style")) {
            iframe.Attributes["style"] = InnerStyle;
        }

        Wrap(output, iframe);
    }

    private static void RenderVideo(TagHelperOutput output, string src) {
        var video = new TagBuilder("video");

        foreach (var attribute in output.Attributes) {
            video.Attributes[attribute.Name] = attribute.Value?.ToString();
        }

        video.Attributes["src"] = src;

        if (!video.Attributes.ContainsKey("controls")) {
            video.Attributes["controls"] = "true";
        }

        if (!video.Attributes.ContainsKey("style")) {
            video.Attributes["style"] = InnerStyle;
        }

        Wrap(output, video);
    }

    private static void Wrap(TagHelperOutput output, TagBuilder inner) {
        output.TagName = "div";
        output.Attributes.Clear();
        output.Attributes.Add("style", WrapperStyle);
        output.Content.SetHtmlContent(inner.ToHtmlString());
    }
}
