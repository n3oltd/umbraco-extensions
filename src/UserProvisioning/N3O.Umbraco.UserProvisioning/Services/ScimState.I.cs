using N3O.Umbraco.UserProvisioning.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.UserProvisioning.Services;

public interface IScimState {
    Task<ScimGroupState> GetGroupAsync(string groupId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScimGroupState>> GetGroupsAsync(IEnumerable<string> groupIds,
                                                       CancellationToken cancellationToken = default);
    Task<ScimUserState> GetUserAsync(Guid userKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScimUserState>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task UpdateGroupAsync(string groupId, Action<ScimGroupState> update, CancellationToken cancellationToken = default);
    Task UpdateUserAsync(Guid userKey, Action<ScimUserState> update, CancellationToken cancellationToken = default);
}
