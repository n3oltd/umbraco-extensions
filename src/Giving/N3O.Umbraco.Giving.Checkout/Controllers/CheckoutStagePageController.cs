using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.Constants;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Giving.Checkout.Content;
using N3O.Umbraco.Giving.Checkout.Lookups;
using N3O.Umbraco.Hosting;
using N3O.Umbraco.Lookups;
using N3O.Umbraco.Pages;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;

namespace N3O.Umbraco.Giving.Checkout.Controllers;

[ResponseCache(CacheProfileName = CacheProfiles.NoCache)]
public abstract class CheckoutStagePageController : PageController {
    private static readonly string CompletePageAlias = AliasHelper<CheckoutCompletePageContent>.ContentTypeAlias();
    
    private readonly ICheckoutAccessor _checkoutAccessor;
    private readonly IContentCache _contentCache;

    protected CheckoutStagePageController(ILogger<CheckoutStagePageController> logger,
                                          ICompositeViewEngine compositeViewEngine,
                                          IUmbracoContextAccessor umbracoContextAccessor,
                                          IPublishedUrlProvider publishedUrlProvider,
                                          IPagePipeline pagePipeline,
                                          IContentCache contentCache,
                                          IServiceProvider serviceProvider,
                                          ICheckoutAccessor checkoutAccessor,
                                          IEnumerable<IContentRenderabilityFilter> contentRenderabilityFilters)
        : base(logger,
               compositeViewEngine,
               umbracoContextAccessor,
               publishedUrlProvider,
               pagePipeline,
               contentCache,
               serviceProvider,
               contentRenderabilityFilters) {
        _checkoutAccessor = checkoutAccessor;
        _contentCache = contentCache;
    }

    public override async Task<IActionResult> Index(CancellationToken cancellationToken) {
        var checkout = await _checkoutAccessor.GetOrCreateAsync(CancellationToken.None, true);

        string redirectUrl = null;

        if (checkout == null) {
            redirectUrl = _contentCache.Special(SpecialPages.Donate).AbsoluteUrl();
        } else if (checkout.IsComplete && !CurrentPage.ContentType.Alias.EqualsInvariant(CompletePageAlias)) {
            redirectUrl = _contentCache.Single<CheckoutCompletePageContent>().Content().AbsoluteUrl();
        } else if (checkout.Progress.CurrentStage != Stage && !Stage.CanRevisit) {
            redirectUrl = checkout.Progress.CurrentStage.GetUrl(_contentCache);
        }

        if (redirectUrl.HasValue()) {
            return Redirect(redirectUrl);
        } else {
            return await base.Index(cancellationToken);
        }
    }

    protected abstract CheckoutStage Stage { get; }
}
