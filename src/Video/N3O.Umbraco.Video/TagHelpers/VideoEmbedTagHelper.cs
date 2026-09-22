using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using N3O.Umbraco.Constants;
using System.Net;
using System.Text.RegularExpressions;
using Umbraco.Extensions;

namespace N3O.Umbraco.Video.TagHelpers;

[HtmlTargetElement($"{Prefixes.TagHelpers}video-embed")]
public class VideoEmbedTagHelper : TagHelper {
    private const string WrapperStyle = "position: relative; width: 100%; height: 0; padding-bottom: 56.25%;";
    private const string InnerStyle = "position: absolute; top: 0; left: 0; width: 100%; height: 100%; z-index: 1;";

    private static readonly Regex YouTube = new(
        @"((?<=(v|V)/)|(?<=be/)|(?<=(\?|\&)v=)|(?<=embed/)|(?<=shorts/)|(?<=live/))([\w-]+)",
        RegexOptions.Compiled);

    private static readonly Regex Vimeo = new(
        @"vimeo\.com/(?:video/|channels/[\w-]+/|groups/[\w-]+/videos/)?(\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex Dailymotion = new(
        @"(?:dailymotion\.com/(?:embed/)?video/|dai\.ly/)([a-zA-Z0-9]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex Facebook = new(
        @"facebook\.com/(?:[^/]+/videos/|watch/?\?v=|share/v/|video\.php\?v=|reel/)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex DirectFile = new(
        @"\.(mp4|webm|ogg|m4v)(\?.*)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    [HtmlAttributeName("url")]
    public string Url { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output) {
        if (Url.IsNullOrWhiteSpace()) {
            output.SuppressOutput();

            return;
        }

        var (tagName, src) = Resolve(Url);

        if (tagName == null) {
            output.SuppressOutput();

            return;
        }

        var inner = new TagBuilder(tagName);

        foreach (var attribute in output.Attributes) {
            inner.Attributes[attribute.Name] = attribute.Value?.ToString();
        }

        inner.Attributes["src"] = src;

        if (tagName == "iframe") {
            if (!inner.Attributes.ContainsKey("frameborder")) {
                inner.Attributes["frameborder"] = "0";
            }

            if (!inner.Attributes.ContainsKey("allowfullscreen")) {
                inner.Attributes["allowfullscreen"] = "true";
            }
        } else if (tagName == "video" && !inner.Attributes.ContainsKey("controls")) {
            inner.Attributes["controls"] = "true";
        }

        if (!inner.Attributes.ContainsKey("style")) {
            inner.Attributes["style"] = InnerStyle;
        }

        output.TagName = "div";
        output.Attributes.Clear();
        output.Attributes.Add("style", WrapperStyle);
        output.Content.SetHtmlContent(inner.ToHtmlString());
    }

    private static (string TagName, string Src) Resolve(string url) {
        var youTubeMatch = YouTube.Match(url);

        if (youTubeMatch.Success) {
            var host = url.InvariantContains("youtube-nocookie.com")
                           ? "https://www.youtube-nocookie.com"
                           : "https://www.youtube.com";

            return ("iframe", $"{host}/embed/{youTubeMatch.Groups[0].Value}?enablejsapi=1");
        }

        var vimeoMatch = Vimeo.Match(url);

        if (vimeoMatch.Success) {
            return ("iframe", $"https://player.vimeo.com/video/{vimeoMatch.Groups[1].Value}");
        }

        var dailymotionMatch = Dailymotion.Match(url);

        if (dailymotionMatch.Success) {
            return ("iframe", $"https://www.dailymotion.com/embed/video/{dailymotionMatch.Groups[1].Value}");
        }

        if (Facebook.IsMatch(url)) {
            return ("iframe", $"https://www.facebook.com/plugins/video.php?href={WebUtility.UrlEncode(url)}&show_text=false");
        }

        if (DirectFile.IsMatch(url)) {
            return ("video", url);
        }

        return (null, null);
    }
}
