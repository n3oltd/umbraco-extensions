using N3O.Umbraco.Content;
using N3O.Umbraco.Context;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Localization;
using N3O.Umbraco.Marketing.Content;
using N3O.Umbraco.Marketing.Models;
using NodaTime;
using NodaTime.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Extensions;

namespace N3O.Umbraco.Marketing.Services;

public class MarketingExport : IMarketingExport {
    // Umbraco Engage seeds this visitor and reassigns sessions to it, both when a visitor denies
    // analytics consent and when a session passes the anonymization horizon. It carries
    // visitorType 0 like a person, so the bot filter does not exclude it
    private const string AnonymousVisitorExternalId = "11111111-1111-1111-1111-111111111111";

    private const string AnyPageviewsSql = @"
SELECT TOP 1 1
FROM umbracoEngageAnalyticsPageview pv
INNER JOIN umbracoEngageAnalyticsPage p ON p.id = pv.pageId
WHERE p.domain = @0 OR p.domain = @1";

    private const string GoalsSql = @";WITH CandidateSessions AS (
    SELECT DISTINCT pv.sessionId
    FROM umbracoEngageAnalyticsPageview pv
    WHERE pv.timestamp >= @0 AND pv.timestamp < @1
),
SessionFirstPageview AS (
    SELECT pv.sessionId, MIN(pv.id) AS pageviewId
    FROM umbracoEngageAnalyticsPageview pv
    WHERE pv.sessionId IN (SELECT sessionId FROM CandidateSessions)
    GROUP BY pv.sessionId
)
SELECT g.name AS Name,
       gc.value AS Value,
       pv.timestamp AS Timestamp,
       pv.utmSource AS Source,
       pv.utmMedium AS Medium,
       pv.utmCampaign AS Campaign,
       rp.domain AS ReferrerDomain
FROM umbracoEngageAnalyticsGoalCompletion gc
INNER JOIN umbracoEngageAnalyticsPageview gpv ON gpv.id = gc.pageviewId
INNER JOIN umbracoEngageAnalyticsSession s ON s.id = gpv.sessionId
INNER JOIN SessionFirstPageview fp ON fp.sessionId = s.id
INNER JOIN umbracoEngageAnalyticsPageview pv ON pv.id = fp.pageviewId
INNER JOIN umbracoEngageAnalyticsPage p ON p.id = pv.pageId
INNER JOIN umbracoEngageAnalyticsVisitor v ON v.id = s.visitorId
INNER JOIN umbracoEngageSettingsGoal g ON g.id = gc.goalId
LEFT JOIN umbracoEngageAnalyticsPage rp ON rp.id = pv.referrerPageId
WHERE v.visitorType = 0
  AND (p.domain = @2 OR p.domain = @3)
  AND s.id IN (SELECT sessionId FROM CandidateSessions)";

    // Cut by pageview rather than by session, so the domain filter is the page's own: a session that
    // lands on one host variant and browses the other still counts each page against its own host
    private const string PagesSql = @";WITH CandidateSessions AS (
    SELECT DISTINCT pv.sessionId
    FROM umbracoEngageAnalyticsPageview pv
    WHERE pv.timestamp >= @0 AND pv.timestamp < @1
),
SessionFirstPageview AS (
    SELECT pv.sessionId, MIN(pv.id) AS pageviewId
    FROM umbracoEngageAnalyticsPageview pv
    WHERE pv.sessionId IN (SELECT sessionId FROM CandidateSessions)
    GROUP BY pv.sessionId
)
SELECT pv.timestamp AS Timestamp,
       p.path AS Path,
       CASE WHEN fp.pageviewId = pv.id THEN 1 ELSE 0 END AS IsEntrance
FROM umbracoEngageAnalyticsPageview pv
INNER JOIN umbracoEngageAnalyticsPage p ON p.id = pv.pageId
INNER JOIN umbracoEngageAnalyticsSession s ON s.id = pv.sessionId
INNER JOIN umbracoEngageAnalyticsVisitor v ON v.id = s.visitorId
LEFT JOIN SessionFirstPageview fp ON fp.sessionId = pv.sessionId
WHERE pv.timestamp >= @0 AND pv.timestamp < @1
  AND v.visitorType = 0
  AND p.path IS NOT NULL
  AND (p.domain = @2 OR p.domain = @3)";

    private const string SessionsSql = @";WITH CandidateSessions AS (
    SELECT DISTINCT pv.sessionId
    FROM umbracoEngageAnalyticsPageview pv
    WHERE pv.timestamp >= @0 AND pv.timestamp < @1
),
SessionFirstPageview AS (
    SELECT pv.sessionId, MIN(pv.id) AS pageviewId, COUNT(*) AS pageviews
    FROM umbracoEngageAnalyticsPageview pv
    WHERE pv.sessionId IN (SELECT sessionId FROM CandidateSessions)
    GROUP BY pv.sessionId
),
CandidateVisitors AS (
    SELECT DISTINCT s.visitorId
    FROM umbracoEngageAnalyticsSession s
    WHERE s.id IN (SELECT sessionId FROM CandidateSessions)
),
VisitorFirstSession AS (
    SELECT s.visitorId, MIN(s.id) AS sessionId
    FROM umbracoEngageAnalyticsSession s
    WHERE s.visitorId IN (SELECT visitorId FROM CandidateVisitors)
    GROUP BY s.visitorId
)
SELECT s.id AS SessionId,
       s.visitorId AS VisitorId,
       pv.timestamp AS Timestamp,
       pv.utmSource AS Source,
       pv.utmMedium AS Medium,
       pv.utmCampaign AS Campaign,
       rp.domain AS ReferrerDomain,
       fp.pageviews AS Pageviews,
       CASE WHEN vfs.sessionId = s.id THEN 1 ELSE 0 END AS IsNewVisitor,
       CASE WHEN v.externalId = @4 THEN 1 ELSE 0 END AS IsAnonymousVisitor
FROM umbracoEngageAnalyticsSession s
INNER JOIN SessionFirstPageview fp ON fp.sessionId = s.id
INNER JOIN umbracoEngageAnalyticsPageview pv ON pv.id = fp.pageviewId
INNER JOIN umbracoEngageAnalyticsPage p ON p.id = pv.pageId
INNER JOIN umbracoEngageAnalyticsVisitor v ON v.id = s.visitorId
INNER JOIN VisitorFirstSession vfs ON vfs.visitorId = s.visitorId
LEFT JOIN umbracoEngageAnalyticsPage rp ON rp.id = pv.referrerPageId
WHERE v.visitorType = 0 AND (p.domain = @2 OR p.domain = @3)";

    private readonly IBaseCurrencyAccessor _baseCurrencyAccessor;
    private readonly IContentCache _contentCache;
    private readonly ILocalClock _localClock;
    private readonly IUmbracoDatabaseFactory _umbracoDatabaseFactory;

    public MarketingExport(IBaseCurrencyAccessor baseCurrencyAccessor,
                         IContentCache contentCache,
                         ILocalClock localClock,
                         IUmbracoDatabaseFactory umbracoDatabaseFactory) {
        _baseCurrencyAccessor = baseCurrencyAccessor;
        _contentCache = contentCache;
        _localClock = localClock;
        _umbracoDatabaseFactory = umbracoDatabaseFactory;
    }

    public async Task<DailyRes> GetDailyAsync(string siteId,
                                              LocalDate from,
                                              LocalDate to,
                                              CancellationToken cancellationToken) {
        var site = GetSite(siteId);

        if (site == null) {
            return null;
        }

        var zone = _localClock.GetZone();
        var host = GetHost(siteId);

        if (host == null) {
            return null;
        }

        // The padding absorbs the timezone shift: a session is bucketed by its local date, so the
        // UTC filter must reach a day either side of the requested window
        var fromUtc = from.PlusDays(-1).AtMidnight().ToDateTimeUnspecified();
        var toUtc = to.PlusDays(2).AtMidnight().ToDateTimeUnspecified();

        List<SessionRow> sessionRows;
        List<GoalCompletionRow> goalRows;
        List<PageviewRow> pageviewRows;

        using (var db = _umbracoDatabaseFactory.CreateDatabase()) {
            sessionRows = await db.FetchAsync<SessionRow>(SessionsSql,
                                                          fromUtc,
                                                          toUtc,
                                                          host,
                                                          ToggleWww(host),
                                                          AnonymousVisitorExternalId);
            goalRows = await db.FetchAsync<GoalCompletionRow>(GoalsSql, fromUtc, toUtc, host, ToggleWww(host));
            pageviewRows = await db.FetchAsync<PageviewRow>(PagesSql, fromUtc, toUtc, host, ToggleWww(host));
        }

        var res = new DailyRes();
        res.Goals = ToGoalRows(goalRows, zone, from, to);
        res.Pages = ToPageRows(pageviewRows, zone, from, to);
        res.Traffic = ToTrafficRows(sessionRows, zone, from, to);
        res.Users = ToUserRows(sessionRows, zone, from, to);

        return res;
    }

    public async Task<bool> HasRecordedTrafficAsync(string siteId, CancellationToken cancellationToken) {
        var host = GetHost(siteId);

        if (host == null) {
            return false;
        }

        using (var db = _umbracoDatabaseFactory.CreateDatabase()) {
            var any = await db.FirstOrDefaultAsync<int?>(AnyPageviewsSql, host, ToggleWww(host));

            return any.HasValue;
        }
    }

    public SiteRes GetSite(string siteId) {
        var site = GetSites().SingleOrDefault(x => x.Id.EqualsInvariant(siteId));

        return site;
    }

    public IReadOnlyList<SiteRes> GetSites() {
        var settings = _contentCache.Single<MarketingExportSettingsContent>();
        var root = settings?.Content()?.Root();

        if (root == null) {
            return [];
        }

        var res = new SiteRes();
        res.CurrencyCode = _baseCurrencyAccessor.GetBaseCurrency()?.Code;
        res.Id = root.Key.ToString();
        res.Name = root.Name;
        res.TimeZone = _localClock.GetZone().Id;
        res.Url = root.Url(mode: UrlMode.Absolute);

        return [res];
    }

    private string GetHost(string siteId) {
        var site = GetSite(siteId);

        return site != null && Uri.TryCreate(site.Url, UriKind.Absolute, out var siteUri) ? siteUri.Host : null;
    }

    private static string Canonicalize(string value) {
        return value.HasValue() ? value.Trim().ToLowerInvariant() : null;
    }

    private static string ToggleWww(string host) {
        return host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
                   ? host.Substring(4)
                   : "www." + host;
    }

    private static LocalDate ToLocalDate(DateTime timestamp, DateTimeZone zone) {
        var instant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(timestamp, DateTimeKind.Utc));

        return instant.InZone(zone).Date;
    }

    private static IEnumerable<GoalRow> ToGoalRows(IEnumerable<GoalCompletionRow> rows,
                                                   DateTimeZone zone,
                                                   LocalDate from,
                                                   LocalDate to) {
        var dated = rows.Select(x => new { Date = ToLocalDate(x.Timestamp, zone), Row = x })
                        .Where(x => x.Date >= from && x.Date <= to);

        var grouped = dated.GroupBy(x => new {
            x.Date,
            x.Row.Name,
            Source = Canonicalize(x.Row.Source),
            Medium = Canonicalize(x.Row.Medium),
            Campaign = Canonicalize(x.Row.Campaign),
            Referrer = Canonicalize(x.Row.ReferrerDomain)
        });

        foreach (var group in grouped) {
            var row = new GoalRow();
            row.Campaign = group.Key.Campaign;
            row.Count = group.Count();
            row.Date = LocalDatePattern.Iso.Format(group.Key.Date);
            row.Medium = group.Key.Medium;
            row.Name = group.Key.Name;
            row.Referrer = group.Key.Referrer;
            row.Source = group.Key.Source;
            row.Value = group.Sum(x => x.Row.Value);

            yield return row;
        }
    }

    private static IEnumerable<PageRow> ToPageRows(IEnumerable<PageviewRow> rows,
                                                    DateTimeZone zone,
                                                    LocalDate from,
                                                    LocalDate to) {
        var dated = rows.Select(x => new { Date = ToLocalDate(x.Timestamp, zone), Row = x })
                        .Where(x => x.Date >= from && x.Date <= to);

        var grouped = dated.GroupBy(x => new { x.Date, Path = Canonicalize(x.Row.Path) });

        foreach (var group in grouped) {
            var row = new PageRow();
            row.Date = LocalDatePattern.Iso.Format(group.Key.Date);
            row.Entrances = group.Count(x => x.Row.IsEntrance);
            row.Pageviews = group.Count();
            row.Path = group.Key.Path;

            yield return row;
        }
    }

    private static IEnumerable<TrafficRow> ToTrafficRows(IEnumerable<SessionRow> rows,
                                                         DateTimeZone zone,
                                                         LocalDate from,
                                                         LocalDate to) {
        var dated = rows.Select(x => new { Date = ToLocalDate(x.Timestamp, zone), Row = x })
                        .Where(x => x.Date >= from && x.Date <= to);

        var grouped = dated.GroupBy(x => new {
            x.Date,
            Source = Canonicalize(x.Row.Source),
            Medium = Canonicalize(x.Row.Medium),
            Campaign = Canonicalize(x.Row.Campaign),
            Referrer = Canonicalize(x.Row.ReferrerDomain)
        });

        foreach (var group in grouped) {
            var row = new TrafficRow();
            row.Campaign = group.Key.Campaign;
            row.Date = LocalDatePattern.Iso.Format(group.Key.Date);
            row.Medium = group.Key.Medium;
            row.NewUsers = group.Where(x => x.Row.IsNewVisitor && !x.Row.IsAnonymousVisitor)
                                .Select(x => x.Row.VisitorId)
                                .Distinct()
                                .Count();
            row.Pageviews = group.Sum(x => x.Row.Pageviews);
            row.Referrer = group.Key.Referrer;
            row.Sessions = group.Select(x => x.Row.SessionId).Distinct().Count();
            row.Source = group.Key.Source;

            yield return row;
        }
    }

    // A distinct count cannot be summed, so this carries no channel dimension: a consumer folding
    // rows onto a coarser channel would count a visitor once for each channel they arrived by
    private static IEnumerable<UserRow> ToUserRows(IEnumerable<SessionRow> rows,
                                                   DateTimeZone zone,
                                                   LocalDate from,
                                                   LocalDate to) {
        var dated = rows.Where(x => !x.IsAnonymousVisitor)
                        .Select(x => new { Date = ToLocalDate(x.Timestamp, zone), Row = x })
                        .Where(x => x.Date >= from && x.Date <= to);

        var grouped = dated.GroupBy(x => x.Date);

        foreach (var group in grouped) {
            var row = new UserRow();
            row.Count = group.Select(x => x.Row.VisitorId).Distinct().Count();
            row.Date = LocalDatePattern.Iso.Format(group.Key);

            yield return row;
        }
    }
}
