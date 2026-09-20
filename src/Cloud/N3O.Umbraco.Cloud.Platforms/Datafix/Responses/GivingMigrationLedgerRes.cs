using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationLedgerRes {
    public int Campaigns { get; set; }
    public int Offerings { get; set; }
    public int CrossSells { get; set; }
    public IReadOnlyList<GivingMigrationLedgerEntryRes> Entries { get; set; } = [];
}
