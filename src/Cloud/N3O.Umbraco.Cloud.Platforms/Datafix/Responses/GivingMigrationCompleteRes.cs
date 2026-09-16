using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationCompleteRes {
    public string SubscriptionId { get; set; }
    public string Message { get; set; }
    public GivingMigrationLockRes Lock { get; set; }
    public int CampaignsVerified { get; set; }
    public int OfferingsPublished { get; set; }
    public int OfferingsFailed { get; set; }
    public IReadOnlyList<GivingMigrationRunItemRes> Items { get; set; } = [];
}
