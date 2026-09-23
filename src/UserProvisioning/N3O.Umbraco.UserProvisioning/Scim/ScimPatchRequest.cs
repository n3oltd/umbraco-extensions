using Newtonsoft.Json;
using System.Collections.Generic;

namespace N3O.Umbraco.UserProvisioning.Scim;

public class ScimPatchRequest {
    [JsonProperty("Operations")]
    public IReadOnlyList<ScimPatchOperation> Operations { get; set; }

    [JsonProperty("schemas")]
    public IEnumerable<string> Schemas { get; set; }
}
