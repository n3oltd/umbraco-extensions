using N3O.Umbraco.UserProvisioning.Exceptions;
using N3O.Umbraco.UserProvisioning.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.IO;

namespace N3O.Umbraco.UserProvisioning.Json;

public static class ScimJson {
    // CamelCasePropertyNamesContractResolver rewrites explicitly named properties as well as the rest,
    // which turns the list response's mandatory Resources into resources
    public static readonly JsonSerializerSettings Settings = Build();

    private static JsonSerializerSettings Build() {
        var namingStrategy = new CamelCaseNamingStrategy();
        namingStrategy.OverrideSpecifiedNames = false;

        var resolver = new DefaultContractResolver();
        resolver.NamingStrategy = namingStrategy;

        var settings = new JsonSerializerSettings();
        settings.ContractResolver = resolver;
        // Otherwise a number is coerced into a string or a boolean rather than refused
        settings.Converters.Add(new ScimBooleanConverter());
        settings.Converters.Add(new ScimStringConverter());
        settings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
        // Otherwise a string that looks like a date is read into a patch value as a date, and loses its text
        settings.DateParseHandling = DateParseHandling.None;
        // Otherwise a member that leads with $ref is read as a reference to another object, and binds as null
        settings.MetadataPropertyHandling = MetadataPropertyHandling.Ignore;
        settings.NullValueHandling = NullValueHandling.Ignore;

        return settings;
    }

    public static T Read<T>(string body) {
        RefuseRepeatedNames(body);

        return JsonConvert.DeserializeObject<T>(body, Settings);
    }

    public static string Write(object value) {
        return JsonConvert.SerializeObject(value, Settings);
    }

    private static void RefuseRepeatedNames(string body) {
        using (var reader = new JsonTextReader(new StringReader(body))) {
            var names = new Stack<HashSet<string>>();

            reader.DateParseHandling = DateParseHandling.None;

            while (reader.Read()) {
                if (reader.TokenType == JsonToken.StartObject) {
                    names.Push(new HashSet<string>(ScimText.Comparer));
                } else if (reader.TokenType == JsonToken.EndObject) {
                    names.Pop();
                } else if (reader.TokenType == JsonToken.PropertyName && !names.Peek().Add((string) reader.Value)) {
                    throw ScimException.InvalidSyntax($"{reader.Path} is named more than once");
                }
            }
        }
    }
}
