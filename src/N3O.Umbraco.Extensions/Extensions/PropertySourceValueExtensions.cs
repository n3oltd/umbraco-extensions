using System.Text.Json;
using System.Text.Json.Nodes;

namespace N3O.Umbraco.Extensions;

public static class PropertySourceValueExtensions {
    // A property's source value is only a string when it came from the MessagePack nucache, which is what a
    // document's own properties use. Inside a block editor the value is deserialised by Umbraco's
    // JsonObjectConverter, which answers a JsonObject for an object, a JsonArray for an array of objects, and a
    // List<T> for an array of scalars or of arrays. Under NuCacheSerializerType=JSON every complex value arrives
    // as a JsonElement instead. So the type depends on where the value was read from, and testing for one of
    // those shapes silently yields nothing for the others.
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

        // Named explicitly rather than left to the object overload's runtime-type dispatch, so the shape being
        // serialized is the one the caller can see.
        return JsonSerializer.Serialize(sourceValue, sourceValue.GetType());
    }
}
