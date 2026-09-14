using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using N3O.Umbraco.Bundling.Models;
using N3O.Umbraco.Constants;
using N3O.Umbraco.Extensions;

namespace N3O.Umbraco.Bundling.TagHelpers;

[HtmlTargetElement($"{Prefixes.TagHelpers}js-bundle")]
public class JsBundleTagHelper : TagHelper {
    private readonly IAssetManifest _assetManifest;

    public JsBundleTagHelper(IAssetManifest assetManifest) {
        _assetManifest = assetManifest;
    }

    [HtmlAttributeName("name")]
    public string Name { get; set; } = BundlingConstants.Bundles.Main;

    public override void Process(TagHelperContext context, TagHelperOutput output) {
        var references = _assetManifest.GetJs(Name);

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
        var scriptTag = new TagBuilder("script");

        scriptTag.Attributes.Add("src", reference.Url);

        if (reference.Module) {
            scriptTag.Attributes.Add("type", "module");
        }

        if (reference.Integrity.HasValue()) {
            scriptTag.Attributes.Add("integrity", reference.Integrity);
            scriptTag.Attributes.Add("crossorigin", "anonymous");
        }

        return scriptTag;
    }
}
