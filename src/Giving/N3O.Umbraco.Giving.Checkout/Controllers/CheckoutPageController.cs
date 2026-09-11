using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.Content;
using N3O.Umbraco.Context;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Giving.Checkout.Content;
using N3O.Umbraco.Hosting;
using N3O.Umbraco.Pages;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;

namespace N3O.Umbraco.Giving.Checkout.Controllers;

public class CheckoutPageController : PageController {
    private readonly ICheckoutAccessor _checkoutAccessor;
    private readonly IQueryStringAccessor _queryStringAccessor;
    private readonly IContentCache _contentCache;
    private readonly ILogger<CheckoutPageController> _logger;

    public CheckoutPageController(ILogger<CheckoutPageController> logger,
                                  ICompositeViewEngine compositeViewEngine,
                                  IUmbracoContextAccessor umbracoContextAccessor,
                                  IPublishedUrlProvider publishedUrlProvider,
                                  IPagePipeline pagePipeline,
                                  IContentCache contentCache,
                                  IServiceProvider serviceProvider,
                                  ICheckoutAccessor checkoutAccessor,
                                  IQueryStringAccessor queryStringAccessor,
                                  IEnumerable<IContentRenderabilityFilter> contentRenderabilityFilters)
        : base(logger,
               compositeViewEngine,
               umbracoContextAccessor,
               publishedUrlProvider,
               pagePipeline,
               contentCache,
               serviceProvider,
               contentRenderabilityFilters) {
        _contentCache = contentCache;
        _logger = logger;
        _checkoutAccessor = checkoutAccessor;
        _queryStringAccessor = queryStringAccessor;
    }

    public override async Task<IActionResult> Index(CancellationToken cancellationToken) {
        if (_queryStringAccessor.Has("framed")) {
            return CurrentTemplate(new ContentModel(CurrentPage));
        } else {
            var checkout = await _checkoutAccessor.GetOrCreateAsync(cancellationToken);

            string url;

            if (checkout == null) {
                url = CheckoutRedirects.DonateUrl(_contentCache, _logger);
            } else if (checkout.IsComplete) {
                url = _contentCache.Single<CheckoutCompletePageContent>().Content().AbsoluteUrl();
            } else {
                url = checkout.Progress.CurrentStage.GetUrl(_contentCache);
            }

            return Redirect(url);
        }
    }
}
