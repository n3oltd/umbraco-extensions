using System;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationLedgerEntryRes {
    public Guid LegacyId { get; set; }
    public Guid NewId { get; set; }
    public string Kind { get; set; }
    public string Name { get; set; }
}
