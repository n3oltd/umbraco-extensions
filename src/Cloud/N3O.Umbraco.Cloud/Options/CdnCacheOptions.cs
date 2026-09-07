using System;

namespace N3O.Umbraco.Cloud.Options;

public class CdnCacheOptions {
    public TimeSpan MaxAge { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan NotFoundRetryInterval { get; set; } = TimeSpan.FromSeconds(60);
}
