using System;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationCrossSellRes {
    public Guid LegacyUpsellId { get; set; }
    public string LegacyUpsellName { get; set; }
    public string LegacyPath { get; set; }
    public string CrossSellName { get; set; }
    public string CrossSellContentTypeAlias { get; set; }
    public string Status { get; set; }
    public string Message { get; set; }
    public bool Ready { get; set; }
    public Guid? TargetCrossSellId { get; set; }
}
