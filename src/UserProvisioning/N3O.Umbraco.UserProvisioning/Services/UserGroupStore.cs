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
            await SetMembersAsync(group, ScimGroupPatch.ParseKeys(resource.Members));
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
            await SetMembersAsync(group, ScimGroupPatch.Resolve(group.Members, operations));
        });
    }

    public async Task<ScimGroup> ReplaceAsync(ScimGroup resource) {
        return await MutateAsync(resource.Id, async group => {
            await SetExternalIdAsync(group, resource.ExternalId);
            await SetMembersAsync(group, ScimGroupPatch.ParseKeys(resource.Members));
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

        foreach (var group in _settings.UserGroups.OrderBy(x => x.DisplayName)) {
            var userGroup = _userService.GetUserGroupByAlias(group.Alias);

            if (userGroup == null) {
                _logger.LogWarning("Directory group {DisplayName} maps to user group {Alias}, which this site does " +
                                   "not have, so nobody in it can be provisioned",
                                   group.DisplayName,
                                   group.Alias);

                continue;
            }

            groups.Add(Map(group.DisplayName, userGroup, await _state.GetGroupAsync(Identify(group.DisplayName))));
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

    // Visibility now survives leaving every group, so the test for disabling is the groups
    // themselves rather than whether the endpoint can still read the user
    private bool HoldsMappedGroup(IUser user) {
        return user.Groups.Any(x => _settings.UserGroups.Any(g => g.Alias.Is(x.Alias)));
    }

    // Membership is what the directory asserted for this group, not everyone holding the Umbraco
    // group, because two directory groups may map to one alias and each owns only its own members.
    // No record at all is not an empty record: it means the directory has never said, and until it
    // does the members it was given are taken to be the ones already there. Reading it as "nobody"
    // would hide every existing member, and a removal for someone the endpoint cannot see is
    // answered successfully without disabling them
    private BackOfficeUserGroup Map(string displayName, IUserGroup userGroup, ScimGroupState state) {
        var holders = _userService.GetAllInGroup(userGroup.Id)
                                  .Where(x => x.Id != UmbracoConstants.Security.SuperUserId)
                                  .Where(x => _settings.Governs(x.Username))
                                  .ToList();

        var asserted = state?.Members.ToHashSet() ?? Inherited(userGroup.Alias, holders);

        var members = holders.Where(x => asserted.Contains(x.Key)).Select(UserStore.Map).ToList();

        var group = new BackOfficeUserGroup();
        group.Alias = userGroup.Alias;
        group.DisplayName = displayName;
        group.ExternalId = state?.ExternalId;
        group.Id = Identify(displayName);
        group.Members = members;

        return group;
    }

    // The membership to write is decided from a read of the membership held, so the lock has to span
    // both. It is taken on the Umbraco group rather than the directory group, because two directory
    // groups may share one and each decides whether to keep the membership by reading the other
    private async Task<ScimGroup> MutateAsync(string id, Func<BackOfficeUserGroup, Task> mutate) {
        var alias = _settings.UserGroups.FirstOrDefault(x => Identify(x.DisplayName).Is(id))?.Alias ?? id;

        using (await _locker.LockAsync(LockKey.Generate<UserGroupStore>(alias))) {
            await mutate(await GetRequiredAsync(id));

            return ToScim(await GetRequiredAsync(id));
        }
    }

    // Members already in the Umbraco group predate any record of who put them there. Where one
    // directory group feeds it they can only have come from that one, so they are taken as its
    // members and a leaver can still be found and removed. Where several feed it there is nothing to
    // tell them apart, and guessing would let one group hold a member the directory never gave it and
    // block their removal from the group that did
    private ISet<Guid> Inherited(string alias, IEnumerable<IUser> holders) {
        var feeders = _settings.UserGroups.Count(x => x.Alias.Is(alias));

        return feeders == 1 ? holders.Select(x => x.Key).ToHashSet() : new HashSet<Guid>();
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

    private async Task SetMembersAsync(BackOfficeUserGroup group, ISet<Guid> wanted) {
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

        // Recorded last. A save that fails leaves the record saying the member is still there, so the
        // next attempt sees them and can try again; recording first would hide them from every read
        await _state.UpdateGroupAsync(group.Id, x => x.SetMembers(wanted));

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
