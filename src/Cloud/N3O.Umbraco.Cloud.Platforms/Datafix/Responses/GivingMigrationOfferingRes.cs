using System;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationOfferingRes {
    public Guid LegacyOptionId { get; set; }
    public string LegacyOptionName { get; set; }
    public string LegacyOptionAlias { get; set; }
    public bool LegacyPublished { get; set; }
    public string OfferingName { get; set; }
    public string OfferingContentTypeAlias { get; set; }
    public string Status { get; set; }
    public Guid? TargetOfferingId { get; set; }
}
