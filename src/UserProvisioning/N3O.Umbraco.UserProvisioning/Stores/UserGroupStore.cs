using Microsoft.Extensions.Logging;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.UserProvisioning.Models;
using Rsk.AspNetCore.Scim.Exceptions;
using Rsk.AspNetCore.Scim.Filters;
using Rsk.AspNetCore.Scim.Models;
using Rsk.AspNetCore.Scim.Parsers;
using Rsk.AspNetCore.Scim.Stores;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using ScimGroup = Rsk.AspNetCore.Scim.Models.Group;
using ScimMember = Rsk.AspNetCore.Scim.Models.Member;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace N3O.Umbraco.UserProvisioning.Stores;

public class UserGroupStore : IScimStore<ScimGroup> {
    private readonly ILogger<UserGroupStore> _logger;
    private readonly IScimQueryBuilderFactory _queryBuilderFactory;
    private readonly UserProvisioningSettings _settings;
    private readonly IUserGroupService _userGroupService;
    private readonly IUserService _userService;

    public UserGroupStore(ILogger<UserGroupStore> logger,
                          IScimQueryBuilderFactory queryBuilderFactory,
                          UserProvisioningSettings settings,
                          IUserGroupService userGroupService,
                          IUserService userService) {
        _logger = logger;
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

        await SetMembersAsync(group, ParseKeys(resource.Members));

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

        var matching = Build(_queryBuilderFactory.CreateQueryBuilder(all.AsQueryable()).Filter(query.Filter));

        var builder = _queryBuilderFactory.CreateQueryBuilder(matching.AsQueryable())
                                          .Page(query.StartIndex, query.Count);

        if (query.Sort != null) {
            builder = builder.Sort(query.Sort.By, query.Sort.Direction);
        }

        var page = Build(builder);

        return new ScimPageResults<ScimGroup>(page.Select(ToScim).ToList(), matching.Count);
    }

    public async Task<ScimGroup> GetById(string id, ResourceAttributeSet attributes) {
        return ToScim(await GetRequiredAsync(id));
    }

    // The patch executor replaces a collection rather than appending to it, so an add of one member
    // would drop everybody already in the group
    public async Task<ScimGroup> PartialUpdate(string resourceId, IEnumerable<PatchCommand> updates) {
        var group = await GetRequiredAsync(resourceId);
        var members = group.Members.Select(x => Guid.Parse(x.Id)).ToHashSet();
        var applicable = updates.Where(x => IsMembersPath(x.Path)).ToList();

        if (!applicable.Any() && updates.Any()) {
            _logger.LogWarning("Patch of group {DisplayName} changed no membership; paths were {Paths}",
                               group.DisplayName,
                               string.Join(", ", updates.Select(x => x.Path?.ToString() ?? "(none)")));
        }

        foreach (var update in applicable) {
            var keys = GetMemberKeys(update);

            if (update.Operation == PatchOperation.Add) {
                members.UnionWith(keys);
            } else if (update.Operation == PatchOperation.Replace) {
                members.Clear();
                members.UnionWith(keys);
            } else if (keys.Any()) {
                members.ExceptWith(keys);
            } else {
                members.Clear();
            }
        }

        await SetMembersAsync(group, members);

        return ToScim(await GetRequiredAsync(resourceId));
    }

    public async Task<ScimGroup> Update(ScimGroup resource) {
        var group = await GetRequiredAsync(resource.Id);

        await SetMembersAsync(group, ParseKeys(resource.Members));

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
                throw new ScimStoreException($"Could not add users to group {group.Alias.Quote()}: {attempt.Result}");
            }
        }

        if (removed.Any()) {
            var model = new UsersToUserGroupManipulationModel(groupKey, removed);
            var attempt = await _userGroupService.RemoveUsersFromUserGroupAsync(model,
                                                                                UmbracoConstants.Security.SuperUserKey);

            if (!attempt.Success) {
                throw new ScimStoreException($"Could not remove users from group {group.Alias.Quote()}: " +
                                             $"{attempt.Result}");
            }
        }
    }

    private static IEnumerable<string> EnumerateValues(object value) {
        if (value == null) {
            yield break;
        }

        if (value is string text) {
            yield return text;
        } else if (value is ScimMember member) {
            yield return member.Value;
        } else if (value is JsonElement json) {
            foreach (var jsonValue in EnumerateJson(json)) {
                yield return jsonValue;
            }
        } else if (value is IEnumerable items) {
            foreach (var item in items) {
                foreach (var itemValue in EnumerateValues(item)) {
                    yield return itemValue;
                }
            }
        } else {
            throw new ScimStoreException($"Cannot read member values from {value.GetType().Name}");
        }
    }

    private static IEnumerable<string> EnumerateJson(JsonElement json) {
        if (json.ValueKind == JsonValueKind.Array) {
            foreach (var item in json.EnumerateArray()) {
                foreach (var itemValue in EnumerateJson(item)) {
                    yield return itemValue;
                }
            }
        } else if (json.ValueKind == JsonValueKind.Object && json.TryGetProperty("value", out var value)) {
            yield return value.GetString();
        } else if (json.ValueKind == JsonValueKind.String) {
            yield return json.GetString();
        }
    }

    private static IReadOnlyList<T> Build<T>(IScimQueryBuilder<T> builder) {
        var results = builder.Build().ToList();

        if (builder.Errors.OrEmpty().Any()) {
            throw new ScimStoreInvalidQueryException("The filter could not be applied", builder.Errors);
        }

        return results;
    }

    private static ISet<Guid> GetMemberKeys(PatchCommand command) {
        var values = EnumerateValues(command.Value).ToList();

        foreach (var element in command.Path.PathElements.OfType<ValuePathExpression>()) {
            if (element.ValueFilter is AttributeComparisonFilterExpression comparison &&
                comparison.Literal is LiteralStringFilterExpression literal) {
                values.Add(literal.Value);
            }
        }

        return ParseKeys(values);
    }

    private static bool IsMembersPath(PathExpression path) {
        var element = path?.PathElements?.FirstOrDefault();

        return element != null &&
               element.PathElements.Length > 0 &&
               element.PathElements[0].EqualsInvariant("members");
    }

    private static ISet<Guid> ParseKeys(IEnumerable<ScimMember> members) {
        return members == null ? null : ParseKeys(members.Select(x => x.Value));
    }

    private static ISet<Guid> ParseKeys(IEnumerable<string> values) {
        var keys = new HashSet<Guid>();

        foreach (var value in values.OrEmpty().Where(x => x.HasValue())) {
            if (!Guid.TryParse(value, out var key)) {
                throw new ScimStoreException($"Member {value.Quote()} is not a user id");
            }

            keys.Add(key);
        }

        return keys;
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
