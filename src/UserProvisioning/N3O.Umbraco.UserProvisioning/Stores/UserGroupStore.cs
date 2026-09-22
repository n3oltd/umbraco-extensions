using N3O.Umbraco.Extensions;
using N3O.Umbraco.UserProvisioning.Models;
using Rsk.AspNetCore.Scim.Exceptions;
using Rsk.AspNetCore.Scim.Filters;
using Rsk.AspNetCore.Scim.Models;
using Rsk.AspNetCore.Scim.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using ScimGroup = Rsk.AspNetCore.Scim.Models.Group;
using ScimMember = Rsk.AspNetCore.Scim.Models.Member;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace N3O.Umbraco.UserProvisioning.Stores;

public class UserGroupStore : IScimStore<ScimGroup> {
    private readonly IPatchCommandExecutor _patchCommandExecutor;
    private readonly IScimQueryBuilderFactory _queryBuilderFactory;
    private readonly UserProvisioningSettings _settings;
    private readonly IUserGroupService _userGroupService;
    private readonly IUserService _userService;

    public UserGroupStore(IPatchCommandExecutor patchCommandExecutor,
                          IScimQueryBuilderFactory queryBuilderFactory,
                          UserProvisioningSettings settings,
                          IUserGroupService userGroupService,
                          IUserService userService) {
        _patchCommandExecutor = patchCommandExecutor;
        _queryBuilderFactory = queryBuilderFactory;
        _settings = settings;
        _userGroupService = userGroupService;
        _userService = userService;
    }

    public async Task<ScimGroup> Add(ScimGroup resource) {
        var group = await FindByDisplayNameAsync(resource.DisplayName);

        if (group == null) {
            throw new ScimStoreException($"No user group is mapped to {resource.DisplayName.Quote()}");
        }

        await SetMembersAsync(group, resource.Members?.Select(x => x.Value));

        return ToScim(await GetRequiredAsync(group.Id));
    }

    public Task Delete(string id) {
        throw new ScimStoreException("User groups cannot be deleted by the identity provider");
    }

    public async Task<IEnumerable<string>> Exists(IEnumerable<string> ids) {
        var all = await GetAllAsync();
        var keys = all.Select(x => x.Id).ToHashSet(StringComparer.InvariantCultureIgnoreCase);

        return ids.Where(keys.Contains).ToList();
    }

    public Task<ScimCursorPageResults<ScimGroup>> GetAll(ICursorResourceQuery query) {
        throw new NotSupportedException("Cursor pagination is not supported; the service provider uses " +
                                        "index pagination");
    }

    public async Task<ScimPageResults<ScimGroup>> GetAll(IIndexResourceQuery query) {
        var all = await GetAllAsync();

        var matching = _queryBuilderFactory.CreateQueryBuilder(all.AsQueryable())
                                           .Filter(query.Filter)
                                           .Build()
                                           .ToList();

        var builder = _queryBuilderFactory.CreateQueryBuilder(matching.AsQueryable())
                                          .Page(query.StartIndex, query.Count);

        if (query.Sort != null) {
            builder = builder.Sort(query.Sort.By, query.Sort.Direction);
        }

        var page = builder.Build().ToList();

        return new ScimPageResults<ScimGroup>(page.Select(ToScim).ToList(), matching.Count);
    }

    public async Task<ScimGroup> GetById(string id, ResourceAttributeSet attributes) {
        return ToScim(await GetRequiredAsync(id));
    }

    public async Task<ScimGroup> PartialUpdate(string resourceId, IEnumerable<PatchCommand> updates) {
        var group = await GetRequiredAsync(resourceId);
        var patched = ToScim(group);

        foreach (var update in updates) {
            _patchCommandExecutor.Execute(patched, update);
        }

        await SetMembersAsync(group, patched.Members?.Select(x => x.Value));

        return ToScim(await GetRequiredAsync(resourceId));
    }

    public async Task<ScimGroup> Update(ScimGroup resource) {
        var group = await GetRequiredAsync(resource.Id);

        await SetMembersAsync(group, resource.Members?.Select(x => x.Value));

        return ToScim(await GetRequiredAsync(resource.Id));
    }

    private async Task<BackOfficeUserGroup> FindByDisplayNameAsync(string displayName) {
        var all = await GetAllAsync();

        return all.SingleOrDefault(x => x.DisplayName.EqualsInvariant(displayName));
    }

    private async Task<IReadOnlyList<BackOfficeUserGroup>> GetAllAsync() {
        var groups = new List<BackOfficeUserGroup>();

        foreach (var (displayName, alias) in _settings.UserGroups.OrderBy(x => x.Key)) {
            var userGroup = await _userGroupService.GetAsync(alias);

            if (userGroup == null) {
                continue;
            }

            groups.Add(Map(displayName, userGroup));
        }

        return groups;
    }

    private async Task<BackOfficeUserGroup> GetRequiredAsync(string id) {
        var all = await GetAllAsync();
        var group = all.SingleOrDefault(x => x.Id.EqualsInvariant(id));

        if (group == null) {
            throw new ScimStoreItemDoesNotExistException($"No user group found with id {id.Quote()}");
        }

        return group;
    }

    private BackOfficeUserGroup Map(string displayName, IUserGroup userGroup) {
        var members = _userService.GetAllInGroup(userGroup.Id).Select(UserStore.Map).ToList();

        var group = new BackOfficeUserGroup();
        group.Alias = userGroup.Alias;
        group.DisplayName = displayName;
        group.Id = userGroup.Key.ToString();
        group.Members = members;

        return group;
    }

    private async Task SetMembersAsync(BackOfficeUserGroup group, IEnumerable<string> memberIds) {
        if (memberIds == null) {
            return;
        }

        var groupKey = Guid.Parse(group.Id);
        var wanted = new HashSet<Guid>();

        foreach (var memberId in memberIds.Where(x => x.HasValue())) {
            if (!Guid.TryParse(memberId, out var memberKey)) {
                throw new ScimStoreException($"Member {memberId.Quote()} is not a user id");
            }

            wanted.Add(memberKey);
        }

        var held = group.Members.Select(x => Guid.Parse(x.Id)).ToHashSet();

        var added = wanted.Except(held).ToArray();
        var removed = held.Except(wanted).ToArray();

        if (added.Any()) {
            var model = new UsersToUserGroupManipulationModel(groupKey, added);
            var attempt = await _userGroupService.AddUsersToUserGroupAsync(model,
                                                                           UmbracoConstants.Security.SuperUserKey);

            if (!attempt.Success) {
                throw new ScimStoreException($"Could not add users to group {group.Alias.Quote()}: {attempt.Result}");
            }
        }

        if (removed.Any()) {
            var model = new UsersToUserGroupManipulationModel(groupKey, removed);
            var attempt = await _userGroupService.RemoveUsersFromUserGroupAsync(model,
                                                                                UmbracoConstants.Security.SuperUserKey);

            if (!attempt.Success) {
                throw new ScimStoreException($"Could not remove users from group " +
                                             $"{group.Alias.Quote()}: {attempt.Result}");
            }
        }
    }

    private static ScimGroup ToScim(BackOfficeUserGroup group) {
        var members = group.Members.Select(x => {
            var member = new ScimMember();
            member.Display = x.Name;
            member.Type = "User";
            member.Value = x.Id;

            return member;
        }).ToList();

        var scimGroup = new ScimGroup();
        scimGroup.DisplayName = group.DisplayName;
        scimGroup.Id = group.Id;
        scimGroup.Members = members;

        return scimGroup;
    }
}
