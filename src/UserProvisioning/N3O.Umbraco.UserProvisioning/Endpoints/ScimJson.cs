using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace N3O.Umbraco.UserProvisioning.Endpoints;

public static class ScimJson {
    // CamelCasePropertyNamesContractResolver rewrites explicitly named properties as well as the rest,
    // which turns the list response's mandatory Resources into resources
    public static readonly JsonSerializerSettings Settings = Build();

    private static JsonSerializerSettings Build() {
        var resolver = new DefaultContractResolver();
        resolver.NamingStrategy = new CamelCaseNamingStrategy { OverrideSpecifiedNames = false };

        var settings = new JsonSerializerSettings();
        settings.ContractResolver = resolver;
        settings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
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
