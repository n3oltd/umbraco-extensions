using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Hosting;
using N3O.Umbraco.Marketing.Models;
using System;
using System.Threading.Tasks;
using Umbraco.Engage.Headless.Common.HeadlessHttpContext;
using Umbraco.Engage.Headless.Services;
using Umbraco.Engage.Infrastructure.Analytics.Collection.Extractors;
using Umbraco.Engage.Infrastructure.Analytics.IpFilters;

namespace N3O.Umbraco.Marketing.Controllers;

// Registers the pageview for a page rendered with browser Engage. The route avoids the tokens ad-blocking lists match
// on (analytics, track, pageview, visit, collect), which block Engage's own headless routes.
public class EngageController : ApiController {
    private const string PreviewCookieName = "UMB_PREVIEW";

    private readonly IHeadlessPageViewService _headlessPageViewService;
    private readonly IHttpContextIpAddressExtractor _ipAddressExtractor;
    private readonly IIpFiltersService _ipFiltersService;

    public EngageController(IHeadlessPageViewService headlessPageViewService,
                            IHttpContextIpAddressExtractor ipAddressExtractor,
                            IIpFiltersService ipFiltersService) {
        _headlessPageViewService = headlessPageViewService;
        _ipAddressExtractor = ipAddressExtractor;
        _ipFiltersService = ipFiltersService;
    }

    [HttpPost("v1/session")]
    public async Task<ActionResult<SessionRes>> StartSession(SessionReq req) {
        if (!BrowserEngage.IsEnabled()) {
            return NotFound();
        }

        if (!IsSameOrigin() || req.Url?.IsAbsoluteUri != true || !req.Url.Host.EqualsInvariant(Request.Host.Host)) {
            return BadRequest();
        }

        var res = new SessionRes();

        // The page's own request would have been excluded from Engage for these reasons, and the headless
        // registration does not apply them itself.
        if (Request.Cookies.ContainsKey(PreviewCookieName) ||
            _ipFiltersService.IsFiltered(_ipAddressExtractor.ExtractIpAddress(HttpContext))) {
            return Ok(res);
        }

        var headlessContext = new HeadlessHttpContext(HttpContext,
                                                      req.Url.AbsoluteUri,
                                                      req.Referrer?.AbsoluteUri,
                                                      Request.Headers.UserAgent);
        headlessContext.User = HttpContext.User;

        if (!_headlessPageViewService.IsAllowedToRegisterPageview(headlessContext)) {
            return StatusCode(StatusCodes.Status402PaymentRequired);
        }

        var pageview = await _headlessPageViewService.RegisterRemotePageView(headlessContext);

        res.PageviewId = pageview?.Guid;

        return Ok(res);
    }

    // Sec-Fetch-Site is absent from older browsers, which still send Origin with a POST.
    private bool IsSameOrigin() {
        var fetchSite = Request.Headers["Sec-Fetch-Site"].ToString();

        if (fetchSite.HasValue()) {
            return fetchSite.EqualsInvariant("same-origin");
        }

        var origin = Request.Headers[HeaderNames.Origin].ToString();

        return Uri.TryCreate(origin, UriKind.Absolute, out var originUri) &&
               originUri.Host.EqualsInvariant(Request.Host.Host);
    }
}
