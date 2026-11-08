using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.Security;
using Perplex.ContentBlocks.Definitions;
using Perplex.ContentBlocks.Rendering;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace N3O.Umbraco.Blocks.Perplex;

// The new members are reached only through the re-declared IContentBlocksRenderer, and the base's own loops would
// bypass them.
public class PerplexContentBlocksRenderer : ContentBlocksRenderer, IContentBlocksRenderer {
    private readonly ILogger<PerplexContentBlocksRenderer> _logger;
    private readonly IRenderFailures _renderFailures;

    public PerplexContentBlocksRenderer(IContentBlockDefinitionRepository definitionRepository,
                                        ILogger<PerplexContentBlocksRenderer> logger,
                                        IRenderFailures renderFailures)
        : base(definitionRepository) {
        _logger = logger;
        _renderFailures = renderFailures;
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
            return _renderFailures.ToHtml(failedBlock.Exception);
        }

        try {
            return await base.RenderBlockAsync(block,
                                               renderViewComponentAsync,
                                               renderPartialViewAsync,
                                               isBackOfficePreview);
        } catch (Exception ex) when (_renderFailures.CanIsolate(ex)) {
            _logger.LogError(ex,
                             "Failed to render block {ContentTypeAlias} {BlockId}",
                             block.Content.ContentType.Alias,
                             block.Id);

            return _renderFailures.ToHtml(ex);
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
}
