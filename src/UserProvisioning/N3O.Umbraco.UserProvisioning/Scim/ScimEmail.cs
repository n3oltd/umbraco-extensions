using Newtonsoft.Json;

namespace N3O.Umbraco.UserProvisioning.Scim;

public class ScimEmail {
    [JsonProperty("primary")]
    public bool Primary { get; set; }

    [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
    public string Type { get; set; }

    [JsonProperty("value")]
    public string Value { get; set; }
}
