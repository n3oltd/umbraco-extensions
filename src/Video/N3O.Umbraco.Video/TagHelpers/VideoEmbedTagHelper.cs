using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using N3O.Umbraco.Constants;
using Umbraco.Extensions;

namespace N3O.Umbraco.Video.TagHelpers;

[HtmlTargetElement($"{Prefixes.TagHelpers}video-embed")]
public class VideoEmbedTagHelper : TagHelper {
    private const string WrapperStyle = "position: relative; width: 100%; height: 0; padding-bottom: 56.25%;";
    private const string InnerStyle = "position: absolute; top: 0; left: 0; width: 100%; height: 100%; z-index: 1;";

    private readonly IVideoEmbedResolver _resolver;

    public VideoEmbedTagHelper(IVideoEmbedResolver resolver) {
        _resolver = resolver;
    }

    [HtmlAttributeName("url")]
    public string Url { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output) {
        var embed = _resolver.Resolve(Url);

        if (embed == null) {
            output.SuppressOutput();

            return;
        }

        var inner = new TagBuilder(embed.TagName);

        foreach (var attribute in output.Attributes) {
            inner.Attributes[attribute.Name] = attribute.Value?.ToString();
        }

        inner.Attributes["src"] = embed.Src;

        foreach (var attribute in embed.DefaultAttributes) {
            if (!inner.Attributes.ContainsKey(attribute.Key)) {
                inner.Attributes[attribute.Key] = attribute.Value;
            }
        }

        if (!inner.Attributes.ContainsKey("style")) {
            inner.Attributes["style"] = InnerStyle;
        }

        output.TagName = "div";
        output.Attributes.Clear();
        output.Attributes.Add("style", WrapperStyle);
        output.Content.SetHtmlContent(inner.ToHtmlString());
    }
}
