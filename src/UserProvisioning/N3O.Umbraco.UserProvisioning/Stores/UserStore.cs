using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.UserProvisioning.Filters;
using N3O.Umbraco.UserProvisioning.Models;
using N3O.Umbraco.UserProvisioning.Scim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace N3O.Umbraco.UserProvisioning.Stores;

public class UserStore : IScimStore<ScimUser> {
    private const int PageSize = 500;

    private readonly IEntityService _entityService;
    private readonly GlobalSettings _globalSettings;
    private readonly ILogger<UserStore> _logger;
    private readonly UserProvisioningSettings _settings;
    private readonly IUserGroupService _userGroupService;
    private readonly IUserService _userService;

    public UserStore(IEntityService entityService,
                     IOptions<GlobalSettings> globalSettings,
                     ILogger<UserStore> logger,
                     UserProvisioningSettings settings,
                     IUserGroupService userGroupService,
                     IUserService userService) {
        _entityService = entityService;
        _globalSettings = globalSettings.Value;
        _logger = logger;
        _settings = settings;
        _userGroupService = userGroupService;
        _userService = userService;
    }

    public async Task<ScimUser> CreateAsync(ScimUser resource) {
        var email = GetEmail(resource);

        if (!email.HasValue()) {
            throw ScimException.InvalidValue("A user requires either userName or a primary email address");
        }

        if (await FindByEmailAsync(email) != null) {
            throw ScimException.Conflict($"A user with email {email.Quote()} already exists");
        }

        var userGroup = await _userGroupService.GetAsync(_settings.DefaultUserGroupAlias);

        if (userGroup == null) {
            throw new ScimException(HttpStatusCode.InternalServerError,
                                    $"No user group exists with alias {_settings.DefaultUserGroupAlias.Quote()}");
        }

        var model = new UserCreateModel();
        model.Email = email;
        model.Name = GetName(resource, email);
        model.UserGroupKeys = new HashSet<Guid> { userGroup.Key };
        model.UserName = email;

        var attempt = await _userService.CreateAsync(UmbracoConstants.Security.SuperUserKey, model, true);

        if (!attempt.Success) {
            throw new ScimException(HttpStatusCode.InternalServerError,
                                    $"Could not create user {email.Quote()}: {attempt.Status}");
        }

        var created = attempt.Result.CreatedUser;

        if (resource.Active == false) {
            await SetActiveAsync(created.Key, false);
        }

        return ToScim(Map(await GetRequiredAsync(created.Key.ToString())));
    }

    public async Task DeleteAsync(string id) {
        var user = await GetRequiredAsync(id);

        await SetActiveAsync(user.Key, false);
    }

    public async Task<ScimUser> GetAsync(string id) {
        return ToScim(Map(await GetRequiredAsync(id)));
    }

    public async Task<ScimListResponse<ScimUser>> ListAsync(ScimQuery query) {
        var all = await GetAllUsersAsync();

        var matching = all.Select(Map)
                          .Where(x => query.Filter == null || query.Filter.Matches(Describe(x)))
                          .OrderBy(x => x.UserName, StringComparer.InvariantCultureIgnoreCase)
                          .Select(ToScim)
                          .ToList();

        return ScimPaging.Page(matching, query);
    }

    public static BackOfficeUser Map(IUser user) {
        var backOfficeUser = new BackOfficeUser();
        backOfficeUser.Active = user.IsApproved;
        backOfficeUser.Email = user.Email;
        backOfficeUser.Id = user.Key.ToString();
        backOfficeUser.Name = user.Name;
        backOfficeUser.UserName = user.Username;

        return backOfficeUser;
    }

    public async Task<ScimUser> PatchAsync(string id, IEnumerable<ScimPatchOperation> operations) {
        var user = await GetRequiredAsync(id);
        var patched = ToScim(Map(user));

        foreach (var operation in operations.OrEmpty()) {
            ScimUserPatch.Apply(patched, operation);
        }

        return await ApplyAsync(user, patched);
    }

    public async Task<ScimUser> ReplaceAsync(ScimUser resource) {
        var user = await GetRequiredAsync(resource.Id);

        return await ApplyAsync(user, resource);
    }

    private async Task<ScimUser> ApplyAsync(IUser user, ScimUser resource) {
        var current = Map(user);

        var updated = new BackOfficeUser();
        updated.Active = resource.Active ?? current.Active;
        updated.Email = GetEmail(resource).HasValue() ? GetEmail(resource) : current.Email;
        updated.Id = current.Id;
        updated.Name = GetName(resource, current.Name);
        updated.UserName = updated.Email;

        if (RequiresUpdate(current, updated)) {
            var model = new UserUpdateModel { ExistingUserKey = user.Key };
            model.ContentStartNodeKeys = GetKeys(user.StartContentIds, UmbracoObjectTypes.Document);
            model.Email = updated.Email;
            model.HasContentRootAccess = HasRootAccess(user.StartContentIds);
            model.HasMediaRootAccess = HasRootAccess(user.StartMediaIds);
            model.LanguageIsoCode = user.Language.HasValue() ? user.Language : _globalSettings.DefaultUILanguage;
            model.MediaStartNodeKeys = GetKeys(user.StartMediaIds, UmbracoObjectTypes.Media);
            model.Name = updated.Name;
            model.UserGroupKeys = user.Groups.Select(x => x.Key).ToHashSet();
            model.UserName = updated.Email;

            var attempt = await _userService.UpdateAsync(UmbracoConstants.Security.SuperUserKey, model);

            if (!attempt.Success) {
                throw new ScimException(HttpStatusCode.InternalServerError,
                                        $"Could not update user {user.Key}: {attempt.Status}");
            }
        }

        if (updated.Active != current.Active) {
            await SetActiveAsync(user.Key, updated.Active);
        }

        return ToScim(Map(await GetRequiredAsync(user.Key.ToString())));
    }

    private async Task<IUser> FindByEmailAsync(string email) {
        var all = await GetAllUsersAsync();

        return all.SingleOrDefault(x => x.Email.EqualsInvariant(email));
    }

    private async Task<IReadOnlyList<IUser>> GetAllUsersAsync() {
        var users = new List<IUser>();
        var skip = 0;

        while (true) {
            var attempt = await _userService.GetAllAsync(UmbracoConstants.Security.SuperUserKey, skip, PageSize);

            if (!attempt.Success) {
                throw new ScimException(HttpStatusCode.InternalServerError, $"Could not read users: {attempt.Status}");
            }

            var items = attempt.Result.Items.ToList();

            users.AddRange(items.Where(x => x.Key != UmbracoConstants.Security.SuperUserKey));

            skip += PageSize;

            if (!items.Any() || skip >= attempt.Result.Total) {
                break;
            }
        }

        return users;
    }

    private ISet<Guid> GetKeys(IEnumerable<int> ids, UmbracoObjectTypes objectType) {
        var keys = ids.OrEmpty()
                      .Select(x => _entityService.GetKey(x, objectType))
                      .Where(x => x.Success)
                      .Select(x => x.Result);

        return new HashSet<Guid>(keys);
    }

    private async Task<IUser> GetRequiredAsync(string id) {
        if (!Guid.TryParse(id, out var key) || key == UmbracoConstants.Security.SuperUserKey) {
            throw ScimException.NotFound($"No user found with id {id.Quote()}");
        }

        var user = await _userService.GetAsync(key);

        if (user == null) {
            throw ScimException.NotFound($"No user found with id {id.Quote()}");
        }

        return user;
    }

    private async Task SetActiveAsync(Guid key, bool active) {
        var keys = new HashSet<Guid> { key };

        var status = active
                         ? await _userService.EnableAsync(UmbracoConstants.Security.SuperUserKey, keys)
                         : await _userService.DisableAsync(UmbracoConstants.Security.SuperUserKey, keys);

        if (status != UserOperationStatus.Success) {
            throw new ScimException(HttpStatusCode.InternalServerError,
                                    $"Could not set user {key} active to {active}: {status}");
        }
    }

    private static ScimAttributes Describe(BackOfficeUser user) {
        return new ScimAttributes().Add("active", user.Active)
                                   .Add("displayName", user.Name)
                                   .Add("emails.value", user.Email)
                                   .Add("id", user.Id)
                                   .Add("name.familyName", FamilyName(user.Name))
                                   .Add("name.formatted", user.Name)
                                   .Add("name.givenName", GivenName(user.Name))
                                   .Add("userName", user.UserName);
    }

    private static string FamilyName(string name) {
        var parts = SplitName(name);

        return parts.Length > 1 ? parts[1] : null;
    }

    private static string GetEmail(ScimUser resource) {
        var primary = resource.Emails.OrEmpty().FirstOrDefault(x => x.Primary)?.Value;

        return primary.HasValue() ? primary : resource.UserName;
    }

    private static string GetName(ScimUser resource, string fallback) {
        var parts = new[] { resource.Name?.GivenName, resource.Name?.FamilyName };
        var name = string.Join(" ", parts.Where(x => x.HasValue()));

        if (name.HasValue()) {
            return name;
        }

        if (resource.Name?.Formatted.HasValue() == true) {
            return resource.Name.Formatted;
        }

        return resource.DisplayName.HasValue() ? resource.DisplayName : fallback;
    }

    private static string GivenName(string name) {
        var parts = SplitName(name);

        return parts.Length > 0 ? parts[0] : null;
    }

    private static bool HasRootAccess(IEnumerable<int> startNodeIds) {
        return startNodeIds.OrEmpty().Contains(UmbracoConstants.System.Root);
    }

    private static bool RequiresUpdate(BackOfficeUser current, BackOfficeUser updated) {
        return !updated.Email.EqualsInvariant(current.Email) || !updated.Name.EqualsInvariant(current.Name);
    }

    private static string[] SplitName(string name) {
        return name.HasValue() ? name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries) : [];
    }

    private static ScimUser ToScim(BackOfficeUser user) {
        var email = new ScimEmail();
        email.Primary = true;
        email.Type = "work";
        email.Value = user.Email;

        var name = new ScimName();
        name.FamilyName = FamilyName(user.Name);
        name.Formatted = user.Name;
        name.GivenName = GivenName(user.Name);

        var meta = new ScimMeta();
        meta.ResourceType = "User";

        var scimUser = new ScimUser();
        scimUser.Active = user.Active;
        scimUser.DisplayName = user.Name;
        scimUser.Emails = [email];
        scimUser.Id = user.Id;
        scimUser.Meta = meta;
        scimUser.Name = name;
        scimUser.Schemas = [ScimConstants.Schemas.User];
        scimUser.UserName = user.UserName;

        return scimUser;
    }

}
