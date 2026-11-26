using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Perplex.ContentBlocks.Rendering;
using System.Threading.Tasks;

namespace N3O.Umbraco.Blocks.Perplex.Extensions;

public static class HtmlHelperExtensions {
    public static Task<IHtmlContent> RenderContentBlocksAsync(this IHtmlHelper html,
                                                              IContentBlocks contentBlocks,
                                                              IContentBlocksRenderer renderer) {
        var viewComponentHelper = html.ViewContext
                                      .HttpContext
                                      .RequestServices
                                      .GetRequiredService<IViewComponentHelper>();

        // A view component helper taken from the container is not bound to any view until it is contextualized
        ((IViewContextAware) viewComponentHelper).Contextualize(html.ViewContext);

        return renderer.RenderAsync(contentBlocks,
                                    (componentType, arguments) =>
                                        viewComponentHelper.InvokeAsync(componentType, arguments),
                                    (partialViewName, model) => html.PartialAsync(partialViewName, model),
                                    isBackOfficePreview: false);
    }
}
