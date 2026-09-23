using Microsoft.Extensions.Logging;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.UserProvisioning.Filters;
using N3O.Umbraco.UserProvisioning.Models;
using N3O.Umbraco.UserProvisioning.Scim;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace N3O.Umbraco.UserProvisioning.Stores;

public class UserGroupStore : IScimStore<ScimGroup> {
    private readonly ILogger<UserGroupStore> _logger;
    private readonly UserProvisioningSettings _settings;
    private readonly IUserGroupService _userGroupService;
    private readonly IUserService _userService;

    public UserGroupStore(ILogger<UserGroupStore> logger,
                          UserProvisioningSettings settings,
                          IUserGroupService userGroupService,
                          IUserService userService) {
        _logger = logger;
        _settings = settings;
        _userGroupService = userGroupService;
        _userService = userService;
    }

    // User groups belong to the site, so a create is only ever the provisioning service reconciling one
    // the configuration already names
    public async Task<ScimGroup> CreateAsync(ScimGroup resource) {
        var group = await FindByDisplayNameAsync(resource.DisplayName);

        if (group == null) {
            throw ScimException.InvalidValue($"No user group is mapped to {resource.DisplayName.Quote()}");
        }

        await SetMembersAsync(group, ScimGroupPatch.ParseKeys(resource.Members));

        return ToScim(await GetRequiredAsync(group.Id));
    }

    public Task DeleteAsync(string id) {
        throw new ScimException(HttpStatusCode.BadRequest, "User groups cannot be deleted by the identity provider");
    }

    public async Task<ScimGroup> GetAsync(string id) {
        return ToScim(await GetRequiredAsync(id));
    }

    public async Task<ScimListResponse<ScimGroup>> ListAsync(ScimQuery query) {
        var all = await GetAllAsync();

        var matching = all.Where(x => query.Filter == null || query.Filter.Matches(Describe(x)))
                          .OrderBy(x => x.DisplayName, StringComparer.InvariantCultureIgnoreCase)
                          .Select(ToScim)
                          .ToList();

        return ScimPaging.Page(matching, query);
    }

    public async Task<ScimGroup> PatchAsync(string id, IEnumerable<ScimPatchOperation> operations) {
        var group = await GetRequiredAsync(id);

        if (operations.OrEmpty().Any() && !operations.Any(ScimGroupPatch.IsMembership)) {
            _logger.LogWarning("Patch of group {DisplayName} changed no membership; paths were {Paths}",
                               group.DisplayName,
                               string.Join(", ", operations.Select(x => x.Path ?? "(none)")));
        }

        await SetMembersAsync(group, ScimGroupPatch.Resolve(group.Members, operations));

        return ToScim(await GetRequiredAsync(id));
    }

    public async Task<ScimGroup> ReplaceAsync(ScimGroup resource) {
        var group = await GetRequiredAsync(resource.Id);

        await SetMembersAsync(group, ScimGroupPatch.ParseKeys(resource.Members));

        return ToScim(await GetRequiredAsync(resource.Id));
    }

    private async Task<BackOfficeUserGroup> FindByDisplayNameAsync(string displayName) {
        var all = await GetAllAsync();

        return all.SingleOrDefault(x => x.DisplayName.Is(displayName));
    }

    private async Task<IReadOnlyList<BackOfficeUserGroup>> GetAllAsync() {
        var groups = new List<BackOfficeUserGroup>();

        foreach (var (displayName, alias) in _settings.UserGroups.OrderBy(x => x.Key)) {
            var userGroup = await _userGroupService.GetAsync(alias);

            if (userGroup == null) {
                _logger.LogWarning("Directory group {DisplayName} maps to user group {Alias}, which this site does " +
                                   "not have, so nobody in it can be provisioned",
                                   displayName,
                                   alias);

                continue;
            }

            groups.Add(Map(displayName, userGroup));
        }

        return groups;
    }

    private async Task<BackOfficeUserGroup> GetRequiredAsync(string id) {
        var all = await GetAllAsync();
        var group = all.SingleOrDefault(x => x.Id.Is(id));

        if (group == null) {
            throw ScimException.NotFound($"No user group found with id {id.Quote()}");
        }

        return group;
    }

    private BackOfficeUserGroup Map(string displayName, IUserGroup userGroup) {
        var members = _userService.GetAllInGroup(userGroup.Id)
                                  .Where(x => x.Key != UmbracoConstants.Security.SuperUserKey)
                                  .Select(UserStore.Map)
                                  .ToList();

        var group = new BackOfficeUserGroup();
        group.Alias = userGroup.Alias;
        group.DisplayName = displayName;
        group.Id = userGroup.Key.ToString();
        group.Members = members;

        return group;
    }

    private async Task SetMembersAsync(BackOfficeUserGroup group, ISet<Guid> wanted) {
        if (wanted == null) {
            return;
        }

        var groupKey = Guid.Parse(group.Id);
        var held = group.Members.Select(x => Guid.Parse(x.Id)).ToHashSet();

        var added = wanted.Except(held).ToArray();
        var removed = held.Except(wanted).ToArray();

        if (added.Any()) {
            var model = new UsersToUserGroupManipulationModel(groupKey, added);
            var attempt = await _userGroupService.AddUsersToUserGroupAsync(model,
                                                                          UmbracoConstants.Security.SuperUserKey);

            if (!attempt.Success) {
                throw new ScimException(HttpStatusCode.InternalServerError,
                                        $"Could not add users to group {group.Alias.Quote()}: {attempt.Result}");
            }
        }

        if (removed.Any()) {
            var model = new UsersToUserGroupManipulationModel(groupKey, removed);
            var attempt = await _userGroupService.RemoveUsersFromUserGroupAsync(model,
                                                                               UmbracoConstants.Security.SuperUserKey);

            if (!attempt.Success) {
                throw new ScimException(HttpStatusCode.InternalServerError,
                                        $"Could not remove users from group {group.Alias.Quote()}: {attempt.Result}");
            }
        }
    }

    private static ScimAttributes Describe(BackOfficeUserGroup group) {
        return new ScimAttributes().Add("displayName", group.DisplayName)
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
        scimGroup.Id = group.Id;
        scimGroup.Members = members;
        scimGroup.Meta = meta;
        scimGroup.Schemas = [ScimConstants.Schemas.Group];

        return scimGroup;
    }
}
