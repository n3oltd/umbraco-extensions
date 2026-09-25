using N3O.Umbraco.UserProvisioning.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace N3O.Umbraco.UserProvisioning.Json;

public class ScimStringConverter : JsonConverter<string> {
    public override bool CanWrite => false;

    public override string ReadJson(JsonReader reader,
                                    Type objectType,
                                    string existingValue,
                                    bool hasExistingValue,
                                    JsonSerializer serializer) {
        var path = reader.Path;

        return JToken.Load(reader).ReadString(path);
    }

    public override void WriteJson(JsonWriter writer, string value, JsonSerializer serializer) {
        throw new NotSupportedException();
    }
}
