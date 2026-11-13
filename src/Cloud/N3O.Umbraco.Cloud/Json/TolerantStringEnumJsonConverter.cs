using Microsoft.Extensions.Logging;
using N3O.Umbraco.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Concurrent;

namespace N3O.Umbraco.Cloud.Json;

[NoRegisterAll]
public class TolerantStringEnumJsonConverter : StringEnumConverter {
    private static readonly ConcurrentDictionary<(Type, string), bool> LoggedUnknownValues = new();

    private readonly ILogger _logger;

    public TolerantStringEnumJsonConverter(ILogger logger) {
        _logger = logger;
    }

    public override object ReadJson(JsonReader reader,
                                    Type objectType,
                                    object existingValue,
                                    JsonSerializer serializer) {
        if (reader.TokenType != JsonToken.String) {
            return base.ReadJson(reader, objectType, existingValue, serializer);
        }

        try {
            return base.ReadJson(reader, objectType, existingValue, serializer);
        } catch (JsonSerializationException) {
            var value = (string) reader.Value;

            if (LoggedUnknownValues.TryAdd((objectType, value), true)) {
                _logger.LogError("Reading unknown {EnumType} value {Value} at {Path} as null",
                                 Nullable.GetUnderlyingType(objectType).Name,
                                 value,
                                 reader.Path);
            }

            return null;
        }
    }
}
