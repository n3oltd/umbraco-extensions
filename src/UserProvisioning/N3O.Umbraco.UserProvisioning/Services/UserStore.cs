using AsyncKeyedLock;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.UserProvisioning.Entities;
using N3O.Umbraco.UserProvisioning.Exceptions;
using N3O.Umbraco.UserProvisioning.Extensions;
using N3O.Umbraco.UserProvisioning.Filters;
using N3O.Umbraco.UserProvisioning.Models;
using N3O.Umbraco.Utilities;
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

    private readonly AsyncKeyedLocker<string> _locker;
    private readonly UserProvisioningSettings _settings;
    private readonly IScimState _state;
    private readonly IUserService _userService;

    public UserStore(AsyncKeyedLocker<string> locker,
                     UserProvisioningSettings settings,
                     IScimState state,
                     IUserService userService) {
        _locker = locker;
        _settings = settings;
        _state = state;
        _userService = userService;
    }

    public async Task<ScimUser> CreateAsync(ScimUser resource) {
        Validate(resource, null);

        var email = GetEmail(resource);

        if (!email.HasValue()) {
            throw ScimException.InvalidValue("A user requires either userName or a primary email address");
        }

        // No group is assigned: granting one so the user can be read back grants access the directory
        // never asked for, and nothing later takes it away
        var user = _userService.CreateUserWithIdentity(email, email);
        user.IsApproved = resource.Active ?? true;
        user.Name = GetName(resource, email);

        _userService.Save(user);

        // Saving assigns the persisted key, so the in-memory entity is not the one this user is read by
        var created = _userService.GetByUsername(email);

        if (created == null) {
            throw new ScimException(HttpStatusCode.InternalServerError,
                                    $"User {email.Quote()} was created but could not be read back");
        }

        await RecordAsync(created.Key, resource, true);

        return await ReadAsync(created);
    }

    public async Task DeleteAsync(string id) {
        await MutateAsync(id, user => {
            SetActive(user, false);

            return Task.FromResult<ScimUser>(null);
        });
    }

    public async Task<ScimUser> GetAsync(string id) {
        return await ReadAsync(await GetRequiredAsync(id));
    }

    public async Task<ScimListResponse<ScimUser>> ListAsync(ScimQuery query) {
        var states = (await _state.GetUsersAsync()).ToDictionary(x => x.Id.Value);

        var matching = (await GetGovernedAsync()).Select(x => Map(x, states.GetValueOrDefault(x.Key)))
                                    .Where(x => query.Filter == null || query.Filter.Matches(Describe(x)))
                                    .OrderBy(x => x.UserName, StringComparer.InvariantCultureIgnoreCase)
                                    .Select(ToScim)
                                    .ToList();

        return ScimPaging.Page(matching, query);
    }

    public static BackOfficeUser Map(IUser user) {
        return Map(user, null);
    }

    // Umbraco holds one name, so the parts are kept as sent rather than recovered by splitting it
    public static BackOfficeUser Map(IUser user, ScimUserState state) {
        var backOfficeUser = new BackOfficeUser();
        backOfficeUser.Active = user.IsApproved;
        backOfficeUser.Email = user.Email;
        backOfficeUser.ExternalId = state?.ExternalId;
        backOfficeUser.FamilyName = state?.FamilyName;
        backOfficeUser.GivenName = state?.GivenName;
        backOfficeUser.Id = user.Key.ToString();
        backOfficeUser.Name = user.Name;
        backOfficeUser.UserName = user.Username;

        return backOfficeUser;
    }

    // One user is patched more than once in a cycle, and a change read before another's write is lost
    private async Task<ScimUser> MutateAsync(string id, Func<IUser, Task<ScimUser>> mutate) {
        using (await _locker.LockAsync(LockKey.Generate<UserStore>(id))) {
            return await mutate(await GetRequiredAsync(id));
        }
    }

    private async Task<ScimUser> ReadAsync(IUser user) {
        return ToScim(Map(user, await _state.GetUserAsync(user.Key)));
    }

    private async Task RecordAsync(Guid userKey, ScimUser resource, bool always = false) {
        var externalId = resource.ExternalId;
        var familyName = resource.Name?.FamilyName;
        var givenName = resource.Name?.GivenName;

        if (!always && !externalId.HasValue() && !familyName.HasValue() && !givenName.HasValue()) {
            return;
        }

        await _state.UpdateUserAsync(userKey, x => {
            if (externalId.HasValue()) {
                x.SetExternalId(externalId);
            }

            if (familyName.HasValue() || givenName.HasValue()) {
                x.SetName(givenName, familyName);
            }
        });
    }

    public async Task<ScimUser> PatchAsync(string id, IEnumerable<ScimPatchOperation> operations) {
        return await MutateAsync(id, async user => {
            var externalId = ScimGroupPatch.ReadExternalId(operations);
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

            if (externalId.HasValue()) {
                patched.ExternalId = externalId;
            }

            Validate(patched, user);

            await RecordAsync(user.Key, patched);

            return await ApplyAsync(user, patched);
        });
    }

    public async Task<ScimUser> ReplaceAsync(ScimUser resource) {
        return await MutateAsync(resource.Id, async user => {
            Validate(resource, user);

            await RecordAsync(user.Key, resource);

            return await ApplyAsync(user, resource);
        });
    }

    private async Task<ScimUser> ApplyAsync(IUser user, ScimUser resource) {
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

        return await ReadAsync(user);
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

    private async Task<IReadOnlyList<IUser>> GetGovernedAsync() {
        var provisioned = (await _state.GetUsersAsync()).Select(x => x.Id.Value).ToHashSet();

        return GetAll(_userService).Where(x => IsGoverned(x, provisioned.Contains(x.Key))).ToList();
    }

    // IUserService offers no lookup by key, so the governed set is the only route from a SCIM ID to a
    // user
    private async Task<IUser> GetRequiredAsync(string id) {
        if (!Guid.TryParse(id, out var key)) {
            throw ScimException.NotFound($"No user found with ID {id.Quote()}");
        }

        var user = (await GetGovernedAsync()).SingleOrDefault(x => x.Key == key);

        if (user == null) {
            throw ScimException.NotFound($"No user found with ID {id.Quote()}");
        }

        return user;
    }

    // The governed domain is what keeps users created by hand outside the directory's reach; one the
    // directory created is its own whether or not it has been put in a group yet
    private bool IsGoverned(IUser user, bool provisioned) {
        return _settings.Governs(user.Username) &&
               (provisioned || user.Groups.Any(x => _settings.UserGroups.Any(g => g.Alias.Is(x.Alias))));
    }

    private void SetActive(IUser user, bool active) {
        user.IsApproved = active;

        _userService.Save(user);
    }

    private void Validate(ScimUser resource, IUser user) {
        if (resource.Emails.OrEmpty().Any(x => x == null)) {
            throw ScimException.InvalidValue("An email cannot be null");
        }

        var email = GetEmail(resource);

        if (email.HasValue() && !_settings.Governs(email)) {
            throw ScimException.InvalidValue($"Email {email.Quote()} is not in a governed domain");
        }

        if (!email.HasValue() || email.Is(user?.Email)) {
            return;
        }

        var holders = new[] { _userService.GetByEmail(email), _userService.GetByUsername(email) };

        if (holders.Any(x => x != null && x.Key != user?.Key)) {
            throw ScimException.Conflict($"A user with email {email.Quote()} already exists");
        }
    }

    private static ScimAttributes Describe(BackOfficeUser user) {
        return new ScimAttributes().Add("active", user.Active)
                                   .Add("displayName", user.Name)
                                   .Add("emails.value", user.Email)
                                   .Add("externalId", user.ExternalId)
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
        name.FamilyName = user.FamilyName.HasValue() ? user.FamilyName : FamilyName(user.Name);
        name.Formatted = user.Name;
        name.GivenName = user.GivenName.HasValue() ? user.GivenName : GivenName(user.Name);

        var meta = new ScimMeta();
        meta.ResourceType = "User";

        var scimUser = new ScimUser();
        scimUser.Active = user.Active;
        scimUser.DisplayName = user.Name;
        scimUser.Emails = [email];
        scimUser.ExternalId = user.ExternalId;
        scimUser.Id = user.Id;
        scimUser.Meta = meta;
        scimUser.Name = name;
        scimUser.Schemas = [ScimConstants.Schemas.User];
        scimUser.UserName = user.UserName;

        return scimUser;
    }
}
