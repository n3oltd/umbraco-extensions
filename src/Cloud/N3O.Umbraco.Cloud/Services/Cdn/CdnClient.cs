using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Cloud.Models;
using N3O.Umbraco.Cloud.Options;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Exceptions;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JsonSerializer = N3O.Umbraco.Cloud.Lookups.JsonSerializer;

namespace N3O.Umbraco.Cloud;

public class CdnClient : ICdnClient {
    private static readonly SemaphoreSlim ConcurrencyLimit = new(10, 10);
    private static readonly ConcurrentDictionary<string, CdnDownloadResult> Downloads = new(StringComparer.InvariantCultureIgnoreCase);
    private static readonly ConcurrentDictionary<string, Lazy<Task<CdnDownloadResult>>> Refreshes = new(StringComparer.InvariantCultureIgnoreCase);
    private static readonly ConcurrentDictionary<string, Instant> InvalidatedAt = new(StringComparer.InvariantCultureIgnoreCase);

    private readonly ICloudUrl _cloudUrl;
    private readonly IClock _clock;
    private readonly IJsonProvider _jsonProvider;
    private readonly ILogger<CdnClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly Duration _maxAge;
    private readonly Duration _maxRetention;
    private readonly Duration _notFoundRetryInterval;

    public CdnClient(ICloudUrl cloudUrl,
                     IClock clock,
                     IJsonProvider jsonProvider,
                     ILogger<CdnClient> logger,
                     IOptions<CdnCacheOptions> options) {
        _cloudUrl = cloudUrl;
        _clock = clock;
        _jsonProvider = jsonProvider;
        _logger = logger;

        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(15);

        _maxAge = Duration.FromTimeSpan(options.Value.MaxAge);
        _notFoundRetryInterval = Duration.FromTimeSpan(options.Value.NotFoundRetryInterval);
        _maxRetention = Duration.Max(_maxAge, _notFoundRetryInterval);
    }

    public async Task<string> DownloadAsync(string path, CancellationToken cancellationToken = default) {
        var publishedUrl = GetPublishedContentUrl(path);
        
        var download = await FetchAsync(publishedUrl, cancellationToken);

        return download.Content;
    }

    public async Task<T> DownloadPublishedContentAsync<T>(PublishedFileKind kind,
                                                          string path,
                                                          JsonSerializer jsonSerializer,
                                                          CancellationToken cancellationToken = default) {
        var publishedUrl = GetPublishedContentUrl(kind, path);

        var download = await FetchAsync(publishedUrl, cancellationToken);

        return download.Content.IfNotNull(x => Deserialize<T>(x, jsonSerializer));
    }

    public async Task<PublishedContentResult> DownloadPublishedContentAsync(string path,
                                                                            CancellationToken cancellationToken = default) {
        var publishedUrl = GetPublishedContentUrl(path);

        var download = await FetchAsync(publishedUrl, cancellationToken);

        if (download.Error) {
            return PublishedContentResult.ForError(path);
        }

        var jObject = download.Content.IfNotNull(JObject.Parse);
        var kind = jObject.GetPublishedFileKind();
            
        if (kind.HasValue()) {
            Guid.TryParse(jObject["id"]?.ToString(), out var id);
                
            return PublishedContentResult.ForFound(id, kind, path, jObject);
        } else {
            return PublishedContentResult.ForNotFound(path);
        }
    }

    public void Evict(string path) {
        Invalidate(GetPublishedContentUrl(path));
    }

    public void Evict(PublishedFileKind kind, string path) {
        Invalidate(GetPublishedContentUrl(kind, path));
    }

    private void Invalidate(string publishedUrl) {
        var now = _clock.GetCurrentInstant();

        InvalidatedAt[publishedUrl] = now;

        var cutoff = now - _maxRetention;

        foreach (var invalidation in InvalidatedAt) {
            if (invalidation.Value < cutoff) {
                InvalidatedAt.TryRemove(invalidation);
            }
        }
    }

    private async Task<CdnDownloadResult> FetchAsync(string publishedUrl, CancellationToken cancellationToken) {
        // A refresh already in flight when an eviction lands cannot satisfy that eviction, so a caller joining
        // it takes one further turn rather than accepting the entry that refresh produces.
        for (var attempt = 0; attempt < 2; attempt++) {
            var download = Downloads.GetOrDefault(publishedUrl);

            if (download != null &&
                !download.IsExpired(_clock) &&
                !download.CanRetry(_clock) &&
                !download.WasInvalidated(GetInvalidatedAt(publishedUrl))) {
                return download;
            }

            // Coalesce refreshes per URL so a failing CDN cannot pile up in-flight fetches and drain ConcurrencyLimit.
            var refresh = Refreshes.GetOrAdd(publishedUrl,
                                             url => new Lazy<Task<CdnDownloadResult>>(() => RefreshAsync(url)));

            await refresh.Value.WaitAsync(cancellationToken);

            // A refresh that could not replace the entry will not replace it on a second turn either.
            if (ReferenceEquals(Downloads.GetOrDefault(publishedUrl), download)) {
                break;
            }
        }

        return Downloads[publishedUrl];
    }

    private async Task<CdnDownloadResult> RefreshAsync(string publishedUrl) {
        try {
            var startedAt = _clock.GetCurrentInstant();
            var cdnDownloadResult = await DownloadStringAsync(publishedUrl, startedAt, CancellationToken.None);

            // Refreshes for a URL are serialised, so this is the entry AddOrUpdate will see.
            if (cdnDownloadResult.NotFound && Downloads.GetOrDefault(publishedUrl)?.Success == true) {
                cdnDownloadResult = CdnDownloadResult.ForNotFoundReplacingSuccess(startedAt);
            }

            // A cached success survives a transient error, but not a not-found: content that has gone must stop
            // being served.
            var result = Downloads.AddOrUpdate(publishedUrl,
                                               cdnDownloadResult,
                                               (_, existing) => cdnDownloadResult.Error && existing.Success
                                                                    ? existing
                                                                    : cdnDownloadResult);

            if (ReferenceEquals(result, cdnDownloadResult)) {
                ClearInvalidation(publishedUrl, startedAt);
            }

            return result;
        } finally {
            Refreshes.TryRemove(publishedUrl, out _);
        }
    }

    private void ClearInvalidation(string publishedUrl, Instant startedAt) {
        var invalidatedAt = GetInvalidatedAt(publishedUrl);

        if (invalidatedAt == null) {
            return;
        }

        if (invalidatedAt.Value < startedAt) {
            InvalidatedAt.TryRemove(new KeyValuePair<string, Instant>(publishedUrl, invalidatedAt.Value));
        }
    }

    private Instant? GetInvalidatedAt(string publishedUrl) {
        if (InvalidatedAt.TryGetValue(publishedUrl, out var invalidatedAt)) {
            return invalidatedAt;
        } else {
            return null;
        }
    }

    private async Task<CdnDownloadResult> DownloadStringAsync(string publishedUrl,
                                                              Instant startedAt,
                                                              CancellationToken cancellationToken) {
        try {
            var download = await GetStringRateLimitedAsync(publishedUrl, cancellationToken);

            return CdnDownloadResult.ForSuccess(startedAt, download, _maxAge);
        } catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
            _logger.LogDebug("CDN 404 for {PublishedUrl}", publishedUrl);

            return CdnDownloadResult.ForNotFound(startedAt, _notFoundRetryInterval);
        } catch (Exception ex) {
            _logger.LogWarning(ex, "Could not download {PublishedUrl}", publishedUrl);

            return CdnDownloadResult.ForError(startedAt);
        }
    }
    
    private string GetPublishedContentUrl(PublishedFileKind kind, string path) {
        return GetPublishedContentUrl(GetPublishedPath(kind, path));
    }

    private T Deserialize<T>(string json, JsonSerializer jsonSerializer) {
        if (jsonSerializer == JsonSerializers.JsonProvider) {
            return _jsonProvider.DeserializeObject<T>(json);
        } else if (jsonSerializer == JsonSerializers.Simple) {
            return JsonConvert.DeserializeObject<T>(json);
        } else {
            throw UnrecognisedValueException.For(jsonSerializer);
        }
    }
    
    private string GetPublishedPath(PublishedFileKind kind, string path) {
        return $"{kind.PathSegment}/{path}";
    }
    
    private string GetPublishedContentUrl(string path) {
        return _cloudUrl.ForCdn(CdnRoots.Connect, path);
    }
    
    private async Task<string> GetStringRateLimitedAsync(string publishedUrl, CancellationToken cancellationToken) {
        await ConcurrencyLimit.WaitAsync(cancellationToken);

        try {
            return await _httpClient.GetStringAsync(publishedUrl, cancellationToken);
        } finally {
            ConcurrencyLimit.Release();
        }
    }
}