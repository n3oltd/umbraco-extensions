using N3O.Umbraco.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Reflection;

namespace N3O.Umbraco.Cloud.Json;

// Published content is written by the cloud, which may already use enum values this site's generated client lacks.
public class PublishedContentContractResolver : JsonContractResolver {
    private static readonly TolerantStringEnumConverter TolerantStringEnumConverter = new();

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization) {
        var jProperty = base.CreateProperty(member, memberSerialization);

        if (jProperty.Converter is StringEnumConverter && IsNullableEnum(jProperty.PropertyType)) {
            jProperty.Converter = TolerantStringEnumConverter;
        }

        return jProperty;
    }

    private static bool IsNullableEnum(Type type) {
        return Nullable.GetUnderlyingType(type)?.IsEnum == true;
    }
}
