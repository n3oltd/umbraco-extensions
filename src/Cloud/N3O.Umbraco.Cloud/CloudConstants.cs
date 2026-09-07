using System.Collections.Generic;

namespace N3O.Umbraco.Cloud;

public static class CloudConstants {
    public const string BackOfficeApiName = "CloudBackOffice";
    
    public static class Clients {
        public static class HttpRetry {
            public static readonly IReadOnlyDictionary<int, int> RetryIntervals = new Dictionary<int, int> {
                { 1, 5 },
                { 2, 30 },
                { 3, 60 },
                { 4, 120 }
            };
        };
    }

    public static class Configuration {
        public const string CdnCacheSection = "CdnCache";
    }

    public static class Environment {
        public static class Keys {
            public static string DataRegion = "DataRegion";
            public static string SubscriptionId = "SubscriptionId";
        }
    }
}