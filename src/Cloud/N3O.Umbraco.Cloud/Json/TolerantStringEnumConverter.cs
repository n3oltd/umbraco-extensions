using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace N3O.Umbraco.Cloud.Json;

public class TolerantStringEnumConverter : StringEnumConverter {
    public override object ReadJson(JsonReader reader,
                                    Type objectType,
                                    object existingValue,
                                    Newtonsoft.Json.JsonSerializer serializer) {
        if (reader.TokenType != JsonToken.String) {
            return base.ReadJson(reader, objectType, existingValue, serializer);
        }

        try {
            return base.ReadJson(reader, objectType, existingValue, serializer);
        } catch (JsonSerializationException) when (Nullable.GetUnderlyingType(objectType) != null) {
            return null;
        }
    }
}
