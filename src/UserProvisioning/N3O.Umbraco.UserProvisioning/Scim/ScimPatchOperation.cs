using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace N3O.Umbraco.UserProvisioning.Scim;

public class ScimPatchOperation {
    [JsonProperty("op")]
    public string Op { get; set; }

    [JsonProperty("path")]
    public string Path { get; set; }

    [JsonProperty("value")]
    public JToken Value { get; set; }
}
