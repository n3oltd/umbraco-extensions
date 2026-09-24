using N3O.Umbraco.UserProvisioning.Exceptions;
using Newtonsoft.Json.Linq;

namespace N3O.Umbraco.UserProvisioning.Extensions;

public static class ScimValues {
    public static bool? ReadBoolean(this JToken value, string attribute) {
        if (IsNull(value)) {
            return null;
        }

        if (value.Type == JTokenType.Boolean) {
            return value.Value<bool>();
        }

        // Unless it is told otherwise, the provisioning service sends a boolean as the string "False"
        if (value.Type == JTokenType.String && bool.TryParse(value.Value<string>(), out var flag)) {
            return flag;
        }

        throw ScimException.InvalidValue($"{attribute} must be true or false");
    }

    public static string ReadString(this JToken value, string attribute) {
        if (IsNull(value)) {
            return null;
        }

        if (value.Type == JTokenType.String) {
            return value.Value<string>();
        }

        throw ScimException.InvalidValue($"{attribute} must be a string");
    }

    private static bool IsNull(JToken value) {
        return value == null || value.Type == JTokenType.Null;
    }
}
