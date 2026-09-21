using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationRewriteRes {
    public string Message { get; set; }
    public bool Preview { get; set; }
    public int PagesScanned { get; set; }
    public int PagesMatched { get; set; }
    public int PagesRewritten { get; set; }
    public int ReferencesFound { get; set; }
    public int ReferencesRewritten { get; set; }
    public int ReferencesUnmapped { get; set; }
    public int Failed { get; set; }
    public IEnumerable<GivingMigrationRewriteItemRes> Items { get; set; } = [];
    public IEnumerable<GivingMigrationIssueRes> Issues { get; set; } = [];
}
