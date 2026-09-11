using System;

namespace N3O.Umbraco.Cloud;

public static class CloudConstants {
    public const string BackOfficeApiName = "CloudBackOffice";
    
    public static class Clients {
        public const int MaxConnectionsPerServer = 64;

        public static readonly TimeSpan PooledConnectionLifetime = TimeSpan.FromMinutes(2);
        public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);
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
