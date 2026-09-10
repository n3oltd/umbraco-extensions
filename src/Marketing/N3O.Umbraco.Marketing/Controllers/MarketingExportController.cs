using Microsoft.AspNetCore.Mvc;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Constants;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Hosting;
using N3O.Umbraco.Marketing.Content;
using N3O.Umbraco.Marketing.Models;
using N3O.Umbraco.Marketing.Services;
using NodaTime.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Marketing.Controllers;

[ApiDocument(MarketingConstants.ApiName)]
public class MarketingExportController : ApiController {
    private readonly IContentCache _contentCache;
    private readonly IMarketingExport _marketingExport;

    public MarketingExportController(IContentCache contentCache, IMarketingExport marketingExport) {
        _contentCache = contentCache;
        _marketingExport = marketingExport;
    }

    [HttpGet("v1/daily")]
    public async Task<ActionResult<DailyRes>> GetDaily(string siteId,
                                                       string from,
                                                       string to,
                                                       CancellationToken cancellationToken) {
        if (!IsAuthorized()) {
            return Unauthorized();
        }

        var fromResult = LocalDatePattern.Iso.Parse(from ?? "");
        var toResult = LocalDatePattern.Iso.Parse(to ?? "");

        if (!fromResult.Success || !toResult.Success) {
            return BadRequest("from and to must be dates in the format yyyy-MM-dd");
        }

        if (fromResult.Value > toResult.Value) {
            return BadRequest("from must not be later than to");
        }

        var res = await _marketingExport.GetDailyAsync(siteId, fromResult.Value, toResult.Value, cancellationToken);

        if (res == null) {
            return NotFound();
        }

        if (!res.Goals.Any() &&
            !res.Traffic.Any() &&
            !await _marketingExport.HasRecordedTrafficAsync(siteId, cancellationToken)) {
            return Conflict("Umbraco Engage has recorded no pageviews for this site's configured host");
        }

        return Ok(res);
    }

    [HttpGet("v1/sites")]
    public ActionResult<SitesRes> GetSites() {
        if (!IsAuthorized()) {
            return Unauthorized();
        }

        var res = new SitesRes();
        res.Sites = _marketingExport.GetSites();

        return Ok(res);
    }

    [HttpGet("v1/sites/{siteId}")]
    public ActionResult<SiteRes> GetSite(string siteId) {
        if (!IsAuthorized()) {
            return Unauthorized();
        }

        var res = _marketingExport.GetSite(siteId);

        if (res == null) {
            return NotFound();
        }

        return Ok(res);
    }

    private bool IsAuthorized() {
        var settings = _contentCache.Single<MarketingExportSettingsContent>();

        return Request.HasKey(HttpHeaders.ExportKey, settings?.ExportKey);
    }
}
