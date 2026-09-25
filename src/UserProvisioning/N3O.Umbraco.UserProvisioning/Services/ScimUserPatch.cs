using N3O.Umbraco.Extensions;
using N3O.Umbraco.UserProvisioning.Exceptions;
using N3O.Umbraco.UserProvisioning.Extensions;
using N3O.Umbraco.UserProvisioning.Filters;
using N3O.Umbraco.UserProvisioning.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.UserProvisioning.Services;

public static class ScimUserPatch {
    public static void Apply(ScimUser user, ScimPatchOperation operation) {
        var op = operation.Op ?? "";
        var path = ScimPath.Parse(operation.Path);

        if (path == null) {
            foreach (var property in AsObject(operation.Value).Properties()) {
                var attribute = ScimPath.Parse(property.Name);

                if (attribute == null) {
                    throw ScimException.InvalidPath("An attribute in a patch value has no name");
                }

                Set(user, attribute, property.Value, op);
            }
        } else {
            Set(user, path, operation.Value, op);
        }
    }

    private static JObject AsObject(JToken value) {
        if (value is JObject json) {
            return json;
        }

        throw ScimException.InvalidValue("A patch without a path carries a resource");
    }

    private static IEnumerable<JObject> AsObjects(ScimPath path, JToken value) {
        if (value is JArray array) {
            if (array.Any(x => x is not JObject)) {
                throw ScimException.InvalidValue($"Each value of {path} must be an object");
            }

            return array.Cast<JObject>();
        }

        return value is JObject json ? [json] : [];
    }

    private static ScimEmail Email(string address, bool primary) {
        var email = new ScimEmail();
        email.Primary = primary;
        email.Value = address;

        return email;
    }

    private static ScimEmail ReadEmail(ScimPath path, JObject json) {
        return Email(json.ReadString("value", $"{path}.value"), json.ReadBoolean("primary", $"{path}.primary") == true);
    }

    private static ScimName ReadName(ScimPath path, JToken value) {
        if (value.IsNull()) {
            return null;
        }

        if (value is not JObject json) {
            throw ScimException.InvalidValue($"{path} must be an object");
        }

        var name = new ScimName();
        name.FamilyName = json.ReadString("familyName", $"{path}.familyName");
        name.Formatted = json.ReadString("formatted", $"{path}.formatted");
        name.GivenName = json.ReadString("givenName", $"{path}.givenName");

        return name;
    }

    private static void Set(ScimUser user, ScimPath path, JToken value, string op) {
        var removing = op.Is("remove");

        if (path.Is("active")) {
            user.Active = removing ? null : value.ReadBoolean(path.ToString());
        } else if (path.Is("userName")) {
            user.UserName = removing ? null : value.ReadString(path.ToString());
        } else if (path.Is("displayName")) {
            user.DisplayName = removing ? null : value.ReadString(path.ToString());
        } else if (path.Is("externalId")) {
            user.ExternalId = removing ? null : value.ReadString(path.ToString());
        } else if (path.Is("name")) {
            SetName(user, path, value, removing);
        } else if (path.Is("emails")) {
            SetEmails(user, path, value, removing);
        } else {
            throw ScimException.InvalidPath($"{path} is not an attribute this endpoint stores");
        }
    }

    // Umbraco holds one address, so a patch of any email is a patch of that one, whichever member of
    // the collection the path selects
    private static void SetEmails(ScimUser user, ScimPath path, JToken value, bool removing) {
        var element = path.Attribute.Elements.Length > 1 ? path.Attribute.Elements[1] : path.SubAttribute;

        if (element.HasValue() && !element.Is("value")) {
            throw ScimException.InvalidPath($"{path} is not an attribute this endpoint stores");
        }

        if (removing) {
            user.Emails = [];

            return;
        }

        var emails = element.HasValue()
                         ? [Email(value.ReadString(path.ToString()), true)]
                         : AsObjects(path, value).Select(x => ReadEmail(path, x)).ToList();

        if (!emails.Any(x => x.Value.HasValue())) {
            throw ScimException.InvalidValue($"{path} carries no email address");
        }

        user.Emails = emails;
    }

    private static void SetName(ScimUser user, ScimPath path, JToken value, bool removing) {
        user.Name ??= new ScimName();

        var element = path.Attribute.Elements.Length > 1 ? path.Attribute.Elements[1] : path.SubAttribute;

        if (!element.HasValue()) {
            var replacement = removing ? null : ReadName(path, value);

            user.Name = replacement ?? new ScimName();

            return;
        }

        var text = removing ? null : value.ReadString(path.ToString());

        if (element.Is("familyName")) {
            user.Name.FamilyName = text;
        } else if (element.Is("formatted")) {
            user.Name.Formatted = text;
        } else if (element.Is("givenName")) {
            user.Name.GivenName = text;
        } else {
            throw ScimException.InvalidPath($"{path} is not an attribute this endpoint stores");
        }
    }

}
