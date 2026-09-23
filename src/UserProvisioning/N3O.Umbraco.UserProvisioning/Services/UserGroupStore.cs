using Microsoft.Extensions.Logging;
using N3O.Umbraco.Extensions;
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
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace N3O.Umbraco.UserProvisioning.Services;

public class UserGroupStore : IScimStore<ScimGroup> {
    private readonly ILogger<UserGroupStore> _logger;
    private readonly UserProvisioningSettings _settings;
    private readonly IUserService _userService;

    public UserGroupStore(ILogger<UserGroupStore> logger,
                          UserProvisioningSettings settings,
                          IUserService userService) {
        _logger = logger;
        _settings = settings;
        _userService = userService;
    }

    // User groups belong to the site, so a create is only ever the provisioning service reconciling
    // one the configuration already names
    public Task<ScimGroup> CreateAsync(ScimGroup resource) {
        var group = GetAll().SingleOrDefault(x => x.DisplayName.Is(resource.DisplayName));

        if (group == null) {
            throw ScimException.InvalidValue($"No user group is mapped to {resource.DisplayName.Quote()}");
        }

        SetMembers(group, ScimGroupPatch.ParseKeys(resource.Members));

        return Task.FromResult(ToScim(GetRequired(group.Id)));
    }

    public Task DeleteAsync(string id) {
        throw new ScimException(HttpStatusCode.BadRequest, "User groups cannot be deleted by the identity provider");
    }

    public Task<ScimGroup> GetAsync(string id) {
        return Task.FromResult(ToScim(GetRequired(id)));
    }

    public Task<ScimListResponse<ScimGroup>> ListAsync(ScimQuery query) {
        var matching = GetAll().Where(x => query.Filter == null || query.Filter.Matches(Describe(x)))
                               .OrderBy(x => x.DisplayName, StringComparer.InvariantCultureIgnoreCase)
                               .Select(ToScim)
                               .ToList();

        return Task.FromResult(ScimPaging.Page(matching, query));
    }

    public Task<ScimGroup> PatchAsync(string id, IEnumerable<ScimPatchOperation> operations) {
        var group = GetRequired(id);

        if (operations.OrEmpty().Any() && !operations.Any(ScimGroupPatch.IsMembership)) {
            _logger.LogWarning("Patch of group {DisplayName} changed no membership; paths were {Paths}",
                               group.DisplayName,
                               string.Join(", ", operations.Select(x => x.Path ?? "(none)")));
        }

        SetMembers(group, ScimGroupPatch.Resolve(group.Members, operations));

        return Task.FromResult(ToScim(GetRequired(id)));
    }

    public Task<ScimGroup> ReplaceAsync(ScimGroup resource) {
        var group = GetRequired(resource.Id);

        SetMembers(group, ScimGroupPatch.ParseKeys(resource.Members));

        return Task.FromResult(ToScim(GetRequired(resource.Id)));
    }

    // A user leaving every mapped group is a user this endpoint can no longer read, so they are
    // disabled here rather than left enabled and out of reach
    private void DisableUngoverned(IEnumerable<IUser> removed) {
        foreach (var user in removed) {
            if (user.IsApproved && !IsGoverned(user)) {
                user.IsApproved = false;

                _userService.Save(user);
            }
        }
    }

    private IReadOnlyList<BackOfficeUserGroup> GetAll() {
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

            groups.Add(Map(group.DisplayName, userGroup));
        }

        return groups;
    }

    private BackOfficeUserGroup GetRequired(string id) {
        var group = GetAll().SingleOrDefault(x => x.Id.Is(id));

        if (group == null) {
            throw ScimException.NotFound($"No user group found with id {id.Quote()}");
        }

        return group;
    }

    private bool IsGoverned(IUser user) {
        return user.Groups.Any(x => _settings.UserGroups.Any(g => g.Alias.Is(x.Alias)));
    }

    private BackOfficeUserGroup Map(string displayName, IUserGroup userGroup) {
        var members = _userService.GetAllInGroup(userGroup.Id)
                                  .Where(x => x.Id != UmbracoConstants.Security.SuperUserId)
                                  .Select(UserStore.Map)
                                  .ToList();

        var group = new BackOfficeUserGroup();
        group.Alias = userGroup.Alias;
        group.DisplayName = displayName;
        group.Id = Identify(displayName);
        group.Members = members;

        return group;
    }

    private void SetMembers(BackOfficeUserGroup group, ISet<Guid> wanted) {
        if (wanted == null) {
            return;
        }

        var held = group.Members.Select(x => Guid.Parse(x.Id)).ToHashSet();
        var added = wanted.Except(held).ToHashSet();
        var removed = held.Except(wanted).ToHashSet();

        if (!added.Any() && !removed.Any()) {
            return;
        }

        var userGroup = _userService.GetUserGroupByAlias(group.Alias);
        var users = UserStore.GetAll(_userService).Where(x => added.Contains(x.Key) || removed.Contains(x.Key));

        // Membership is set on each user rather than on the group, because assigning a user set to a
        // group replaces the whole set and would drop anyone the directory does not know about
        foreach (var user in users.ToList()) {
            if (added.Contains(user.Key)) {
                user.AddGroup(userGroup.ToReadOnlyGroup());
            } else {
                user.RemoveGroup(group.Alias);
            }

            _userService.Save(user);
        }

        DisableUngoverned(UserStore.GetAll(_userService).Where(x => removed.Contains(x.Key)));
    }

    // Two directory groups may name the same Umbraco group, and SCIM requires an id per resource, so
    // the id is derived from the directory group's name rather than taken from Umbraco
    private static string Identify(string displayName) {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(displayName.ToLowerInvariant()));

        return new Guid(hash).ToString();
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
