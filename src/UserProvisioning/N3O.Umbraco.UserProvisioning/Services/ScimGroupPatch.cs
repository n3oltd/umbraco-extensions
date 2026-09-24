using N3O.Umbraco.Extensions;
using N3O.Umbraco.UserProvisioning.Exceptions;
using N3O.Umbraco.UserProvisioning.Extensions;
using N3O.Umbraco.UserProvisioning.Filters;
using N3O.Umbraco.UserProvisioning.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.UserProvisioning.Services;

public static class ScimGroupPatch {

    public static bool IsMembership(ScimPatchOperation operation) {
        var path = ScimPath.Parse(operation.Path);

        if (path != null) {
            return path.Is("members");
        }

        return operation.Value is JObject json && json.Property("members") != null;
    }

    // An identity provider re-sends its own key every cycle until a read gives it back
    public static string ReadExternalId(IEnumerable<ScimPatchOperation> operations) {
        string externalId = null;

        foreach (var operation in operations.OrEmpty()) {
            var op = operation.Op ?? "";

            if (!op.Is("add") && !op.Is("replace")) {
                continue;
            }

            var path = ScimPath.Parse(operation.Path);

            if (path != null && path.Is("externalId")) {
                externalId = operation.Value?.Value<string>();
            } else if (path == null && operation.Value is JObject json) {
                externalId = json.Value<string>("externalId") ?? externalId;
            }
        }

        return externalId;
    }

    // Not the members that change: naming one the group already holds is a claim to record all the same
    public static ISet<Guid> Named(IReadOnlyList<BackOfficeUser> held, IEnumerable<ScimPatchOperation> operations) {
        var named = new HashSet<Guid>();

        foreach (var operation in operations.OrEmpty().Where(IsMembership)) {
            if ((operation.Op ?? "").Is("add") || (operation.Op ?? "").Is("replace")) {
                named.UnionWith(ReadKeys(held, operation));
            }
        }

        return named;
    }

    public static ISet<Guid> ParseKeys(IEnumerable<ScimMember> members) {
        return members == null ? null : ParseKeys(members.Select(x => Identify(x.Value, x.Reference)));
    }

    public static ISet<Guid> Resolve(IReadOnlyList<BackOfficeUser> held, IEnumerable<ScimPatchOperation> operations) {
        var members = held.Select(x => Guid.Parse(x.Id)).ToHashSet();

        foreach (var operation in operations.OrEmpty().Where(IsMembership)) {
            var op = operation.Op ?? "";

            if (!op.Is("add") && !op.Is("remove") && !op.Is("replace")) {
                throw ScimException.InvalidValue($"{operation.Op.Quote()} is not a patch operation");
            }

            if (NamesSubAttribute(operation)) {
                throw ScimException.InvalidPath("A member cannot be patched one sub-attribute at a time");
            }

            // Without this a replace deletes the members its path selects and puts nothing back
            if (!op.Is("remove") && Absent(operation.Value)) {
                throw ScimException.InvalidValue($"A {op.ToLowerInvariant()} of members requires a value");
            }

            var keys = ReadKeys(held, operation);

            if (op.Is("add")) {
                members.UnionWith(keys);
            } else if (op.Is("replace") && Selects(operation)) {
                // A filter selects the records to replace, so the rest of the membership survives
                if (!keys.Any()) {
                    throw ScimException.NoTarget("No member of this group matches the path");
                }

                members.ExceptWith(keys);
                members.UnionWith(ParseKeys(ReadValues(operation.Value)));
            } else if (op.Is("replace")) {
                members.Clear();
                members.UnionWith(keys);
            } else if (NamesNobody(operation)) {
                members.Clear();
            } else if (keys.Any() || Selects(operation)) {
                // The same removal arrives more than once, so refusing one that matches nobody fails
                // every cycle that removes anyone
                members.ExceptWith(keys);
            } else {
                throw ScimException.InvalidValue("The members to remove could not be read from the patch");
            }
        }

        return members;
    }

    // Newtonsoft binds an explicit JSON null to a token rather than to a C# null
    private static bool Absent(JToken value) {
        return value == null || value.Type == JTokenType.Null;
    }

    private static ScimAttributes Describe(BackOfficeUser member) {
        return new ScimAttributes().Add("display", member.Name)
                                   .Add("type", "User")
                                   .Add("value", member.Id);
    }

    private static string Identify(string value, string reference) {
        return value.HasValue() ? value : reference?.Split('/').LastOrDefault();
    }

    private static bool Selects(ScimPatchOperation operation) {
        return ScimPath.Parse(operation.Path)?.ValueFilter != null;
    }

    private static bool NamesSubAttribute(ScimPatchOperation operation) {
        var path = ScimPath.Parse(operation.Path);

        return path != null && (path.Attribute.Elements.Length > 1 || path.SubAttribute.HasValue());
    }

    private static bool NamesNobody(ScimPatchOperation operation) {
        var path = ScimPath.Parse(operation.Path);

        // An absent value names the whole attribute; an explicit null does not, and is refused
        return operation.Value == null && path != null && path.ValueFilter == null;
    }

    private static ISet<Guid> ParseKeys(IEnumerable<string> values) {
        var keys = new HashSet<Guid>();

        foreach (var value in values.OrEmpty().Where(x => x.HasValue())) {
            if (!Guid.TryParse(value, out var key)) {
                throw ScimException.InvalidValue($"Member {value.Quote()} is not a user ID");
            }

            keys.Add(key);
        }

        return keys;
    }

    // A path filter selects among the members already held, so any shape of reference resolves without
    // reading the literal out of the expression
    private static ISet<Guid> ReadKeys(IReadOnlyList<BackOfficeUser> held, ScimPatchOperation operation) {
        var path = ScimPath.Parse(operation.Path);

        if (path?.ValueFilter != null) {
            var selected = held.Where(x => path.ValueFilter.Matches(Describe(x))).Select(x => Guid.Parse(x.Id));

            return new HashSet<Guid>(selected);
        }

        return ParseKeys(ReadValues(operation.Value));
    }

    private static IEnumerable<string> ReadValues(JToken value) {
        if (value == null) {
            yield break;
        }

        if (value is JArray array) {
            foreach (var item in array) {
                foreach (var found in ReadValues(item)) {
                    yield return found;
                }
            }
        } else if (value is JObject json) {
            if (json.Property("members") != null) {
                foreach (var found in ReadValues(json["members"])) {
                    yield return found;
                }
            } else {
                yield return Identify(json.Value<string>("value"), json.Value<string>("$ref"));
            }
        } else {
            yield return value.Value<string>();
        }
    }

}
