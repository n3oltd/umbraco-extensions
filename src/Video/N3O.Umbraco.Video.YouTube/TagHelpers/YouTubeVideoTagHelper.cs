using Microsoft.AspNetCore.Razor.TagHelpers;
using N3O.Umbraco.Constants;
using N3O.Umbraco.Video.Extensions;

namespace N3O.Umbraco.Video.YouTube.TagHelpers;

[HtmlTargetElement($"{Prefixes.TagHelpers}youtube-video")]
public class YouTubeVideoTagHelper : TagHelper {
    private readonly YouTubeVideoPlatform _youTubeVideoPlatform;

    public YouTubeVideoTagHelper(YouTubeVideoPlatform youTubeVideoPlatform) {
        _youTubeVideoPlatform = youTubeVideoPlatform;
    }

    [HtmlAttributeName("video-url")]
    public string VideoUrl { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output) {
        if (_youTubeVideoPlatform.CanEmbed(VideoUrl)) {
            output.RenderVideoEmbed(_youTubeVideoPlatform.GetEmbed(VideoUrl));
        } else {
            output.SuppressOutput();
        }
    }
}
