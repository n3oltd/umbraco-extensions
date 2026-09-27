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
using Umbraco.Cms.Core.Cache.PropertyEditors;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;
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
    private readonly IContentHelper _contentHelper;
    private readonly IBlockEditorElementTypeCache _blockEditorElementTypeCache;
    private readonly ILogger<BlockPreviewBackofficeController> _logger;

    public BlockPreviewBackofficeController(IPublishedRouter publishedRouter,
                                            IBlockPreviewer blockPreviewer,
                                            ILanguageService languageService,
                                            IVariationContextAccessor variationContextAccessor,
                                            IContentLocator contentLocator,
                                            IUmbracoContextAccessor umbracoContextAccessor,
                                            IJsonSerializer jsonSerializer,
                                            IContentTypeService contentTypeService,
                                            IContentHelper contentHelper,
                                            IBlockEditorElementTypeCache blockEditorElementTypeCache,
                                            ILogger<BlockPreviewBackofficeController> logger) {
        _publishedRouter = publishedRouter;
        _blockPreviewer = blockPreviewer;
        _languageService = languageService;
        _variationContextAccessor = variationContextAccessor;
        _contentLocator = contentLocator;
        _umbracoContextAccessor = umbracoContextAccessor;
        _jsonSerializer = jsonSerializer;
        _contentTypeService = contentTypeService;
        _contentHelper = contentHelper;
        _blockEditorElementTypeCache = blockEditorElementTypeCache;
        _logger = logger;
    }

    [HttpPost("previewGridBlocks")]
    public async Task<ActionResult<PreviewBlocksRes>> PreviewGridBlocks(
        [FromQuery(Name = "nodeKey")] Guid? contentId,
        [FromQuery(Name = "documentTypeKey")] Guid? contentTypeId,
        [FromQuery] string propertyAlias,
        [FromQuery] string culture,
        CancellationToken cancellationToken) {
        var blockKeys = new List<Guid>();

        try {
            var req = await ReadRequestAsync();

            if (!req.HasAny(x => x.BlockKeys)) {
                return GetRes(new Dictionary<string, string>());
            }

            blockKeys = req.BlockKeys.Distinct().ToList();

            var publishedContent = GetPublishedContent(contentId, contentTypeId);

            if (publishedContent == null) {
                throw new BlockPreviewWarningException("No published content found");
            }

            await SetCultureAsync(publishedContent, culture);
            await SetupPublishedRequest(publishedContent);

            var blockEditorData = req.BlockValue.ToEditorData(_jsonSerializer, _blockEditorElementTypeCache, _logger);

            if (blockEditorData == null) {
                throw new BlockPreviewErrorException("The block data is invalid");
            }

            var blockGridModel = GetBlockGridModel(publishedContent, propertyAlias, blockEditorData);
            var markup = new Dictionary<string, string>();

            foreach (var blockKey in blockKeys) {
                // The editor aborts a request whose context has changed and will not read its reply.
                if (cancellationToken.IsCancellationRequested) {
                    break;
                }

                markup[blockKey.ToString()] = await PreviewBlockAsync(blockKey,
                                                                      publishedContent,
                                                                      propertyAlias,
                                                                      blockGridModel,
                                                                      blockEditorData);
            }

            return GetRes(markup);
        } catch (Exception ex) {
            var banner = ex is BlockPreviewException previewException
                             ? previewException.Markup
                             : new BlockPreviewErrorException(ex.Message).Markup;

            if (ex is not BlockPreviewException) {
                _logger.LogError(ex,
                                 "Failed to preview blocks of {PropertyAlias} for {NodeKey}",
                                 propertyAlias,
                                 contentId);
            }

            return GetRes(blockKeys.ToDictionary(x => x.ToString(), _ => banner));
        }
    }

    private BlockGridModel GetBlockGridModel(IPublishedContent content,
                                             string propertyAlias,
                                             BlockEditorData<BlockGridValue, BlockGridLayoutItem> blockEditorData) {
        var json = _jsonSerializer.Serialize(blockEditorData.BlockValue);

        var blockGridModel = _contentHelper.GetConvertedValue<BlockGridPropertyValueConverter, BlockGridModel>(
            content.ContentType.Alias,
            propertyAlias,
            json,
            content);

        // A block grid nested inside another block is a property of that block's element type, not the document's.
        if (blockGridModel == null) {
            throw new BlockPreviewWarningException($"Property {propertyAlias.Quote()} is not a block grid on " +
                                                   $"{content.ContentType.Alias.Quote()}");
        }

        return blockGridModel;
    }

    private static PreviewBlocksRes GetRes(Dictionary<string, string> markup) {
        var res = new PreviewBlocksRes();
        res.Markup = markup;

        return res;
    }

    private async Task<string> PreviewBlockAsync(Guid blockKey,
                                                 IPublishedContent content,
                                                 string propertyAlias,
                                                 BlockGridModel blockGridModel,
                                                 BlockEditorData<BlockGridValue, BlockGridLayoutItem> blockEditorData) {
        try {
            var markup = await _blockPreviewer.PreviewBlockAsync(blockKey, content, blockGridModel, blockEditorData);

            return markup.CleanUpMarkupForPreview();
        } catch (BlockPreviewException ex) {
            return ex.Markup;
        } catch (Exception ex) {
            _logger.LogError(ex,
                             "Failed to preview block {BlockKey} of {PropertyAlias} on {NodeKey}",
                             blockKey,
                             propertyAlias,
                             content.Key);

            return new BlockPreviewErrorException(ex.Message).Markup;
        }
    }

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
