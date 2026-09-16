namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationSummaryRes {
    public int LegacyForms { get; set; }
    public int LegacyFolders { get; set; }
    public int LegacyOptions { get; set; }
    public int LegacyUpsellOffers { get; set; }
    public int PlannedCampaigns { get; set; }
    public int PlannedOfferings { get; set; }
    public int AlreadyMigratedCampaigns { get; set; }
    public int SkippedEmptyForms { get; set; }
    public int NamesTakenFromFolder { get; set; }
    public int SlugCollisions { get; set; }
    public int BlockedCampaigns { get; set; }
    public int UnreferencedCampaigns { get; set; }
    public bool Ready { get; set; }
}
