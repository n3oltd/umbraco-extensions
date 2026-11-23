using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace N3O.Umbraco.Blocks.Extensions;

public static class HtmlHelperExtensions {
    public static async Task<IHtmlContent> RenderBlockLayoutAsync(this IHtmlHelper html,
                                                                  IBlockViewModel blockViewModel,
                                                                  string layout) {
        var blockAlias = blockViewModel.Content.ContentType.Alias;

        return await html.RenderBlockLayoutAsync(blockAlias, layout, blockViewModel);
    }

    private static async Task<IHtmlContent> RenderBlockLayoutAsync(this IHtmlHelper html,
                                                                   string blockAlias,
                                                                   string layout,
                                                                   object model) {
        var viewPath = html.GetBlockLayoutViewPath(blockAlias, layout);

        return await html.PartialAsync(viewPath, model);
    }

    private static string GetBlockLayoutViewPath(this IHtmlHelper html, string blockAlias, string layout) {
        var layoutsPath = blockAlias.ToBlockViewsPath();
        var layoutName = layout.ToBlockLayoutName();

        if (layoutName != null) {
            var layoutViewPath = $"{layoutsPath}/{layoutName}.cshtml";
            var viewEngine = html.ViewContext.HttpContext.RequestServices.GetRequiredService<ICompositeViewEngine>();
            var layoutView = viewEngine.GetView(null, layoutViewPath, false);

            if (layoutView.Success) {
                return layoutViewPath;
            }
        }

        return $"{layoutsPath}/default.cshtml";
    }
}