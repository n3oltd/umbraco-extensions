using Microsoft.AspNetCore.Http;
using Umbraco.Engage.Infrastructure.Analytics.Collection;

namespace N3O.Umbraco.Marketing;

// Engage's middleware asks ShouldProcessRequest before routing, to mint a pageview and write the visitor cookie, and
// its MVC result filter asks IsUmbracoRequest, to assign variants and inject its scripts into the page. Answering no
// to both keeps Engage out of every page, and ServerEngageFilter opens both again for a page that needs them.
public class BrowserEngageRequestFilter : IAnalyticsRequestFilter {
    private readonly AnalyticsRequestFilter _analyticsRequestFilter;

    public BrowserEngageRequestFilter(AnalyticsRequestFilter analyticsRequestFilter) {
        _analyticsRequestFilter = analyticsRequestFilter;
    }

    public bool IsUmbracoRequest(HttpContext context) {
        if (BrowserEngage.IsServerRendered(context)) {
            return _analyticsRequestFilter.IsUmbracoRequest(context);
        } else {
            return true;
        }
    }

    public bool ShouldCollectRequest(HttpContext context) {
        return _analyticsRequestFilter.ShouldCollectRequest(context);
    }

    public bool ShouldProcessRequest(HttpContext context) {
        return false;
    }
}
