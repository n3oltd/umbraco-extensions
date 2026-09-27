using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using N3O.Umbraco.Extensions;
using System;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Routing;

namespace N3O.Umbraco.Hosting;

// Opts an HTML page into the Cloudflare edge cache only when every condition for sharing it between visitors holds,
// so a condition that fails or is not recognised leaves the page uncached rather than wrongly cached.
public class EdgeCacheMiddleware : IMiddleware {
    private const string CacheTagHeader = "Cache-Tag";
    private const string CloudflareCacheControlHeader = "Cloudflare-CDN-Cache-Control";

    // max-age bounds staleness for data no purge covers; max-age rather than s-maxage because s-maxage disables stale
    // serving at Cloudflare.
    private const string EdgeCacheControl = "max-age=300, stale-while-revalidate=60, stale-if-error=86400";

    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly Lazy<IPublicAccessService> _publicAccessService;
    private readonly Lazy<IUmbracoContextAccessor> _umbracoContextAccessor;
    private readonly Lazy<IOptionsSnapshot<CookieAuthenticationOptions>> _cookieAuthenticationOptions;

    public EdgeCacheMiddleware(IWebHostEnvironment webHostEnvironment,
                               Lazy<IPublicAccessService> publicAccessService,
                               Lazy<IUmbracoContextAccessor> umbracoContextAccessor,
                               Lazy<IOptionsSnapshot<CookieAuthenticationOptions>> cookieAuthenticationOptions) {
        _webHostEnvironment = webHostEnvironment;
        _publicAccessService = publicAccessService;
        _umbracoContextAccessor = umbracoContextAccessor;
        _cookieAuthenticationOptions = cookieAuthenticationOptions;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next) {
        if (EdgeCaching.IsEnabled()) {
            context.Response.OnStarting(() => ApplyAsync(context));
        }

        await next(context);
    }

    private Task ApplyAsync(HttpContext context) {
        var response = context.Response;

        if (!IsHtml(response)) {
            return Task.CompletedTask;
        }

        if (IsCacheable(context)) {
            response.Headers.CacheControl = "no-cache";
            response.Headers[CloudflareCacheControlHeader] = EdgeCacheControl;
            response.Headers[CacheTagHeader] = EdgeCaching.GetHtmlTag(context.Request.Host.Host);
        } else if (!response.Headers.ContainsKey(HeaderNames.CacheControl)) {
            response.Headers.CacheControl = "private, no-store";
        }

        return Task.CompletedTask;
    }

    private bool IsCacheable(HttpContext context) {
        var request = context.Request;
        var response = context.Response;

        if (!HttpMethods.IsGet(request.Method) ||
            response.StatusCode != StatusCodes.Status200OK ||
            IsMarkedPrivate(response) ||
            response.Headers.ContainsKey(HeaderNames.SetCookie) ||
            EdgeCaching.IsPrevented(context) ||
            _webHostEnvironment.IsStaging() ||
            !IsCanonicalHost(request) ||
            request.QueryString.HasValue ||
            IsIdentified(context) ||
            IsPreview()) {
            return false;
        }

        var content = context.Features.Get<UmbracoRouteValues>()?.PublishedRequest?.PublishedContent;

        if (content == null || _publicAccessService.Value.IsProtected(content.Path).Success) {
            return false;
        }

        return true;
    }

    private bool IsCanonicalHost(HttpRequest request) {
        var canonicalDomain = EnvironmentData.GetOurValue(HostingConstants.Environment.Keys.CanonicalDomain);

        return canonicalDomain.HasValue() && request.Host.Host.EqualsInvariant(canonicalDomain);
    }

    private bool IsHtml(HttpResponse response) {
        return response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true;
    }

    private bool IsIdentified(HttpContext context) {
        var authType = global::Umbraco.Cms.Core.Constants.Security.BackOfficeAuthenticationType;
        var backOfficeCookieName = _cookieAuthenticationOptions.Value.Get(authType).Cookie.Name;

        return context.User.Identity.IsAuthenticated ||
               context.Request.Headers.ContainsKey(HeaderNames.Authorization) ||
               context.Request.Cookies.ContainsKey(backOfficeCookieName);
    }

    private bool IsMarkedPrivate(HttpResponse response) {
        var cacheControl = response.GetTypedHeaders().CacheControl;

        return cacheControl != null && (cacheControl.Private || cacheControl.NoStore || cacheControl.NoCache);
    }

    private bool IsPreview() {
        return _umbracoContextAccessor.Value.TryGetUmbracoContext(out var umbracoContext) &&
               umbracoContext.InPreviewMode;
    }
}
