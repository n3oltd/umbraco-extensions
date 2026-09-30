using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using N3O.Umbraco.Bundling.Models;
using N3O.Umbraco.Constants;
using N3O.Umbraco.Extensions;

namespace N3O.Umbraco.Bundling.TagHelpers;

[HtmlTargetElement($"{Prefixes.TagHelpers}css-bundle")]
public class CssBundleTagHelper : TagHelper {
    private readonly IAssetManifest _assetManifest;

    public CssBundleTagHelper(IAssetManifest assetManifest) {
        _assetManifest = assetManifest;
    }

    [HtmlAttributeName("name")]
    public string Name { get; set; } = BundlingConstants.Bundles.Main;

    public override void Process(TagHelperContext context, TagHelperOutput output) {
        var references = _assetManifest.GetCss(Name);

        if (references.None()) {
            output.SuppressOutput();

            return;
        }

        output.TagName = null;

        foreach (var reference in references) {
            output.Content.AppendHtml(BuildTag(reference));
        }
    }

    private TagBuilder BuildTag(AssetReference reference) {
        var linkTag = new TagBuilder("link");

        linkTag.TagRenderMode = TagRenderMode.SelfClosing;
        linkTag.Attributes.Add("rel", "stylesheet");
        linkTag.Attributes.Add("href", reference.Url);

        if (reference.Integrity.HasValue()) {
            linkTag.Attributes.Add("integrity", reference.Integrity);
            linkTag.Attributes.Add("crossorigin", "anonymous");
        }

        return linkTag;
    }
}
