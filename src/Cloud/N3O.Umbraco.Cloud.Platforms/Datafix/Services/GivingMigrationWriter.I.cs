using N3O.Umbraco.Cloud.Platforms.Models;
using System;
using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public interface IGivingMigrationWriter {
    GivingMigrationRunItemRes CreateCampaign(GivingMigrationCampaignRes plan,
                                             Guid containerId,
                                             GivingPlaceholders placeholders,
                                             ICollection<GivingMigrationLedgerEntry> ledger);
    GivingMigrationRunItemRes CreateCrossSell(GivingMigrationCrossSellRes plan,
                                              Guid containerId,
                                              GivingPlaceholders placeholders,
                                              ICollection<GivingMigrationLedgerEntry> ledger);
    Guid? EnsureCrossSellsContainerId(out string problem);
    Guid? GetCampaignsContainerId();
    GivingMigrationRunItemRes PublishOfferings(GivingMigrationCampaignRes plan);
}
