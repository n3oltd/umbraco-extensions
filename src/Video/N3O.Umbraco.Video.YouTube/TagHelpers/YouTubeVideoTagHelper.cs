using Microsoft.AspNetCore.Razor.TagHelpers;
using N3O.Umbraco.Constants;
using N3O.Umbraco.Video.Extensions;

namespace N3O.Umbraco.Video.YouTube.TagHelpers;

[HtmlTargetElement($"{Prefixes.TagHelpers}youtube-video")]
public class YouTubeVideoTagHelper : TagHelper {
    [HtmlAttributeName("video-url")]
    public string VideoUrl { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output) {
        var videoPlatform = new YouTubeVideoPlatform();

        if (videoPlatform.CanEmbed(VideoUrl)) {
            output.RenderVideoEmbed(videoPlatform.GetEmbed(VideoUrl));
        } else {
            output.SuppressOutput();
        }
    }
}
