using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Security;
using Perplex.ContentBlocks.Definitions;
using Perplex.ContentBlocks.Rendering;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Umbraco.Cms.Web.Common.Routing;

namespace N3O.Umbraco.Blocks.Perplex;

// No base member is virtual and the base calls its own RenderBlockAsync, so IContentBlocksRenderer is re-declared
// here and both loops are repeated to send every block, header included, through the catch below.
public class PerplexContentBlocksRenderer : ContentBlocksRenderer, IContentBlocksRenderer {
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PerplexContentBlocksRenderer> _logger;

    public PerplexContentBlocksRenderer(IContentBlockDefinitionRepository definitionRepository,
                                        IHttpContextAccessor httpContextAccessor,
                                        ILogger<PerplexContentBlocksRenderer> logger)
        : base(definitionRepository) {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public new async Task<IHtmlContent> RenderAsync(IContentBlocks contentBlocks,
                                                    RenderViewComponentAsync renderViewComponentAsync,
                                                    RenderPartialViewAsync renderPartialViewAsync,
                                                    bool isBackOfficePreview = false) {
        if (contentBlocks == null) {
            return HtmlString.Empty;
        }

        var headerHtml = await RenderBlockAsync(contentBlocks.Header,
                                                renderViewComponentAsync,
                                                renderPartialViewAsync,
                                                isBackOfficePreview);
        var blocksHtml = await RenderBlocksAsync(contentBlocks.Blocks,
                                                 renderViewComponentAsync,
                                                 renderPartialViewAsync,
                                                 isBackOfficePreview);

        var html = new HtmlContentBuilder();

        html.AppendHtml(headerHtml);
        html.AppendHtml(blocksHtml);

        return html;
    }

    public new async Task<IHtmlContent> RenderBlockAsync(IContentBlockViewModel block,
                                                         RenderViewComponentAsync renderViewComponentAsync,
                                                         RenderPartialViewAsync renderPartialViewAsync,
                                                         bool isBackOfficePreview = false) {
        if (block is IFailedPerplexBlockViewModel failedBlock) {
            return RenderFailedBlock(failedBlock, failedBlock.Exception);
        }

        try {
            return await base.RenderBlockAsync(block,
                                               renderViewComponentAsync,
                                               renderPartialViewAsync,
                                               isBackOfficePreview);
        } catch (Exception ex) {
            return RenderFailedBlock(block, ex);
        }
    }

    public new async Task<IHtmlContent> RenderBlocksAsync(IEnumerable<IContentBlockViewModel> blocks,
                                                          RenderViewComponentAsync renderViewComponentAsync,
                                                          RenderPartialViewAsync renderPartialViewAsync,
                                                          bool isBackOfficePreview = false) {
        if (blocks == null) {
            return HtmlString.Empty;
        }

        var html = new HtmlContentBuilder();

        foreach (var block in blocks) {
            var blockHtml = await RenderBlockAsync(block,
                                                   renderViewComponentAsync,
                                                   renderPartialViewAsync,
                                                   isBackOfficePreview);

            html.AppendHtml(blockHtml);
        }

        return html;
    }

    private IHtmlContent RenderFailedBlock(IContentBlockViewModel block, Exception exception) {
        var httpContext = _httpContextAccessor.HttpContext;
        var umbracoRouteValues = httpContext.Features.Get<UmbracoRouteValues>();

        _logger.LogError(exception,
                         "Failed to render block {ContentTypeAlias} {BlockId} on content {PublishedContentId}",
                         block.Content.ContentType.Alias,
                         block.Id,
                         umbracoRouteValues?.PublishedRequest.PublishedContent.Key);

        var backofficeUser = httpContext.RequestServices.GetRequiredService<IBackofficeUser>();

        if (backofficeUser.IsLoggedIn()) {
            return exception.ToHtml();
        } else {
            return HtmlString.Empty;
        }
    }
}
