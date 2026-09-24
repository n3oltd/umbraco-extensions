using Newtonsoft.Json;
using System.Collections.Generic;

namespace N3O.Umbraco.UserProvisioning.Models;

public class ScimError {
    [JsonProperty("detail")]
    public string Detail { get; set; }

    [JsonProperty("schemas")]
    public IEnumerable<string> Schemas { get; set; } = [ScimConstants.Schemas.Error];

    [JsonProperty("scimType", NullValueHandling = NullValueHandling.Ignore)]
    public string ScimType { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }
}
