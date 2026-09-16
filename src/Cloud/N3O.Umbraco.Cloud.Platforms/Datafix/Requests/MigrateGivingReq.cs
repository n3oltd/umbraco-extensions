using System;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class MigrateGivingReq {
    public string ExpectSubscriptionId { get; set; }
    public int? Limit { get; set; }
    public Guid? PlaceholderMediaId { get; set; }
    public string AnalyticsTag { get; set; }
}
