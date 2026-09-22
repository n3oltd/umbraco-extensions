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
using Umbraco.Cms.Core.Services.OperationStatus;
using ScimUser = Rsk.AspNetCore.Scim.Models.User;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace N3O.Umbraco.UserProvisioning.Stores;

public class UserStore : IScimStore<ScimUser> {
    private const int PageSize = 500;

    private readonly IPatchCommandExecutor _patchCommandExecutor;
    private readonly IScimQueryBuilderFactory _queryBuilderFactory;
    private readonly UserProvisioningSettings _settings;
    private readonly IUserGroupService _userGroupService;
    private readonly IUserService _userService;

    public UserStore(IPatchCommandExecutor patchCommandExecutor,
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

    public async Task<ScimUser> Add(ScimUser resource) {
        var email = GetEmail(resource);

        if (!email.HasValue()) {
            throw new ScimStoreException("A user requires either userName or a primary email address");
        }

        var existing = await FindByEmailAsync(email);

        if (existing != null) {
            throw new ScimStoreItemAlreadyExistException($"A user with email {email.Quote()} already exists");
        }

        var userGroup = await _userGroupService.GetAsync(_settings.DefaultUserGroupAlias);

        if (userGroup == null) {
            throw new ScimStoreException($"No user group exists with alias {_settings.DefaultUserGroupAlias.Quote()}");
        }

        var model = new UserCreateModel();
        model.Email = email;
        model.Name = GetName(resource, email);
        model.UserGroupKeys = new HashSet<Guid> { userGroup.Key };
        model.UserName = email;

        var attempt = await _userService.CreateAsync(UmbracoConstants.Security.SuperUserKey, model, true);

        if (!attempt.Success) {
            throw new ScimStoreException($"Could not create user {email.Quote()}: {attempt.Status}");
        }

        var created = Map(attempt.Result.CreatedUser);

        if (resource.Active == false) {
            await SetActiveAsync(attempt.Result.CreatedUser.Key, false);
            created.Active = false;
        }

        return ToScim(created);
    }

    public async Task Delete(string id) {
        var user = await GetRequiredAsync(id);

        await SetActiveAsync(user.Key, false);
    }

    public async Task<IEnumerable<string>> Exists(IEnumerable<string> ids) {
        var all = await GetAllUsersAsync();
        var keys = all.Select(x => x.Key.ToString()).ToHashSet(StringComparer.InvariantCultureIgnoreCase);

        return ids.Where(keys.Contains).ToList();
    }

    public Task<ScimCursorPageResults<ScimUser>> GetAll(ICursorResourceQuery query) {
        throw new NotSupportedException("Cursor pagination is not supported; the service provider uses " +
                                        "index pagination");
    }

    public async Task<ScimPageResults<ScimUser>> GetAll(IIndexResourceQuery query) {
        var all = await GetAllAsync();

        var matching = _queryBuilderFactory.CreateQueryBuilder(all)
                                           .Filter(query.Filter)
                                           .Build()
                                           .ToList();

        var builder = _queryBuilderFactory.CreateQueryBuilder(matching.AsQueryable())
                                          .Page(query.StartIndex, query.Count);

        if (query.Sort != null) {
            builder = builder.Sort(query.Sort.By, query.Sort.Direction);
        }

        var page = builder.Build().ToList();

        return new ScimPageResults<ScimUser>(page.Select(ToScim).ToList(), matching.Count);
    }

    public async Task<ScimUser> GetById(string id, ResourceAttributeSet attributes) {
        var user = await GetRequiredAsync(id);

        return ToScim(Map(user));
    }

    public async Task<ScimUser> PartialUpdate(string resourceId, IEnumerable<PatchCommand> updates) {
        var user = await GetRequiredAsync(resourceId);
        var patched = Map(user);

        foreach (var update in updates) {
            _patchCommandExecutor.Execute(patched, update);
        }

        await ApplyAsync(user, patched);

        return ToScim(patched);
    }

    public async Task<ScimUser> Update(ScimUser resource) {
        var user = await GetRequiredAsync(resource.Id);

        var updated = Map(user);
        updated.Active = resource.Active ?? updated.Active;
        updated.Email = GetEmail(resource).HasValue() ? GetEmail(resource) : updated.Email;
        updated.Name = GetName(resource, updated.Name);

        await ApplyAsync(user, updated);

        return ToScim(updated);
    }

    private async Task ApplyAsync(IUser user, BackOfficeUser updated) {
        if (!updated.Name.EqualsInvariant(user.Name) || !updated.Email.EqualsInvariant(user.Email)) {
            // Umbraco marks ExistingUserKey required, so this one model cannot be built by assignment
            var model = new UserUpdateModel { ExistingUserKey = user.Key };
            model.Email = updated.Email;
            model.Name = updated.Name;
            model.UserGroupKeys = user.Groups.Select(x => x.Key).ToHashSet();
            model.UserName = updated.Email;

            var attempt = await _userService.UpdateAsync(UmbracoConstants.Security.SuperUserKey, model);

            if (!attempt.Success) {
                throw new ScimStoreException($"Could not update user {user.Key}: {attempt.Status}");
            }
        }

        if (updated.Active != IsActive(user)) {
            await SetActiveAsync(user.Key, updated.Active);
        }
    }

    private async Task<IUser> FindByEmailAsync(string email) {
        var all = await GetAllUsersAsync();

        return all.SingleOrDefault(x => x.Email.EqualsInvariant(email));
    }

    private async Task<IQueryable<BackOfficeUser>> GetAllAsync() {
        var all = await GetAllUsersAsync();

        return all.Select(Map).AsQueryable();
    }

    private async Task<IReadOnlyList<IUser>> GetAllUsersAsync() {
        var users = new List<IUser>();
        var skip = 0;

        while (true) {
            var attempt = await _userService.GetAllAsync(UmbracoConstants.Security.SuperUserKey, skip, PageSize);

            if (!attempt.Success) {
                throw new ScimStoreException($"Could not read users: {attempt.Status}");
            }

            var items = attempt.Result.Items.ToList();

            users.AddRange(items);

            skip += PageSize;

            if (!items.Any() || users.Count >= attempt.Result.Total) {
                break;
            }
        }

        return users;
    }

    private async Task<IUser> GetRequiredAsync(string id) {
        if (!Guid.TryParse(id, out var key)) {
            throw new ScimStoreItemDoesNotExistException($"No user found with id {id.Quote()}");
        }

        var user = await _userService.GetAsync(key);

        if (user == null) {
            throw new ScimStoreItemDoesNotExistException($"No user found with id {id.Quote()}");
        }

        return user;
    }

    private async Task SetActiveAsync(Guid key, bool active) {
        var keys = new HashSet<Guid> { key };

        var status = active
                         ? await _userService.EnableAsync(UmbracoConstants.Security.SuperUserKey, keys)
                         : await _userService.DisableAsync(UmbracoConstants.Security.SuperUserKey, keys);

        if (status != UserOperationStatus.Success) {
            throw new ScimStoreException($"Could not set user {key} active to {active}: {status}");
        }
    }

    internal static BackOfficeUser Map(IUser user) {
        var backOfficeUser = new BackOfficeUser();
        backOfficeUser.Active = IsActive(user);
        backOfficeUser.Email = user.Email;
        backOfficeUser.Id = user.Key.ToString();
        backOfficeUser.Name = user.Name;
        backOfficeUser.UserName = user.Username;

        return backOfficeUser;
    }

    internal static ScimUser ToScim(BackOfficeUser user) {
        var email = new Email();
        email.Primary = true;
        email.Type = "work";
        email.Value = user.Email;

        var name = new Name();
        name.Formatted = user.Name;

        var scimUser = new ScimUser();
        scimUser.Active = user.Active;
        scimUser.DisplayName = user.Name;
        scimUser.Emails = new[] { email };
        scimUser.Id = user.Id;
        scimUser.Name = name;
        scimUser.UserName = user.UserName;

        return scimUser;
    }

    private static string GetEmail(ScimUser resource) {
        var primary = resource.Emails?.FirstOrDefault(x => x.Primary)?.Value;

        return primary.HasValue() ? primary : resource.UserName;
    }

    private static bool IsActive(IUser user) {
        return user.UserState != UserState.Disabled;
    }

    private static string GetName(ScimUser resource, string fallback) {
        var parts = new[] { resource.Name?.GivenName, resource.Name?.FamilyName };
        var name = string.Join(" ", parts.Where(x => x.HasValue()));

        if (name.HasValue()) {
            return name;
        }

        return resource.DisplayName.HasValue() ? resource.DisplayName : fallback;
    }
}
