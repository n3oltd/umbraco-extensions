using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using N3O.Umbraco.Cloud.Extensions;
using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Extensions;

namespace N3O.Umbraco.Cloud.Platforms.TagHelpers;

public class PlatformsTagHelperComponent : TagHelperComponent {
    public override void Process(TagHelperContext context, TagHelperOutput output) {
        if (context.TagName.EqualsInvariant("head")) {
            var metaTag = new TagBuilder("meta");
            metaTag.MergeAttribute("name", PlatformsMetaTagName.CheckoutSameSite.ToEnumString());
            metaTag.MergeAttribute("content", "true");

            metaTag.TagRenderMode = TagRenderMode.SelfClosing;
            
            output.PostContent.AppendHtml(metaTag);
        }
    }
}
