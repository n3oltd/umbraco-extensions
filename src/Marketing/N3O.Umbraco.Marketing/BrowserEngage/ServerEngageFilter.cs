using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.Hosting;
using System;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common.Routing;
using Umbraco.Engage.Infrastructure.AbTesting.Enums;
using Umbraco.Engage.Infrastructure.AbTesting.Repositories.Interfaces;
using Umbraco.Engage.Infrastructure.Analytics.Collection;
using Umbraco.Engage.Infrastructure.Analytics.Collection.Visitor;
using Umbraco.Engage.Infrastructure.Analytics.Common;
using Umbraco.Engage.Infrastructure.Common;
using Umbraco.Engage.Infrastructure.Personalization.AppliedPersonalizations;
using Umbraco.Engage.Infrastructure.Personalization.PersonalizationProfile;

namespace N3O.Umbraco.Marketing;

// A page with a running A/B test or an active personalisation is varied per visitor on the server, so it is
// rendered by Engage as it would be without browser Engage and is kept out of the edge cache. The steps around
// next() are the ones Engage's RequestHandlingMiddleware performs, which BrowserEngageRequestFilter has switched off
// because the middleware runs before the page is known.
public class ServerEngageFilter : IAsyncResourceFilter {
    private readonly AnalyticsRequestFilter _analyticsRequestFilter;
    private readonly IAbTestRepository _abTestRepository;
    private readonly IAppliedPersonalizationRepository _appliedPersonalizationRepository;
    private readonly IAnalyticsPageviewGuidManager _pageviewGuidManager;
    private readonly IAnalyticsVisitorExternalIdHandler _externalIdHandler;
    private readonly IAnalyticsRequestHandler _requestHandler;
    private readonly IPersonalizationProfileContext _personalizationProfileContext;
    private readonly ILogger<ServerEngageFilter> _logger;

    public ServerEngageFilter(AnalyticsRequestFilter analyticsRequestFilter,
                              IAbTestRepository abTestRepository,
                              IAppliedPersonalizationRepository appliedPersonalizationRepository,
                              IAnalyticsPageviewGuidManager pageviewGuidManager,
                              IAnalyticsVisitorExternalIdHandler externalIdHandler,
                              IAnalyticsRequestHandler requestHandler,
                              IPersonalizationProfileContext personalizationProfileContext,
                              ILogger<ServerEngageFilter> logger) {
        _analyticsRequestFilter = analyticsRequestFilter;
        _abTestRepository = abTestRepository;
        _appliedPersonalizationRepository = appliedPersonalizationRepository;
        _pageviewGuidManager = pageviewGuidManager;
        _externalIdHandler = externalIdHandler;
        _requestHandler = requestHandler;
        _personalizationProfileContext = personalizationProfileContext;
        _logger = logger;
    }

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next) {
        var httpContext = context.HttpContext;
        var publishedRequest = httpContext.Features.Get<UmbracoRouteValues>()?.PublishedRequest;

        if (publishedRequest?.PublishedContent == null ||
            !IsVariedOnServer(publishedRequest.PublishedContent, publishedRequest.Culture) ||
            !_analyticsRequestFilter.ShouldProcessRequest(httpContext)) {
            await next();

            return;
        }

        BrowserEngage.RenderOnServer(httpContext);
        EdgeCaching.Prevent(httpContext);

        try {
            _pageviewGuidManager.EnsurePageviewGuid(httpContext);
            _externalIdHandler.SetOrUpdateExternalId(httpContext);
        } catch (Exception ex) {
            _logger.LogError(ex, ex.Message);

            await next();

            return;
        }

        await next();

        try {
            CompleteRequest(httpContext);
        } catch (Exception ex) {
            _logger.LogError(ex, ex.Message);
        }
    }

    private void CompleteRequest(HttpContext httpContext) {
        if (httpContext.Features.Get<UmbracoEngageRequestContext>() == null) {
            return;
        }

        // Engage's result filter sets this only when the licence enables A/B testing.
        var splitUrlContext = httpContext.Features.Get<UmbracoEngageSplitUrlContext>();

        if (splitUrlContext != null) {
            httpContext.Response.Redirect(splitUrlContext.RedirectUrl);

            return;
        }

        _requestHandler.HandleRequest(httpContext);
        _personalizationProfileContext.GetCurrentProfile();
    }

    private bool IsVariedOnServer(IPublishedContent content, string culture) {
        // Engage evaluates test schedules against local time.
        var now = DateTime.Now;
        var abTests = _abTestRepository.GetAllForContent(content.Id, culture)
                                       .Concat(_abTestRepository.GetAllForContentType(content.ContentType.Id, culture));

        if (abTests.Any(x => x.Status == AbTestStatus.Running && x.IsActive(now))) {
            return true;
        }

        return _appliedPersonalizationRepository.GetActive(content.Id, content.ContentType.Id, culture).Any();
    }
}
