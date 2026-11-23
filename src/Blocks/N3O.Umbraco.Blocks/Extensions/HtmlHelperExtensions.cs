using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace N3O.Umbraco.Blocks.Extensions;

public static class HtmlHelperExtensions {
    public static async Task<IHtmlContent> RenderBlockVariantAsync(this IHtmlHelper html,
                                                                   IBlockViewModel blockViewModel,
                                                                   string variant) {
        var blockAlias = blockViewModel.Content.ContentType.Alias;

        return await html.RenderBlockVariantAsync(blockAlias, variant, blockViewModel);
    }

    private static async Task<IHtmlContent> RenderBlockVariantAsync(this IHtmlHelper html,
                                                                    string blockAlias,
                                                                    string variant,
                                                                    object model) {
        var viewPath = html.GetBlockVariantViewPath(blockAlias, variant);

        return await html.PartialAsync(viewPath, model);
    }

    private static string GetBlockVariantViewPath(this IHtmlHelper html, string blockAlias, string variant) {
        var viewsPath = blockAlias.ToBlockViewsPath();
        var variantName = variant.ToBlockVariantName();

        if (variantName != null) {
            var variantViewPath = $"{viewsPath}/{variantName}.cshtml";
            var viewEngine = html.ViewContext.HttpContext.RequestServices.GetRequiredService<ICompositeViewEngine>();
            var variantView = viewEngine.GetView(null, variantViewPath, false);

            if (variantView.Success) {
                return variantViewPath;
            }
        }

        return $"{viewsPath}/default.cshtml";
    }
}