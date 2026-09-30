using System.Text.Json;
using System.Text.Json.Nodes;

namespace N3O.Umbraco.Extensions;

public static class PropertySourceValueExtensions {
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
