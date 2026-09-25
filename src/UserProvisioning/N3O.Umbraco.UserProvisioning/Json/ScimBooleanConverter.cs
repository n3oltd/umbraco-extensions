using N3O.Umbraco.UserProvisioning.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace N3O.Umbraco.UserProvisioning.Json;

public class ScimBooleanConverter : JsonConverter {
    public override bool CanWrite => false;

    public override bool CanConvert(Type objectType) {
        return objectType == typeof(bool) || objectType == typeof(bool?);
    }

    public override object ReadJson(JsonReader reader,
                                    Type objectType,
                                    object existingValue,
                                    JsonSerializer serializer) {
        var path = reader.Path;

        return JToken.Load(reader).ReadBoolean(path);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
        throw new NotSupportedException();
    }
}
