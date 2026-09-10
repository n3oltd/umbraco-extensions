using N3O.Umbraco.Marketing.Models;
using NodaTime;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Marketing.Services;

public interface IMarketingExport {
    Task<DailyRes> GetDailyAsync(string siteId, LocalDate from, LocalDate to, CancellationToken cancellationToken);
    Task<bool> HasRecordedTrafficAsync(string siteId, CancellationToken cancellationToken);
    SiteRes GetSite(string siteId);
    IReadOnlyList<SiteRes> GetSites();
}
