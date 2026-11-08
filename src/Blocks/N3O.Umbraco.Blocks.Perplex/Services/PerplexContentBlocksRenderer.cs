using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.Security;
using Perplex.ContentBlocks.Definitions;
using Perplex.ContentBlocks.Rendering;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace N3O.Umbraco.Blocks.Perplex;

public class PerplexContentBlocksRenderer : IContentBlocksRenderer {
    private readonly ContentBlocksRenderer _contentBlocksRenderer;
    private readonly ILogger<PerplexContentBlocksRenderer> _logger;
    private readonly IRenderFailures _renderFailures;

    public PerplexContentBlocksRenderer(IContentBlockDefinitionRepository definitionRepository,
                                        ILogger<PerplexContentBlocksRenderer> logger,
                                        IRenderFailures renderFailures) {
        _contentBlocksRenderer = new ContentBlocksRenderer(definitionRepository);
        _logger = logger;
        _renderFailures = renderFailures;
    }

    public async Task<IHtmlContent> RenderAsync(IContentBlocks contentBlocks,
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

    public async Task<IHtmlContent> RenderBlockAsync(IContentBlockViewModel block,
                                                     RenderViewComponentAsync renderViewComponentAsync,
                                                     RenderPartialViewAsync renderPartialViewAsync,
                                                     bool isBackOfficePreview = false) {
        if (block is IFailedPerplexBlockViewModel failedBlock) {
            return RenderFailure(block, failedBlock.Exception, isBackOfficePreview);
        }

        try {
            return await _contentBlocksRenderer.RenderBlockAsync(block,
                                                                 renderViewComponentAsync,
                                                                 renderPartialViewAsync,
                                                                 isBackOfficePreview);
        } catch (Exception ex) when (_renderFailures.CanIsolate(ex)) {
            _logger.LogError(ex,
                             "Failed to render block {ContentTypeAlias} {BlockId}",
                             block.Content.ContentType.Alias,
                             block.Id);

            return RenderFailure(block, ex, isBackOfficePreview);
        }
    }

    public async Task<IHtmlContent> RenderBlocksAsync(IEnumerable<IContentBlockViewModel> blocks,
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

    // The back-office preview scrolls to a block by this anchor's id, and Perplex writes the anchor only for a block
    // that rendered.
    private IHtmlContent RenderFailure(IContentBlockViewModel block, Exception exception, bool isBackOfficePreview) {
        var html = new HtmlContentBuilder();

        if (isBackOfficePreview) {
            html.AppendHtml($"<a id=\"{block.Id}\" class=\"perplex-content-blocks-preview-anchor\"></a>");
        }

        html.AppendHtml(_renderFailures.ToHtml(exception));

        return html;
    }
}
