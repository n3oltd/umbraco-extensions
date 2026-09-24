using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
        settings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
        // Otherwise a string that looks like a date is read into a patch value as a date, and loses its text
        settings.DateParseHandling = DateParseHandling.None;
        // Otherwise a member that leads with $ref is read as a reference to another object, and binds as null
        settings.MetadataPropertyHandling = MetadataPropertyHandling.Ignore;
        settings.NullValueHandling = NullValueHandling.Ignore;

        return settings;
    }

    public static T Read<T>(string body) {
        return JsonConvert.DeserializeObject<T>(body, Settings);
    }

    public static string Write(object value) {
        return JsonConvert.SerializeObject(value, Settings);
    }
}
