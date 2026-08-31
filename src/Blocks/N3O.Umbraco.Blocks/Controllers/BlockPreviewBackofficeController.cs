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

    [HttpPost("previewGridBlocks")]
    public async Task<ActionResult<PreviewBlocksRes>> PreviewGridBlocks(
        [FromQuery(Name = "nodeKey")] Guid? contentId,
        [FromQuery(Name = "documentTypeKey")] Guid? contentTypeId,
        [FromQuery] string propertyAlias,
        [FromQuery] string culture) {
        var blockKeys = new List<Guid>();

        try {
            var req = await ReadRequestAsync();

            if (!req.HasAny(x => x.BlockKeys)) {
                return GetRes(new Dictionary<string, string>(), []);
            }

            blockKeys = req.BlockKeys.Distinct().ToList();

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
            var failed = new List<string>();

            foreach (var blockKey in blockKeys) {
                var preview = await PreviewBlockAsync(blockKey, publishedContent, propertyAlias, blockEditorData);

                markup[blockKey.ToString()] = preview.Markup;

                if (preview.Failed) {
                    failed.Add(blockKey.ToString());
                }
            }

            return GetRes(markup, failed);
        } catch (Exception ex) {
            var banner = ex is BlockPreviewException previewException
                             ? previewException.Markup
                             : new BlockPreviewErrorException(ex.Message).Markup;

            if (ex is not BlockPreviewException) {
                _logger.LogError(ex, "Failed to preview blocks for {NodeKey}", contentId);
            }

            return GetRes(blockKeys.ToDictionary(x => x.ToString(), _ => banner),
                          blockKeys.Select(x => x.ToString()).ToList());
        }
    }

    private static PreviewBlocksRes GetRes(Dictionary<string, string> markup, IEnumerable<string> failed) {
        var res = new PreviewBlocksRes();
        res.Markup = markup;
        res.Failed = failed;

        return res;
    }

    private async Task<(string Markup, bool Failed)> PreviewBlockAsync(
        Guid blockKey,
        IPublishedContent content,
        string propertyAlias,
        BlockEditorData<BlockGridValue, BlockGridLayoutItem> blockEditorData) {
        try {
            var markup = await _blockPreviewer.PreviewBlockAsync(blockKey, content, propertyAlias, blockEditorData);

            return (markup.CleanUpMarkupForPreview(), false);
        } catch (BlockPreviewException ex) {
            return (ex.Markup, true);
        } catch (Exception ex) {
            // The banner carries only the message, so without this the stack trace of a failing block is lost.
            _logger.LogError(ex, "Failed to preview block {BlockKey}", blockKey);

            return (new BlockPreviewErrorException(ex.Message).Markup, true);
        }
    }

    // Not model bound: BlockValue.Layout is typed as an interface, which MVC's formatter cannot deserialize
    // but Umbraco's IJsonSerializer can.
    private async Task<PreviewBlocksReq> ReadRequestAsync() {
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true)) {
            var json = await reader.ReadToEndAsync();

            return json.HasValue() ? _jsonSerializer.Deserialize<PreviewBlocksReq>(json) : null;
        }
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
