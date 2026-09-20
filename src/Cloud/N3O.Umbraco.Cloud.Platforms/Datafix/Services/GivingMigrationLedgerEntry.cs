using System;

namespace N3O.Umbraco.Cloud.Platforms;

// Persisted so the block rewrite can resolve a legacy form reference to the campaign it became, and so a re-run
// knows what already exists.
public class GivingMigrationLedgerEntry {
    public Guid LegacyId { get; set; }
    public Guid NewId { get; set; }
    public string Kind { get; set; }
    public string Name { get; set; }
}
