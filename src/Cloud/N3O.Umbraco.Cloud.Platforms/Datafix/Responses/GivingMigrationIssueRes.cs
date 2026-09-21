using System;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationIssueRes {
    public string Kind { get; set; }
    public string Severity { get; set; }
    public Guid? LegacyId { get; set; }
    public string LegacyName { get; set; }
    public string LegacyPath { get; set; }

    // The page holding the reference, which is a different node from the legacy form the rest of these fields
    // describe, and the one an operator has to open to clear a blocking issue.
    public Guid? PageKey { get; set; }
    public string PageName { get; set; }
    public string PagePath { get; set; }
    public string PropertyAlias { get; set; }
    public string Detail { get; set; }
}
