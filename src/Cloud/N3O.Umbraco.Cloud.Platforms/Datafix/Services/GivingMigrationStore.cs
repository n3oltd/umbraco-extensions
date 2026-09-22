using N3O.Umbraco.Cloud.Platforms.Models;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Json;
using Newtonsoft.Json;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using UmbracoLocks = Umbraco.Cms.Core.Constants.Locks;

namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public class GivingMigrationStore : IGivingMigrationStore {
    private readonly IKeyValueService _keyValueService;
    private readonly IJsonProvider _jsonProvider;
    private readonly ICoreScopeProvider _scopeProvider;
    private readonly IClock _clock;

    public GivingMigrationStore(IKeyValueService keyValueService,
                                IJsonProvider jsonProvider,
                                ICoreScopeProvider scopeProvider,
                                IClock clock) {
        _keyValueService = keyValueService;
        _jsonProvider = jsonProvider;
        _scopeProvider = scopeProvider;
        _clock = clock;
    }

    // The ledger is one key value row read, modified and written back, so two overlapping runs would each write a
    // version missing the other's entries. The lock is taken in the database, which covers every caller and every
    // instance, rather than in this process.
    public void AppendLedger(IEnumerable<GivingMigrationLedgerEntry> entries) {
        InLock(() => {
            var byLegacyId = GetLedger().ToDictionary(x => x.LegacyId);

            foreach (var entry in entries.OrEmpty()) {
                byLegacyId[entry.LegacyId] = entry;
            }

            Save(GivingMigrationConstants.KeyValueKeys.Ledger, byLegacyId.Values.ToList());
        });
    }

    public void DeleteLockSnapshot() {
        _keyValueService.SetValue(GivingMigrationConstants.KeyValueKeys.TreeLockSnapshot, string.Empty);
    }

    public void DeletePlan() {
        _keyValueService.SetValue(GivingMigrationConstants.KeyValueKeys.PersistedPlan, string.Empty);
    }

    public IReadOnlyList<GivingMigrationLedgerEntry> GetLedger() {
        return Read<List<GivingMigrationLedgerEntry>>(GivingMigrationConstants.KeyValueKeys.Ledger) ?? [];
    }

    public IReadOnlyDictionary<string, IReadOnlyList<string>> GetLockSnapshot() {
        var snapshot = Read<Dictionary<string, IReadOnlyList<string>>>(GivingMigrationConstants.KeyValueKeys
                                                                                               .TreeLockSnapshot);

        return snapshot ?? new Dictionary<string, IReadOnlyList<string>>();
    }

    public IReadOnlyDictionary<string, Guid> GetPlaceholderMedia() {
        var media = Read<Dictionary<string, Guid>>(GivingMigrationConstants.KeyValueKeys.Placeholders);

        return media ?? new Dictionary<string, Guid>();
    }

    public GivingMigrationPersistedPlanRes GetPlan() {
        return Read<GivingMigrationPersistedPlanRes>(GivingMigrationConstants.KeyValueKeys.PersistedPlan);
    }

    public void SaveLockSnapshot(IReadOnlyDictionary<string, IReadOnlyList<string>> snapshot) {
        InLock(() => Save(GivingMigrationConstants.KeyValueKeys.TreeLockSnapshot, snapshot));
    }

    public void SavePlaceholderMedia(IReadOnlyDictionary<string, Guid> media) {
        InLock(() => Save(GivingMigrationConstants.KeyValueKeys.Placeholders, media));
    }

    public GivingMigrationPersistedPlanRes SavePlan(GivingMigrationPlanRes plan) {
        var res = new GivingMigrationPersistedPlanRes();
        res.SavedAt = _clock.GetCurrentInstant();
        res.Plan = plan;

        InLock(() => Save(GivingMigrationConstants.KeyValueKeys.PersistedPlan, res));

        return res;
    }

    private void InLock(Action action) {
        using (var scope = _scopeProvider.CreateCoreScope()) {
            scope.EagerWriteLock(UmbracoLocks.KeyValues);

            action();

            scope.Complete();
        }
    }

    // NodaTime values do not round-trip through a bare JsonConvert, so persisted state always goes through the
    // framework provider.
    private T Read<T>(string key) where T : class {
        var json = _keyValueService.GetValue(key);

        return json.HasValue() ? _jsonProvider.DeserializeObject<T>(json) : null;
    }

    private void Save(string key, object value) {
        _keyValueService.SetValue(key, _jsonProvider.SerializeObject(value, Formatting.None));
    }
}
