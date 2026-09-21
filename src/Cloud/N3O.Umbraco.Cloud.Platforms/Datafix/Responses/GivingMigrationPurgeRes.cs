using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationPurgeRes {
    public string SubscriptionId { get; set; }
    public string Message { get; set; }
    public bool Permanent { get; set; }
    public int LegacyForms { get; set; }
    public int Expected { get; set; }
    public int Purged { get; set; }
    public int Failed { get; set; }
    public IEnumerable<GivingMigrationPurgeItemRes> Items { get; set; } = [];
}
