using System;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationStatusItemRes {
    public Guid LegacyFormId { get; set; }
    public string LegacyFormName { get; set; }
    public int LegacyDonationOptions { get; set; }
    public Guid CampaignId { get; set; }
    public string CampaignName { get; set; }
    public bool Published { get; set; }
    public int Offerings { get; set; }
    public int PublishedOfferings { get; set; }
    public bool OfferingsMatchOptions { get; set; }
}
