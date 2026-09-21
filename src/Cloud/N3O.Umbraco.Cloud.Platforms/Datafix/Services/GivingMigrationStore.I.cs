using N3O.Umbraco.Cloud.Platforms.Models;
using System;
using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public interface IGivingMigrationStore {
    void AppendLedger(IEnumerable<GivingMigrationLedgerEntry> entries);
    void DeleteLockSnapshot();
    void DeletePlan();
    IReadOnlyList<GivingMigrationLedgerEntry> GetLedger();
    IReadOnlyDictionary<string, IReadOnlyList<string>> GetLockSnapshot();
    GivingMigrationPersistedPlanRes GetPlan();
    IReadOnlyDictionary<string, Guid> GetPlaceholderMedia();
    void SaveLockSnapshot(IReadOnlyDictionary<string, IReadOnlyList<string>> snapshot);
    void SavePlaceholderMedia(IReadOnlyDictionary<string, Guid> media);
    GivingMigrationPersistedPlanRes SavePlan(GivingMigrationPlanRes plan);
}
