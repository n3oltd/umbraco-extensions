using Newtonsoft.Json;

namespace N3O.Umbraco.UserProvisioning.Models;

public class ScimMember {
    [JsonProperty("display", NullValueHandling = NullValueHandling.Ignore)]
    public string Display { get; set; }

    [JsonProperty("$ref", NullValueHandling = NullValueHandling.Ignore)]
    public string Reference { get; set; }

    [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
    public string Type { get; set; }

    [JsonProperty("value")]
    public string Value { get; set; }
}
