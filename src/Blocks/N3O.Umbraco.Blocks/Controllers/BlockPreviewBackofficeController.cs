using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using N3O.Umbraco.Blocks.Exceptions;
using N3O.Umbraco.Blocks.Extensions;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Hosting;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core;
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

    public BlockPreviewBackofficeController(IPublishedRouter publishedRouter,
                                            IBlockPreviewer blockPreviewer,
                                            ILanguageService languageService,
                                            IVariationContextAccessor variationContextAccessor,
                                            IContentLocator contentLocator,
                                            IUmbracoContextAccessor umbracoContextAccessor,
                                            IJsonSerializer jsonSerializer,
                                            IContentTypeService contentTypeService) {
        _publishedRouter = publishedRouter;
        _blockPreviewer = blockPreviewer;
        _languageService = languageService;
        _variationContextAccessor = variationContextAccessor;
        _contentLocator = contentLocator;
        _umbracoContextAccessor = umbracoContextAccessor;
        _jsonSerializer = jsonSerializer;
        _contentTypeService = contentTypeService;
    }

    [HttpPost("previewGridBlock")]
    public async Task<IActionResult> PreviewGridBlock([FromQuery(Name = "nodeKey")] Guid? contentId,
                                                      [FromQuery(Name = "documentTypeKey")] Guid contentTypeId,
                                                      [FromQuery(Name = "contentUdi")] string blockUdi,
                                                      [FromQuery] string culture,
                                                      [FromBody] BlockGridValue blockData) {
        string markup;

        try {
            var publishedContent = GetPublishedContent(contentId, contentTypeId);

            if (publishedContent == null) {
                throw new BlockPreviewWarningException("No published content found");
            } else {
                var previewCulture = await GetCultureAsync(publishedContent, culture);
                SetCulture(previewCulture);

                await SetupPublishedRequest(publishedContent);

                var blockId = UdiParser.Parse(blockUdi)
                                       .ToId()
                                       .GetValueOrThrow();

                var blockEditorData = blockData.DeserializeAndClean(_jsonSerializer, _contentTypeService);

                if (blockEditorData == null) {
                    throw new BlockPreviewErrorException("The block data is invalid");
                } else {
                    markup = await _blockPreviewer.PreviewBlockAsync(blockId,
                                                                     publishedContent.ContentType.Alias,
                                                                     blockEditorData);
                }
            }

            markup = markup.CleanUpMarkupForPreview();
        } catch (BlockPreviewException ex) {
            markup = ex.Markup;
        } catch (Exception ex) {
            var blockPreviewException = new BlockPreviewErrorException(ex.Message);

            markup = blockPreviewException.Markup;
        }

        return Ok(markup);
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

    private IPublishedContent GetPublishedContent(Guid? contentId, Guid contentTypeId) {
        var content = contentId.IfNotNull(x => _contentLocator.ById(x));

        if (content != null) {
            return content;
        }

        var contentType = _contentTypeService.Get(contentTypeId);

        return contentType != null ? _contentLocator.All(contentType.Alias).FirstOrDefault() : null;
    }

    private async Task<string> GetCultureAsync(IPublishedContent content, string culture) {
        var currentCulture = culture.HasValue() ? culture : content?.GetCultureFromDomains();

        if (!currentCulture.HasValue() || currentCulture == "undefined") {
            var defaultLanguage = await _languageService.GetDefaultLanguageAsync();
            currentCulture = defaultLanguage?.IsoCode;
        }

        return currentCulture;
    }

    // Synchronous because a culture set inside an async method is restored when that method returns.
    private void SetCulture(string culture) {
        _variationContextAccessor.VariationContext = new VariationContext(culture);

        var cultureInfo = new CultureInfo(culture);
        Thread.CurrentThread.CurrentCulture = cultureInfo;
        Thread.CurrentThread.CurrentUICulture = cultureInfo;
    }
}
