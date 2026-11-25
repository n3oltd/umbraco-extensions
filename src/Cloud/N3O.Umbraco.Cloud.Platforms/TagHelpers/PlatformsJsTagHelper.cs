using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using N3O.Umbraco.Constants;
using Umbraco.Extensions;

namespace N3O.Umbraco.Cloud.Platforms.TagHelpers;

[HtmlTargetElement($"{Prefixes.TagHelpers}platforms-js")]
public class PlatformsJsTagHelper : TagHelper {
    private readonly ICloudUrl _cloudUrl;
    private readonly ISubscriptionAccessor _subscriptionAccessor;

    public PlatformsJsTagHelper(ICloudUrl cloudUrl, ISubscriptionAccessor subscriptionAccessor) {
        _cloudUrl = cloudUrl;
        _subscriptionAccessor = subscriptionAccessor;
    }

    public override void Process(TagHelperContext context, TagHelperOutput output) {
        output.TagName = null;

        var scriptTag = new TagBuilder("script");
        scriptTag.Attributes.Add("type", "module");
        scriptTag.Attributes.Add("src", _cloudUrl.ForPlatforms(PlatformsConstants.PlatformsJs.Path));
        scriptTag.Attributes.Add("data-subscription", _subscriptionAccessor.GetSubscription().Id.Code);

        output.Content.AppendHtmlLine("<!-- N3O Platforms JS (https://n3o.ltd) -->");
        output.Content.AppendHtml(scriptTag.ToHtmlString());
    }
}
