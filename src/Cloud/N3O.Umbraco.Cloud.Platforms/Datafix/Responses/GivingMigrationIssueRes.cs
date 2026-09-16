using System;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationIssueRes {
    public string Kind { get; set; }
    public string Severity { get; set; }
    public Guid? LegacyId { get; set; }
    public string LegacyName { get; set; }
    public string LegacyPath { get; set; }
    public string PropertyAlias { get; set; }
    public string Detail { get; set; }
}
