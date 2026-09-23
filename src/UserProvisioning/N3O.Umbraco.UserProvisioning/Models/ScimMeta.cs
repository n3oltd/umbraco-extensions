using Newtonsoft.Json;
using System;

namespace N3O.Umbraco.UserProvisioning.Models;

public class ScimMeta {
    [JsonProperty("created", NullValueHandling = NullValueHandling.Ignore)]
    public DateTime? Created { get; set; }

    [JsonProperty("lastModified", NullValueHandling = NullValueHandling.Ignore)]
    public DateTime? LastModified { get; set; }

    [JsonProperty("location", NullValueHandling = NullValueHandling.Ignore)]
    public string Location { get; set; }

    [JsonProperty("resourceType")]
    public string ResourceType { get; set; }
}
