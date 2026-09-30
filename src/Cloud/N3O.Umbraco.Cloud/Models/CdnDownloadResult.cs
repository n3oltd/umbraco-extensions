using NodaTime;

namespace N3O.Umbraco.Cloud.Models;

public class CdnDownloadResult {
    private static readonly Duration ErrorRetryInterval = Duration.FromSeconds(10);

    private CdnDownloadResult(bool success,
                              bool error,
                              string content,
                              Instant timestamp,
                              Duration maxAge,
                              Duration retryInterval) {
        Success = success;
        Error = error;
        Content = content;
        Timestamp = timestamp;
        MaxAge = maxAge;
        RetryInterval = retryInterval;
    }

    public bool Success { get; }
    public bool Error { get; }
    public string Content { get; }
    public Instant Timestamp { get; }
    public bool NotFound => !Success && !Error;

    private Duration MaxAge { get; }
    private Duration RetryInterval { get; }

    public bool CanRetry(IClock clock) {
        if (Success) {
            return false;
        } else {
            var age = clock.GetCurrentInstant() - Timestamp;

            return age > RetryInterval;
        }
    }

    public bool IsExpired(IClock clock) {
        if (Success) {
            var age = clock.GetCurrentInstant() - Timestamp;

            return age > MaxAge;
        } else {
            return false;
        }
    }

    public bool WasInvalidated(Instant? invalidatedAt) {
        if (invalidatedAt == null) {
            return false;
        }

        return Timestamp <= invalidatedAt.Value;
    }

    public static CdnDownloadResult ForNotFound(Instant startedAt, Duration retryInterval) {
        return new CdnDownloadResult(false, false, null, startedAt, Duration.Zero, retryInterval);
    }

    // A not-found that displaces content the CDN was serving may be a blip mid-publish rather than a
    // deletion, and until it is retried the page it replaced is gone.
    public static CdnDownloadResult ForNotFoundReplacingSuccess(Instant startedAt) {
        return new CdnDownloadResult(false, false, null, startedAt, Duration.Zero, ErrorRetryInterval);
    }

    public static CdnDownloadResult ForError(Instant startedAt) {
        return new CdnDownloadResult(false, true, null, startedAt, Duration.Zero, ErrorRetryInterval);
    }

    public static CdnDownloadResult ForSuccess(Instant startedAt, string content, Duration maxAge) {
        return new CdnDownloadResult(true, false, content, startedAt, maxAge, Duration.Zero);
    }
}