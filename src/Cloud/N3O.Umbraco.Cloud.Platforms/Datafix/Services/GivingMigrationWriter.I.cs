using N3O.Umbraco.Cloud.Platforms.Models;
using System;

namespace N3O.Umbraco.Cloud.Platforms;

public interface IGivingMigrationWriter {
    Guid? GetCampaignsContainerId();
    GivingPlaceholders BuildPlaceholders(Guid mediaId, string analyticsTag);
    GivingMigrationRunItemRes CreateCampaign(GivingMigrationCampaignRes plan,
                                             Guid containerId,
                                             GivingPlaceholders placeholders);
    GivingMigrationRunItemRes PublishOfferings(GivingMigrationCampaignRes plan);
}
