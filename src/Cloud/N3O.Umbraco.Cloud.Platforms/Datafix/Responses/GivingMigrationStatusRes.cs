using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationStatusRes {
    public string SubscriptionId { get; set; }
    public bool Complete { get; set; }
    public int LegacyForms { get; set; }
    public int LegacyDonationOptions { get; set; }
    public int UnmigratedForms { get; set; }
    public int UnmigratedCrossSells { get; set; }
    public int CampaignsWithOfferingMismatch { get; set; }
    public int Campaigns { get; set; }
    public int PublishedCampaigns { get; set; }
    public int Offerings { get; set; }
    public int PublishedOfferings { get; set; }
    public IEnumerable<GivingMigrationStatusItemRes> Items { get; set; } = [];
}
