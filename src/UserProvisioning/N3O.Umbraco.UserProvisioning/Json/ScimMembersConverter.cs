using N3O.Umbraco.UserProvisioning.Exceptions;
using N3O.Umbraco.UserProvisioning.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace N3O.Umbraco.UserProvisioning.Json;

public class ScimMembersConverter : JsonConverter<IReadOnlyList<ScimMember>> {
    public override bool CanWrite => false;

    public override IReadOnlyList<ScimMember> ReadJson(JsonReader reader,
                                                       Type objectType,
                                                       IReadOnlyList<ScimMember> existingValue,
                                                       bool hasExistingValue,
                                                       JsonSerializer serializer) {
        if (reader.TokenType == JsonToken.Null) {
            throw ScimException.NullMembers();
        }

        return serializer.Deserialize<List<ScimMember>>(reader);
    }

    public override void WriteJson(JsonWriter writer, IReadOnlyList<ScimMember> value, JsonSerializer serializer) {
        throw new NotSupportedException();
    }
}
