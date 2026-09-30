using System.Text.Json;
using System.Text.Json.Nodes;

namespace N3O.Umbraco.Extensions;

public static class PropertySourceValueExtensions {
    // A stored value reaches a converter as a string, including a block value stored escaped. A block value stored
    // raw arrives as Umbraco's JsonObjectConverter parsed it: a JsonObject or JsonArray, or a List<T> or
    // List<object> for an array of scalars or mixed items. ContentHelper passes Dictionary<string, object> and
    // List<object>.
    public static string ToSourceValueJson(this object sourceValue) {
        if (sourceValue == null) {
            return null;
        } else if (sourceValue is string str) {
            return str;
        } else if (sourceValue is JsonNode jsonNode) {
            return jsonNode.ToJsonString();
        }

        return JsonSerializer.Serialize(sourceValue);
    }
}
