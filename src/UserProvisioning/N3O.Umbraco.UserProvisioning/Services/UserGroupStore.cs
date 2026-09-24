using AsyncKeyedLock;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.UserProvisioning.Entities;
using N3O.Umbraco.UserProvisioning.Exceptions;
using N3O.Umbraco.UserProvisioning.Extensions;
using N3O.Umbraco.UserProvisioning.Filters;
using N3O.Umbraco.UserProvisioning.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using N3O.Umbraco.Utilities;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace N3O.Umbraco.UserProvisioning.Services;

public class UserGroupStore : IScimStore<ScimGroup> {
    private readonly AsyncKeyedLocker<string> _locker;
    private readonly ILogger<UserGroupStore> _logger;
    private readonly UserProvisioningSettings _settings;
    private readonly IScimState _state;
    private readonly IUserService _userService;

    public UserGroupStore(AsyncKeyedLocker<string> locker,
                          ILogger<UserGroupStore> logger,
                          UserProvisioningSettings settings,
                          IScimState state,
                          IUserService userService) {
        _locker = locker;
        _logger = logger;
        _settings = settings;
        _state = state;
        _userService = userService;
    }

    // User groups belong to the site, so a create is only ever the provisioning service reconciling
    // one the configuration already names
    public async Task<ScimGroup> CreateAsync(ScimGroup resource) {
        var named = (await GetAllAsync()).SingleOrDefault(x => x.DisplayName.Is(resource.DisplayName));

        if (named == null) {
            throw ScimException.InvalidValue($"No user group is mapped to {resource.DisplayName.Quote()}");
        }

        return await MutateAsync(named.Id, async group => {
            await SetExternalIdAsync(group, resource.ExternalId);
            await SetMembersAsync(group, ScimGroupPatch.ParseKeys(resource.Members), null);
        });
    }

    public Task DeleteAsync(string id) {
        throw new ScimException(HttpStatusCode.BadRequest, "User groups cannot be deleted by the identity provider");
    }

    public async Task<ScimGroup> GetAsync(string id) {
        return ToScim(await GetRequiredAsync(id));
    }

    public async Task<ScimListResponse<ScimGroup>> ListAsync(ScimQuery query) {
        var matching = (await GetAllAsync()).Where(x => query.Filter == null || query.Filter.Matches(Describe(x)))
                                            .OrderBy(x => x.DisplayName, StringComparer.InvariantCultureIgnoreCase)
                                            .Select(ToScim)
                                            .ToList();

        return ScimPaging.Page(matching, query);
    }

    public async Task<ScimGroup> PatchAsync(string id, IEnumerable<ScimPatchOperation> operations) {
        return await MutateAsync(id, async group => {
            if (operations.OrEmpty().Any() && !operations.Any(ScimGroupPatch.IsMembership)) {
                _logger.LogWarning("Patch of group {DisplayName} changed no membership; paths were {Paths}",
                                   group.DisplayName,
                                   string.Join(", ", operations.Select(x => x.Path ?? "(none)")));
            }

            await SetExternalIdAsync(group, ScimGroupPatch.ReadExternalId(operations));
            await SetMembersAsync(group,
                                  ScimGroupPatch.Resolve(group.Members, operations),
                                  ScimGroupPatch.Named(group.Members, operations));
        });
    }

    public async Task<ScimGroup> ReplaceAsync(ScimGroup resource) {
        return await MutateAsync(resource.Id, async group => {
            await SetExternalIdAsync(group, resource.ExternalId);
            await SetMembersAsync(group, ScimGroupPatch.ParseKeys(resource.Members), null);
        });
    }

    // A user leaving every mapped group is a user this endpoint can no longer read, so they are
    // disabled here rather than left enabled and out of reach
    private void DisableUngoverned(IEnumerable<IUser> removed) {
        foreach (var user in removed) {
            if (user.IsApproved && !HoldsMappedGroup(user)) {
                user.IsApproved = false;

                _userService.Save(user);
            }
        }
    }

    private async Task<IReadOnlyList<BackOfficeUserGroup>> GetAllAsync() {
        var groups = new List<BackOfficeUserGroup>();
        var mapped = new List<(UserProvisioningGroup Group, IUserGroup UserGroup)>();
        var states = new Dictionary<string, ScimGroupState>();

        foreach (var group in _settings.UserGroups.OrderBy(x => x.DisplayName)) {
            var userGroup = _userService.GetUserGroupByAlias(group.Alias);

            if (userGroup == null) {
                _logger.LogWarning("Directory group {DisplayName} maps to user group {Alias}, which this site does " +
                                   "not have, so nobody in it can be provisioned",
                                   group.DisplayName,
                                   group.Alias);

                continue;
            }

            states[group.DisplayName] = await _state.GetGroupAsync(Identify(group.DisplayName));
            mapped.Add((group, userGroup));
        }

        // Decided on every read, so that changing which directory groups feed a user group re-decides
        // who is inherited rather than leaving the previous answer written down
        foreach (var (group, userGroup) in mapped) {
            var claimed = _settings.UserGroups
                                   .Where(x => x.Alias.Is(group.Alias))
                                   .SelectMany(x => states.GetValueOrDefault(x.DisplayName)?.Members ?? [])
                                   .ToHashSet();

            groups.Add(Map(group.DisplayName, userGroup, states[group.DisplayName], claimed));
        }

        return groups;
    }

    private async Task<BackOfficeUserGroup> GetRequiredAsync(string id) {
        var group = (await GetAllAsync()).SingleOrDefault(x => x.Id.Is(id));

        if (group == null) {
            throw ScimException.NotFound($"No user group found with ID {id.Quote()}");
        }

        return group;
    }

    // A user the endpoint can still read may hold no mapped group at all, so this is the test for
    // disabling rather than whether they are visible
    private bool HoldsMappedGroup(IUser user) {
        return user.Groups.Any(x => _settings.UserGroups.Any(g => g.Alias.Is(x.Alias)));
    }

    // Members are the ones this directory group was given, not everyone holding the Umbraco group,
    // because two directory groups may map to one alias and each owns only its own
    private BackOfficeUserGroup Map(string displayName,
                                    IUserGroup userGroup,
                                    ScimGroupState state,
                                    ISet<Guid> claimed) {
        var holders = _userService.GetAllInGroup(userGroup.Id)
                                  .Where(x => x.Id != UmbracoConstants.Security.SuperUserId)
                                  .Where(x => _settings.Governs(x.Username))
                                  .ToList();

        var asserted = state?.Members.ToHashSet() ?? [];
        var reported = asserted.Concat(Inherited(userGroup.Alias, holders, claimed)).ToHashSet();

        var members = holders.Where(x => reported.Contains(x.Key)).Select(UserStore.Map).ToList();

        var group = new BackOfficeUserGroup();
        group.Alias = userGroup.Alias;
        group.Asserted = asserted.ToList();
        group.DisplayName = displayName;
        group.ExternalId = state?.ExternalId;
        group.Id = Identify(displayName);
        group.Members = members;

        return group;
    }

    // The membership to write is decided from the membership read, so the lock spans both. It is
    // taken on the Umbraco group because two directory groups sharing one decide by reading each other
    private async Task<ScimGroup> MutateAsync(string id, Func<BackOfficeUserGroup, Task> mutate) {
        var alias = _settings.UserGroups.FirstOrDefault(x => Identify(x.DisplayName).Is(id))?.Alias ?? id;

        using (await _locker.LockAsync(LockKey.Generate<UserGroupStore>(alias))) {
            await mutate(await GetRequiredAsync(id));

            return ToScim(await GetRequiredAsync(id));
        }
    }

    // Members nobody claims predate any record of who put them there. With one feeding directory group
    // they can only have come from it, and a leaver has to be readable to be removed; with several,
    // claiming a member one was never given blocks their removal from the group that was
    private ISet<Guid> Inherited(string alias, IEnumerable<IUser> holders, ISet<Guid> claimed) {
        if (_settings.UserGroups.Count(x => x.Alias.Is(alias)) != 1) {
            return new HashSet<Guid>();
        }

        return holders.Select(x => x.Key).Where(x => !claimed.Contains(x)).ToHashSet();
    }

    private async Task<ISet<Guid>> OtherAssertionsAsync(BackOfficeUserGroup group) {
        var others = _settings.UserGroups
                              .Where(x => x.Alias.Is(group.Alias) && !Identify(x.DisplayName).Is(group.Id))
                              .Select(x => Identify(x.DisplayName));

        var states = await _state.GetGroupsAsync(others);

        return states.SelectMany(x => x.Members).ToHashSet();
    }

    private async Task SetExternalIdAsync(BackOfficeUserGroup group, string externalId) {
        if (!externalId.HasValue() || externalId.Is(group.ExternalId)) {
            return;
        }

        await _state.UpdateGroupAsync(group.Id, x => x.SetExternalId(externalId));
    }

    private async Task SetMembersAsync(BackOfficeUserGroup group, ISet<Guid> wanted, ISet<Guid> named) {
        if (wanted == null) {
            return;
        }

        var held = group.Members.Select(x => Guid.Parse(x.Id)).ToHashSet();
        var added = wanted.Except(held).ToHashSet();
        var removed = held.Except(wanted).ToHashSet();

        if (!added.Any() && !removed.Any()) {
            return;
        }

        var elsewhere = await OtherAssertionsAsync(group);
        var userGroup = _userService.GetUserGroupByAlias(group.Alias);
        var users = UserStore.GetAll(_userService)
                             .Where(x => added.Contains(x.Key) || removed.Contains(x.Key))
                             .Where(x => _settings.Governs(x.Username))
                             .ToList();

        var dropped = new List<Guid>();

        // Membership is set on each user rather than on the group, because assigning a user set to a
        // group replaces the whole set and would drop anyone the directory does not know about
        foreach (var user in users) {
            if (added.Contains(user.Key)) {
                user.AddGroup(userGroup.ToReadOnlyGroup());
            } else if (elsewhere.Contains(user.Key)) {
                continue;
            } else {
                user.RemoveGroup(group.Alias);

                dropped.Add(user.Key);
            }

            _userService.Save(user);
        }

        // Recorded last: a save that fails leaves the member in the record, so the next attempt can
        // still see them. Recording first hides them from every read and the retry does nothing
        await _state.UpdateGroupAsync(group.Id,
                                      x => x.SetMembers(group.Asserted
                                                             .Concat(named ?? wanted)
                                                             .Where(wanted.Contains)));

        DisableUngoverned(UserStore.GetAll(_userService).Where(x => dropped.Contains(x.Key)));
    }

    // Two directory groups may name the same Umbraco group, and SCIM requires an ID per resource, so
    // the ID is derived from the directory group's name rather than taken from Umbraco
    private static string Identify(string displayName) {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(displayName.ToLowerInvariant()));

        return new Guid(hash).ToString();
    }

    private static ScimAttributes Describe(BackOfficeUserGroup group) {
        return new ScimAttributes().Add("displayName", group.DisplayName)
                                   .Add("externalId", group.ExternalId)
                                   .Add("id", group.Id);
    }

    private static ScimGroup ToScim(BackOfficeUserGroup group) {
        var members = group.Members.Select(x => {
            var member = new ScimMember();
            member.Display = x.Name;
            member.Type = "User";
            member.Value = x.Id;

            return member;
        }).ToList();

        var meta = new ScimMeta();
        meta.ResourceType = "Group";

        var scimGroup = new ScimGroup();
        scimGroup.DisplayName = group.DisplayName;
        scimGroup.ExternalId = group.ExternalId;
        scimGroup.Id = group.Id;
        scimGroup.Members = members;
        scimGroup.Meta = meta;
        scimGroup.Schemas = [ScimConstants.Schemas.Group];

        return scimGroup;
    }
}
