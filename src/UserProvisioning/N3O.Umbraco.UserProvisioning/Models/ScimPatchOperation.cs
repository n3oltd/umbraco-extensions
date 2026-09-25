using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace N3O.Umbraco.UserProvisioning.Models;

public class ScimPatchOperation {
    [JsonProperty("op")]
    public string Op { get; set; }

    [JsonProperty("path")]
    public string Path { get; set; }

    // ScimJson ignores nulls on read as well as on write, which would bind an explicit null as an absent value
    [JsonProperty("value", NullValueHandling = NullValueHandling.Include)]
    public JToken Value { get; set; }
}
