using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using N3O.Umbraco.Cdn.Cloudflare.Clients;
using N3O.Umbraco.Hosting;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Configuration.Models;

namespace N3O.Umbraco.Cdn.Cloudflare;

// Coalesces the purges requested while this server applies a batch of changes into one trailing purge per sync
// interval, which bounds the purge rate to one per server per interval against the zone's shared rate limit.
public class EdgeCachePurger : IDisposable {
    private readonly object _lock = new();
    private readonly IZonesApiClient _zonesApiClient;
    private readonly ILogger<EdgeCachePurger> _logger;
    private readonly TimeSpan _interval;
    private readonly string _zoneId;
    private readonly string _tag;
    private readonly Timer _timer;
    private bool _scheduled;

    public EdgeCachePurger(IZonesApiClient zonesApiClient,
                           IOptions<GlobalSettings> globalSettings,
                           ILogger<EdgeCachePurger> logger) {
        _zonesApiClient = zonesApiClient;
        _logger = logger;
        _interval = globalSettings.Value.DatabaseServerMessenger.TimeBetweenSyncOperations;
        _zoneId = EnvironmentData.GetOurValue(CloudflareConstants.Environment.Keys.ZoneId);
        _tag = EdgeCaching.GetHtmlTag(EnvironmentData.GetOurValue(HostingConstants.Environment.Keys.CanonicalDomain));
        _timer = new Timer(_ => _ = PurgeAsync());
    }

    public void Dispose() {
        _timer.Dispose();
    }

    public void Schedule() {
        Schedule(_interval);
    }

    private void Schedule(TimeSpan delay) {
        lock (_lock) {
            if (!_scheduled) {
                _scheduled = true;

                _timer.Change(delay, Timeout.InfiniteTimeSpan);
            }
        }
    }

    private async Task PurgeAsync() {
        lock (_lock) {
            _scheduled = false;
        }

        try {
            var req = new ApiPurgeCacheReq();
            req.Tags = [_tag];

            var response = await _zonesApiClient.PurgeCacheAsync(_zoneId, req);

            if (response.StatusCode == HttpStatusCode.TooManyRequests) {
                Schedule(response.Headers.RetryAfter?.Delta ?? _interval);
            } else if (!response.IsSuccessStatusCode) {
                _logger.LogError(response.Error, "Cloudflare did not purge {Tag}: {StatusCode}", _tag, response.StatusCode);
            }
        } catch (Exception ex) {
            _logger.LogError(ex, "Could not purge {Tag} from Cloudflare", _tag);
        }
    }
}
