using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationRewriteRes {
    public string SubscriptionId { get; set; }
    public string Message { get; set; }
    public bool Preview { get; set; }
    public int PagesScanned { get; set; }
    public int PagesMatched { get; set; }
    public int PagesRewritten { get; set; }
    public int ReferencesFound { get; set; }
    public int ReferencesRewritten { get; set; }
    public int ReferencesUnmapped { get; set; }
    public int Failed { get; set; }
    public IReadOnlyList<GivingMigrationRewriteItemRes> Items { get; set; } = [];
    public IReadOnlyList<GivingMigrationIssueRes> Issues { get; set; } = [];
}
