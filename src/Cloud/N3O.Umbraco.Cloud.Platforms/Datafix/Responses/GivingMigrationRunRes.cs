using System;
using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationRunRes {
    public string SubscriptionId { get; set; }
    public string Message { get; set; }
    public int Attempted { get; set; }
    public int Created { get; set; }
    public int Failed { get; set; }
    public int AlreadyMigrated { get; set; }
    public int OfferingsCreated { get; set; }
    public int CrossSellsCreated { get; set; }
    public Guid? IconMediaId { get; set; }
    public Guid? ImageMediaId { get; set; }
    public Guid? HeroImageMediaId { get; set; }
    public IReadOnlyList<GivingMigrationRunItemRes> Items { get; set; } = [];
}
