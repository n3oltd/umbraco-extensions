using N3O.Umbraco.UserProvisioning.Exceptions;
using Newtonsoft.Json.Linq;

namespace N3O.Umbraco.UserProvisioning.Extensions;

public static class ScimValues {
    public static bool IsNull(this JToken value) {
        return value == null || value.Type == JTokenType.Null;
    }

    public static bool? ReadBoolean(this JToken value, string attribute) {
        if (value.IsNull()) {
            return null;
        }

        if (value.Type == JTokenType.Boolean) {
            return value.Value<bool>();
        }

        // Without this a boolean sent as a string is refused, and the provisioning service sends one by default
        if (value.Type == JTokenType.String && bool.TryParse(value.Value<string>(), out var flag)) {
            return flag;
        }

        throw ScimException.InvalidValue($"{attribute} must be true or false");
    }

    public static string ReadString(this JObject json, string name, string attribute) {
        return json.GetValue(name, ScimText.Comparison).ReadString(attribute);
    }

    public static string ReadString(this JToken value, string attribute) {
        if (value.IsNull()) {
            return null;
        }

        if (value.Type == JTokenType.String) {
            return value.Value<string>();
        }

        throw ScimException.InvalidValue($"{attribute} must be a string");
    }
}
