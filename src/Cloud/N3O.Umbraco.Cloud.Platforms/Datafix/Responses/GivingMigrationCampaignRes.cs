using System;
using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationCampaignRes {
    public Guid LegacyFormId { get; set; }
    public string LegacyUdi { get; set; }
    public string LegacyFormName { get; set; }
    public string LegacyFolderName { get; set; }
    public string LegacyPath { get; set; }
    public bool LegacyPublished { get; set; }
    public string CampaignName { get; set; }
    public string CampaignSlug { get; set; }
    public string CampaignContentTypeAlias { get; set; }
    public bool NameFromFolder { get; set; }
    public string Status { get; set; }
    public int ExpectedOfferings { get; set; }
    public bool TargetExists { get; set; }
    public Guid? TargetCampaignId { get; set; }
    public int CreatedOfferings { get; set; }
    public int PageReferences { get; set; }
    public bool Ready { get; set; }
    public string Message { get; set; }
    public IReadOnlyList<GivingMigrationOfferingRes> Offerings { get; set; } = [];
}
