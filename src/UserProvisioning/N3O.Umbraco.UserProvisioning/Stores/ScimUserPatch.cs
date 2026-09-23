using N3O.Umbraco.Extensions;
using N3O.Umbraco.UserProvisioning.Filters;
using N3O.Umbraco.UserProvisioning.Scim;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.UserProvisioning.Stores;

public static class ScimUserPatch {
    public static void Apply(ScimUser user, ScimPatchOperation operation) {
        var op = operation.Op ?? "";

        if (!op.Is("add") && !op.Is("replace") && !op.Is("remove")) {
            throw ScimException.InvalidValue($"{operation.Op.Quote()} is not a patch operation");
        }

        var path = ScimPath.Parse(operation.Path);

        // A patch with no path carries a whole resource, each of whose attributes is one assignment
        if (path == null) {
            foreach (var property in AsObject(operation.Value).Properties()) {
                Set(user, ScimPath.Parse(property.Name), property.Value, op);
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

    private static IEnumerable<JObject> AsObjects(JToken value) {
        if (value is JArray array) {
            return array.OfType<JObject>();
        }

        return value is JObject json ? [json] : [];
    }

    private static void Set(ScimUser user, ScimPath path, JToken value, string op) {
        var removing = op.Is("remove");

        if (path.Is("active")) {
            user.Active = removing ? null : value?.Value<bool?>();
        } else if (path.Is("userName")) {
            user.UserName = removing ? null : value?.Value<string>();
        } else if (path.Is("displayName")) {
            user.DisplayName = removing ? null : value?.Value<string>();
        } else if (path.Is("externalId")) {
            user.ExternalId = removing ? null : value?.Value<string>();
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
        if (removing) {
            user.Emails = [];

            return;
        }

        var address = path.SubAttribute.HasValue() || path.ValueFilter != null
                          ? value?.Value<string>()
                          : AsObjects(value).Select(x => x.Value<string>("value")).FirstOrDefault(x => x.HasValue());

        if (!address.HasValue()) {
            throw ScimException.InvalidValue($"{path} carries no email address");
        }

        var email = new ScimEmail();
        email.Primary = true;
        email.Type = "work";
        email.Value = address;

        user.Emails = [email];
    }

    private static void SetName(ScimUser user, ScimPath path, JToken value, bool removing) {
        user.Name ??= new ScimName();

        var element = path.Attribute.Elements.Length > 1 ? path.Attribute.Elements[1] : path.SubAttribute;

        if (!element.HasValue()) {
            var replacement = removing ? null : value?.ToObject<ScimName>();

            user.Name = replacement ?? new ScimName();

            return;
        }

        var text = removing ? null : value?.Value<string>();

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
