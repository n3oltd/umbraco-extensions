namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationSummaryRes {
    public int LegacyForms { get; set; }
    public int LegacyFolders { get; set; }
    public int LegacyOptions { get; set; }
    public int LegacyUpsellOffers { get; set; }
    public int PlannedCampaigns { get; set; }
    public int PlannedOfferings { get; set; }
    public int PlannedCrossSells { get; set; }
    public int AlreadyMigratedCampaigns { get; set; }
    public int EmptyForms { get; set; }
    public int FormsWithNestedForms { get; set; }
    public int NamesPrefixedWithFolder { get; set; }
    public int BlockedCampaigns { get; set; }
    public bool Ready { get; set; }
}
