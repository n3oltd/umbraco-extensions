using System;

namespace N3O.Umbraco.Analytics;

public static class AnalyticsConstants {
    public static class Tags {
        public static class Cookie {
            public static readonly string Name = "n3o_tags";
            public static readonly TimeSpan Lifetime = TimeSpan.FromDays(90);
        }
    }

    public static class PageModuleKeys {
        public static readonly string DataLayer = nameof(DataLayer);
        public static readonly string GoogleAnalytics4 = nameof(GoogleAnalytics4);
        public static readonly string GoogleTagManager = nameof(GoogleTagManager);
    }
}
