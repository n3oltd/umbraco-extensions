using Microsoft.AspNetCore.Razor.TagHelpers;
using N3O.Umbraco.Constants;
using N3O.Umbraco.Video.Extensions;

namespace N3O.Umbraco.Video.TagHelpers;

[HtmlTargetElement($"{Prefixes.TagHelpers}video")]
public class VideoTagHelper : TagHelper {
    private readonly IVideoPlatformFactory _videoPlatformFactory;

    public VideoTagHelper(IVideoPlatformFactory videoPlatformFactory) {
        _videoPlatformFactory = videoPlatformFactory;
    }

    [HtmlAttributeName("video-url")]
    public string VideoUrl { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output) {
        var videoPlatform = _videoPlatformFactory.GetPlatform(VideoUrl);

        if (videoPlatform == null) {
            output.SuppressOutput();
        } else {
            output.RenderVideoEmbed(videoPlatform.GetEmbed(VideoUrl));
        }
    }
}
