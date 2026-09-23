using N3O.Umbraco.Extensions;
using N3O.Umbraco.UserProvisioning.Exceptions;
using N3O.Umbraco.UserProvisioning.Extensions;
using N3O.Umbraco.UserProvisioning.Filters;
using N3O.Umbraco.UserProvisioning.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace N3O.Umbraco.UserProvisioning.Services;

public class UserStore : IScimStore<ScimUser> {
    private const int PageSize = 500;

    private readonly UserProvisioningSettings _settings;
    private readonly IUserService _userService;

    public UserStore(UserProvisioningSettings settings, IUserService userService) {
        _settings = settings;
        _userService = userService;
    }

    public Task<ScimUser> CreateAsync(ScimUser resource) {
        var email = GetEmail(resource);

        if (!email.HasValue()) {
            throw ScimException.InvalidValue("A user requires either userName or a primary email address");
        }

        if (_userService.GetByEmail(email) != null || _userService.GetByUsername(email) != null) {
            throw ScimException.Conflict($"A user with email {email.Quote()} already exists");
        }

        var userGroup = _userService.GetUserGroupByAlias(_settings.DefaultUserGroupAlias);

        if (userGroup == null) {
            throw new ScimException(HttpStatusCode.InternalServerError,
                                    $"No user group exists with alias {_settings.DefaultUserGroupAlias.Quote()}");
        }

        var user = _userService.CreateUserWithIdentity(email, email);
        user.AddGroup(userGroup.ToReadOnlyGroup());
        user.IsApproved = resource.Active ?? true;
        user.Name = GetName(resource, email);

        _userService.Save(user);

        return Task.FromResult(ToScim(Map(user)));
    }

    public Task DeleteAsync(string id) {
        var user = GetRequired(id);

        SetActive(user, false);

        return Task.CompletedTask;
    }

    public Task<ScimUser> GetAsync(string id) {
        return Task.FromResult(ToScim(Map(GetRequired(id))));
    }

    public Task<ScimListResponse<ScimUser>> ListAsync(ScimQuery query) {
        var matching = GetGoverned().Select(Map)
                                    .Where(x => query.Filter == null || query.Filter.Matches(Describe(x)))
                                    .OrderBy(x => x.UserName, StringComparer.InvariantCultureIgnoreCase)
                                    .Select(ToScim)
                                    .ToList();

        return Task.FromResult(ScimPaging.Page(matching, query));
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

    public Task<ScimUser> PatchAsync(string id, IEnumerable<ScimPatchOperation> operations) {
        var user = GetRequired(id);
        var original = ToScim(Map(user));
        var patched = ToScim(Map(user));

        foreach (var operation in operations.OrEmpty()) {
            ScimUserPatch.Apply(patched, operation);
        }

        // Name parts are derived from the stored name on every read, so an unchanged name would
        // otherwise outrank a displayName the patch did change
        if (Unchanged(original.Name, patched.Name) && !Same(original.DisplayName, patched.DisplayName)) {
            patched.Name = null;
        }

        return Task.FromResult(Apply(user, patched));
    }

    public Task<ScimUser> ReplaceAsync(ScimUser resource) {
        return Task.FromResult(Apply(GetRequired(resource.Id), resource));
    }

    private ScimUser Apply(IUser user, ScimUser resource) {
        var current = Map(user);

        var updated = new BackOfficeUser();
        updated.Active = resource.Active ?? current.Active;
        updated.Email = GetEmail(resource).HasValue() ? GetEmail(resource) : current.Email;
        updated.Id = current.Id;
        updated.Name = GetName(resource, current.Name);
        updated.UserName = updated.Email;

        if (!updated.Email.Is(current.Email) || !updated.Name.Is(current.Name)) {
            user.Email = updated.Email;
            user.Name = updated.Name;
            user.Username = updated.Email;

            _userService.Save(user);
        }

        if (updated.Active != current.Active) {
            SetActive(user, updated.Active);
        }

        return ToScim(Map(user));
    }

    public static IReadOnlyList<IUser> GetAll(IUserService userService) {
        var users = new List<IUser>();
        long page = 0;

        while (true) {
            var items = userService.GetAll(page,
                                           PageSize,
                                           out var total,
                                           nameof(IUser.Username),
                                           Direction.Ascending,
                                           null,
                                           null,
                                           null)
                                   .ToList();

            users.AddRange(items.Where(x => x.Id != UmbracoConstants.Security.SuperUserId));

            page++;

            if (!items.Any() || page * PageSize >= total) {
                break;
            }
        }

        return users;
    }

    private IReadOnlyList<IUser> GetGoverned() {
        return GetAll(_userService).Where(IsGoverned).ToList();
    }

    // IUserService offers no lookup by key, so the governed set is the only route from a SCIM id to a
    // user
    private IUser GetRequired(string id) {
        if (!Guid.TryParse(id, out var key)) {
            throw ScimException.NotFound($"No user found with id {id.Quote()}");
        }

        var user = GetGoverned().SingleOrDefault(x => x.Key == key);

        if (user == null) {
            throw ScimException.NotFound($"No user found with id {id.Quote()}");
        }

        return user;
    }

    // Scoping every read and write to the mapped groups is what keeps users created by hand outside
    // the directory's reach
    private bool IsGoverned(IUser user) {
        return user.Groups.Any(x => _settings.UserGroups.Values.Any(alias => alias.Is(x.Alias)));
    }

    private void SetActive(IUser user, bool active) {
        user.IsApproved = active;

        _userService.Save(user);
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

    private static bool Same(string left, string right) {
        return string.Equals(left, right, StringComparison.InvariantCulture);
    }

    private static string[] SplitName(string name) {
        return name.HasValue() ? name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries) : [];
    }

    private static bool Unchanged(ScimName before, ScimName after) {
        return Same(before?.FamilyName, after?.FamilyName) &&
               Same(before?.Formatted, after?.Formatted) &&
               Same(before?.GivenName, after?.GivenName);
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
