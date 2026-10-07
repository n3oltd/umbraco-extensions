using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Constants;
using Umbraco.Extensions;

namespace N3O.Umbraco.Cloud.Platforms.TagHelpers;

[HtmlTargetElement($"{Prefixes.TagHelpers}platforms-js-preload")]
public class PlatformsJsPreloadTagHelper : TagHelper {
    private readonly ICloudUrl _cloudUrl;

    public PlatformsJsPreloadTagHelper(ICloudUrl cloudUrl) {
        _cloudUrl = cloudUrl;
    }

    public override void Process(TagHelperContext context, TagHelperOutput output) {
        output.TagName = null;

        var linkTag = new TagBuilder("link");
        linkTag.Attributes.Add("rel", "modulepreload");
        linkTag.Attributes.Add("href", _cloudUrl.ForCdn(CdnRoots.Connect, "platforms-js/platforms.js"));

        linkTag.TagRenderMode = TagRenderMode.SelfClosing;

        output.Content.AppendHtml(linkTag.ToHtmlString());
    }
}
