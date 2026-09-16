using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationRunRes {
    public string SubscriptionId { get; set; }
    public string Message { get; set; }
    public GivingMigrationLockRes Lock { get; set; }
    public int Attempted { get; set; }
    public int CampaignsCreated { get; set; }
    public int OfferingsCreated { get; set; }
    public int Failed { get; set; }
    public int SkippedAlreadyMigrated { get; set; }
    public int SkippedBlocked { get; set; }
    public int RemainingPlanned { get; set; }
    public IReadOnlyList<GivingMigrationRunItemRes> Items { get; set; } = [];
}
