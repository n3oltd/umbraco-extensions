using System;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationRewriteItemRes {
    public int PageId { get; set; }
    public Guid PageKey { get; set; }
    public string PageName { get; set; }
    public bool IsBlueprint { get; set; }
    public string PropertyAlias { get; set; }
    public int References { get; set; }
    public int Rewritten { get; set; }
    public string Outcome { get; set; }
    public string Message { get; set; }
}
