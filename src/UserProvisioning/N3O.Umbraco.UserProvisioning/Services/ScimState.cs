using AsyncKeyedLock;
using N3O.Umbraco.Entities;
using N3O.Umbraco.UserProvisioning.Entities;
using N3O.Umbraco.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.UserProvisioning.Services;

public class ScimState : IScimState {
    private readonly IRepository<ScimGroupState> _groups;
    private readonly AsyncKeyedLocker<string> _locker;
    private readonly IRepository<ScimUserState> _users;

    public ScimState(IRepository<ScimGroupState> groups,
                     AsyncKeyedLocker<string> locker,
                     IRepository<ScimUserState> users) {
        _groups = groups;
        _locker = locker;
        _users = users;
    }

    public async Task<ScimGroupState> GetGroupAsync(string groupId, CancellationToken cancellationToken = default) {
        return await _groups.GetAsync(Identify(groupId), cancellationToken);
    }

    public async Task<IReadOnlyList<ScimGroupState>> GetGroupsAsync(IEnumerable<string> groupIds,
                                                                    CancellationToken cancellationToken = default) {
        var states = new List<ScimGroupState>();

        foreach (var groupId in groupIds) {
            var state = await GetGroupAsync(groupId, cancellationToken);

            if (state != null) {
                states.Add(state);
            }
        }

        return states;
    }

    public async Task<ScimUserState> GetUserAsync(Guid userKey, CancellationToken cancellationToken = default) {
        return await _users.GetAsync(new EntityId(userKey), cancellationToken);
    }

    public async Task<IReadOnlyList<ScimUserState>> GetUsersAsync(CancellationToken cancellationToken = default) {
        return (await _users.GetAllAsync(cancellationToken)).ToList();
    }

    public async Task UpdateGroupAsync(string groupId,
                                       Action<ScimGroupState> update,
                                       CancellationToken cancellationToken = default) {
        var id = Identify(groupId);

        using (await _locker.LockAsync(LockKey.Generate<ScimState>(id.ToString()), cancellationToken)) {
            var state = await _groups.GetAsync(id, cancellationToken);

            if (state == null) {
                state = ScimGroupState.Create(id);

                update(state);

                await _groups.InsertAsync(state);
            } else {
                update(state);

                await _groups.UpdateAsync(state);
            }
        }
    }

    public async Task UpdateUserAsync(Guid userKey,
                                      Action<ScimUserState> update,
                                      CancellationToken cancellationToken = default) {
        var id = new EntityId(userKey);

        using (await _locker.LockAsync(LockKey.Generate<ScimState>(id.ToString()), cancellationToken)) {
            var state = await _users.GetAsync(id, cancellationToken);

            if (state == null) {
                state = ScimUserState.Create(id);

                update(state);

                await _users.InsertAsync(state);
            } else {
                update(state);

                await _users.UpdateAsync(state);
            }
        }
    }

    private static EntityId Identify(string groupId) {
        return new EntityId(Guid.Parse(groupId));
    }
}
