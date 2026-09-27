using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Templates.Extensions;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common.Routing;

namespace N3O.Umbraco.Templates;

public class Merger : IMerger {
    private readonly ConcurrentDictionary<IPublishedContent, IReadOnlyDictionary<string, object>> _mergeModelsCache = new();
    private readonly IHtmlHelper _htmlHelper;
    private readonly ITemplateEngine _templateEngine;
    private readonly IEnumerable<IMergeModelsProvider> _mergeModelsProviders;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<Merger> _logger;

    public Merger(IHtmlHelper htmlHelper,
                  ITemplateEngine templateEngine,
                  IEnumerable<IMergeModelsProvider> mergeModelsProviders,
                  IHttpContextAccessor httpContextAccessor,
                  ILogger<Merger> logger) {
        _htmlHelper = htmlHelper;
        _templateEngine = templateEngine;
        _mergeModelsProviders = mergeModelsProviders;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }
    
    public async Task<string> MergeForAsync(IPublishedContent content,
                                            string markup,
                                            CancellationToken cancellationToken = default) {
        var mergeModels = await _mergeModelsProviders.GetMergeModelsAsync(_logger,
                                                                          _httpContextAccessor.HttpContext,
                                                                          content,
                                                                          _mergeModelsCache,
                                                                          cancellationToken);
        var html = _templateEngine.Render(markup, mergeModels);

        return html;
    }

    public async Task<IHtmlContent> MergePartialForAsync(IPublishedContent content,
                                                         ViewContext viewContext,
                                                         string partialViewName,
                                                         object model,
                                                         CancellationToken cancellationToken = default) {
        var mergeModels = default(IReadOnlyDictionary<string, object>);

        if (content.HasValue()) {
            mergeModels = await _mergeModelsProviders.GetMergeModelsAsync(_logger,
                                                                          _httpContextAccessor.HttpContext,
                                                                          content,
                                                                          _mergeModelsCache,
                                                                          cancellationToken);
        }
        
        (_htmlHelper as IViewContextAware)?.Contextualize(viewContext);
        
        var htmlContent = await _htmlHelper.PartialAsync(partialViewName, model);

        return new MergedHtmlContent(_templateEngine, htmlContent, mergeModels);
    }

    public async Task<IHtmlContent> MergePartialForCurrentContentAsync(ViewContext viewContext,
                                                                       string partialViewName,
                                                                       object model,
                                                                       CancellationToken cancellationToken = default) {
        var currentContent = GetCurrentContent();
        
        return await MergePartialForAsync(currentContent, viewContext, partialViewName, model, cancellationToken);
    }
    
    private IPublishedContent GetCurrentContent() {
        var umbracoRouteValues = _httpContextAccessor.HttpContext.Features.Get<UmbracoRouteValues>();

        return umbracoRouteValues?.PublishedRequest.PublishedContent;
    }
}