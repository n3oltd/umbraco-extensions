using System;
using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationRunItemRes {
    public Guid LegacyFormId { get; set; }
    public string LegacyPath { get; set; }
    public string CampaignName { get; set; }
    public Guid? CampaignId { get; set; }
    public int OfferingsExpected { get; set; }
    public int OfferingsCreated { get; set; }
    public string Outcome { get; set; }
    public string Message { get; set; }
    public IReadOnlyList<string> InvalidProperties { get; set; } = [];
}
