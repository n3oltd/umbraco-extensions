using N3O.Umbraco.Constants;
using N3O.Umbraco.Entities;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Json;
using N3O.Umbraco.Types;
using N3O.Umbraco.UserProvisioning.Entities;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Persistence.Dtos;
using Umbraco.Extensions;

namespace N3O.Umbraco.UserProvisioning.Migrations;

// State written on Umbraco 13 is keyed by Id.ToGuid(), the user key that Umbraco 14's AddGuidsToUsers replaces.
// Written through the migration's own database so the re-key commits or rolls back with the plan step.
public class ScimUserKeysMigration : AsyncMigrationBase {
    private readonly IClock _clock;
    private readonly IJsonProvider _jsonProvider;

    public ScimUserKeysMigration(IMigrationContext context, IClock clock, IJsonProvider jsonProvider)
        : base(context) {
        _clock = clock;
        _jsonProvider = jsonProvider;
    }

    protected override async Task MigrateAsync() {
        if (!TableExists(Tables.Entities.Name)) {
            return;
        }

        var users = await Database.FetchAsync<UserDto>(Sql().Select<UserDto>(x => x.Id, x => x.Key)
                                                            .From<UserDto>());
        var keys = users.ToDictionary(x => x.Id.ToGuid(), x => x.Key);

        await RekeyUsersAsync(keys);
        await RemapMembersAsync(keys);
    }

    private async Task<IReadOnlyList<EntityRow>> FetchAsync<T>() {
        return await Database.FetchAsync<EntityRow>(Sql($"SELECT * FROM {Tables.Entities.Name} WHERE Type = @0",
                                                        TypeResolver.PersistedName(typeof(T))));
    }

    private async Task RekeyUsersAsync(IReadOnlyDictionary<Guid, Guid> keys) {
        var rows = await FetchAsync<ScimUserState>();

        foreach (var row in rows) {
            if (!keys.TryGetValue(row.Id, out var key)) {
                continue;
            }

            var old = _jsonProvider.DeserializeObject<ScimUserState>(row.Json);
            var existingRow = rows.SingleOrDefault(x => x.Id == key);

            if (existingRow == null) {
                var state = ScimUserState.Create(new EntityId(key));
                state.SetExternalId(old.ExternalId);
                state.SetName(old.GivenName, old.FamilyName);

                Database.Insert(Tables.Entities.Name, Tables.Entities.PrimaryKey, false, ToRow(state));
            } else {
                var state = _jsonProvider.DeserializeObject<ScimUserState>(existingRow.Json);

                if (!state.ExternalId.HasValue()) {
                    state.SetExternalId(old.ExternalId);
                }

                if (!state.GivenName.HasValue() && !state.FamilyName.HasValue()) {
                    state.SetName(old.GivenName, old.FamilyName);
                }

                await Database.UpdateAsync(ToRow(state));
            }

            await Database.ExecuteAsync(Sql($"DELETE FROM {Tables.Entities.Name} WHERE Id = @0", row.Id));
        }
    }

    private async Task RemapMembersAsync(IReadOnlyDictionary<Guid, Guid> keys) {
        var rows = await FetchAsync<ScimGroupState>();

        foreach (var row in rows) {
            var state = _jsonProvider.DeserializeObject<ScimGroupState>(row.Json);

            if (!state.Members.Any(keys.ContainsKey)) {
                continue;
            }

            state.SetMembers(state.Members.Select(x => keys.GetValueOrDefault(x, x)));

            await Database.UpdateAsync(ToRow(state));
        }
    }

    private EntityRow ToRow(Entity entity) {
        entity.OnSaving(_clock.GetCurrentInstant(), RevisionBehaviour.Increment);

        var row = new EntityRow();
        row.Id = entity.Id;
        row.Revision = entity.Revision;
        row.Timestamp = entity.Timestamp.ToDateTimeUtc();
        row.Type = TypeResolver.PersistedName(entity.GetType());
        row.Json = _jsonProvider.SerializeObject(entity);

        return row;
    }
}
