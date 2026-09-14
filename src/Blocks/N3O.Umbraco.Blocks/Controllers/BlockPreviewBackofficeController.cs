using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.Blocks.Exceptions;
using N3O.Umbraco.Blocks.Extensions;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Hosting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace N3O.Umbraco.Blocks.Controllers;

public class BlockPreviewBackofficeController : BackofficeAuthorizedApiController {
    private readonly IPublishedRouter _publishedRouter;
    private readonly IBlockPreviewer _blockPreviewer;
    private readonly ILanguageService _languageService;
    private readonly IVariationContextAccessor _variationContextAccessor;
    private readonly IContentLocator _contentLocator;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly IContentTypeService _contentTypeService;
    private readonly ILogger<BlockPreviewBackofficeController> _logger;

    public BlockPreviewBackofficeController(IPublishedRouter publishedRouter,
                                            IBlockPreviewer blockPreviewer,
                                            ILanguageService languageService,
                                            IVariationContextAccessor variationContextAccessor,
                                            IContentLocator contentLocator,
                                            IUmbracoContextAccessor umbracoContextAccessor,
                                            IJsonSerializer jsonSerializer,
                                            IContentTypeService contentTypeService,
                                            ILogger<BlockPreviewBackofficeController> logger) {
        _publishedRouter = publishedRouter;
        _blockPreviewer = blockPreviewer;
        _languageService = languageService;
        _variationContextAccessor = variationContextAccessor;
        _contentLocator = contentLocator;
        _umbracoContextAccessor = umbracoContextAccessor;
        _jsonSerializer = jsonSerializer;
        _contentTypeService = contentTypeService;
        _logger = logger;
    }

    // Previews are requested for a whole document at once. Every block on a page previews from the same grid
    // value, so sending and converting it once is the difference between one request and one conversion for the
    // document and one of each per block.
    [HttpPost("previewGridBlocks")]
    public async Task<IActionResult> PreviewGridBlocks([FromQuery(Name = "nodeKey")] Guid? contentId,
                                                       [FromQuery(Name = "documentTypeKey")] Guid? contentTypeId,
                                                       [FromQuery] string propertyAlias,
                                                       [FromQuery] string culture) {
        var req = await ReadRequestAsync();

        if (req?.BlockKeys.HasAny() != true) {
            return Ok(new Dictionary<string, string>());
        }

        var blockKeys = req.BlockKeys.Distinct().ToList();

        try {
            var publishedContent = GetPublishedContent(contentId, contentTypeId);

            if (publishedContent == null) {
                throw new BlockPreviewWarningException("No published content found");
            }

            await SetCultureAsync(publishedContent, culture);
            await SetupPublishedRequest(publishedContent);

            var blockEditorData = req.BlockValue.ToEditorData(_jsonSerializer, _contentTypeService);

            if (blockEditorData == null) {
                throw new BlockPreviewErrorException("The block data is invalid");
            }

            var markup = new Dictionary<string, string>();

            foreach (var blockKey in blockKeys) {
                markup[blockKey.ToString()] = await PreviewBlockAsync(blockKey,
                                                                      publishedContent,
                                                                      propertyAlias,
                                                                      blockEditorData);
            }

            return Ok(markup);
        } catch (Exception ex) {
            // Anything thrown out here applies to the request as a whole, so every block shows the same banner.
            var banner = ex is BlockPreviewException previewException
                             ? previewException.Markup
                             : new BlockPreviewErrorException(ex.Message).Markup;

            if (ex is not BlockPreviewException) {
                _logger.LogError(ex, "Failed to preview blocks for {NodeKey}", contentId);
            }

            return Ok(blockKeys.ToDictionary(x => x.ToString(), _ => banner));
        }
    }

    // One block failing to render says nothing about the rest, so each is caught on its own and the others
    // still return markup.
    private async Task<string> PreviewBlockAsync(Guid blockKey,
                                                 IPublishedContent content,
                                                 string propertyAlias,
                                                 BlockEditorData<BlockGridValue, BlockGridLayoutItem> blockEditorData) {
        try {
            var markup = await _blockPreviewer.PreviewBlockAsync(blockKey, content, propertyAlias, blockEditorData);

            return markup.CleanUpMarkupForPreview();
        } catch (BlockPreviewException ex) {
            return ex.Markup;
        } catch (Exception ex) {
            // The banner carries only the message, so without this the stack trace of a failing block is lost.
            _logger.LogError(ex, "Failed to preview block {BlockKey}", blockKey);

            return new BlockPreviewErrorException(ex.Message).Markup;
        }
    }

    // The body is read as raw JSON rather than model bound. BlockValue.Layout is typed as an interface, which
    // MVC's System.Text.Json formatter cannot deserialize; Umbraco's own IJsonSerializer carries the
    // JsonBlockValueConverter that can. Binding would therefore throw in the formatter, before this action runs,
    // where the catch above could not turn it into an error banner.
    private async Task<PreviewBlocksReq> ReadRequestAsync() {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);

        var json = await reader.ReadToEndAsync();

        return json.HasValue() ? _jsonSerializer.Deserialize<PreviewBlocksReq>(json) : null;
    }

    private async Task SetupPublishedRequest(IPublishedContent content = null) {
        var context = _umbracoContextAccessor.GetRequiredUmbracoContext();

        var requestUrl = new Uri(Request.GetDisplayUrl());
        var requestBuilder = await _publishedRouter.CreateRequestAsync(requestUrl);

        if (content != null) {
            requestBuilder.SetPublishedContent(content);
        }

        context.PublishedRequest = requestBuilder.Build();
    }

    // A document that has never been published is not in the published cache, so it cannot be routed against
    // itself. Any published document of the same type will do, as the preview only needs a page to render in.
    private IPublishedContent GetPublishedContent(Guid? contentId, Guid? contentTypeId) {
        var content = contentId.IfNotNull(x => _contentLocator.ById(x));

        if (content != null) {
            return content;
        }

        var contentType = contentTypeId.IfNotNull(x => _contentTypeService.Get(x));

        return contentType != null ? _contentLocator.All(contentType.Alias).FirstOrDefault() : null;
    }

    private async Task SetCultureAsync(IPublishedContent content, string culture) {
        var currentCulture = culture.HasValue() ? culture : content?.GetCultureFromDomains();

        if (!currentCulture.HasValue() || currentCulture == "undefined") {
            var defaultLanguage = await _languageService.GetDefaultLanguageAsync();
            currentCulture = defaultLanguage?.IsoCode;
        }

        _variationContextAccessor.VariationContext = new VariationContext(currentCulture);

        var cultureInfo = new CultureInfo(currentCulture);
        Thread.CurrentThread.CurrentCulture = cultureInfo;
        Thread.CurrentThread.CurrentUICulture = cultureInfo;
    }
}
