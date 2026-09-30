using N3O.Umbraco.UserProvisioning.Exceptions;
using N3O.Umbraco.UserProvisioning.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.IO;

namespace N3O.Umbraco.UserProvisioning.Json;

public static class ScimJson {
    public static readonly JsonSerializerSettings Settings = Build();

    private static JsonSerializerSettings Build() {
        var namingStrategy = new CamelCaseNamingStrategy();
        namingStrategy.OverrideSpecifiedNames = false;

        var resolver = new DefaultContractResolver();
        resolver.NamingStrategy = namingStrategy;

        var settings = new JsonSerializerSettings();
        settings.ContractResolver = resolver;
        settings.Converters.Add(new ScimBooleanConverter());
        settings.Converters.Add(new ScimStringConverter());
        settings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
        settings.DateParseHandling = DateParseHandling.None;
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
