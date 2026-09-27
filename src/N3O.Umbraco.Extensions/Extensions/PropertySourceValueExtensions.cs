using System.Text.Json;
using System.Text.Json.Nodes;

namespace N3O.Umbraco.Extensions;

public static class PropertySourceValueExtensions {
    // A property's source value is only a string when it came from the MessagePack nucache, which is what a
    // document's own properties use. Inside a block editor the value is deserialised by Umbraco's
    // JsonObjectConverter, which answers a JsonObject for an object, a JsonArray for an array of objects, and a
    // List<T> for an array of scalars or of arrays. A programmatic write through PropertyBuilder stores the
    // unserialised object, and the JSON nucache answers JsonElement for a complex value.
    public static string ToSourceValueJson(this object sourceValue) {
        if (sourceValue == null) {
            return null;
        } else if (sourceValue is string str) {
            return str;
        } else if (sourceValue is JsonNode jsonNode) {
            return jsonNode.ToJsonString();
        } else if (sourceValue is JsonElement jsonElement) {
            return jsonElement.GetRawText();
        }

        return JsonSerializer.Serialize(sourceValue, sourceValue.GetType());
    }
}
