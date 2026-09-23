using Newtonsoft.Json;
using System.Collections.Generic;

namespace N3O.Umbraco.UserProvisioning.Models;

public abstract class ScimResource {
    [JsonProperty("externalId", NullValueHandling = NullValueHandling.Ignore)]
    public string ExternalId { get; set; }

    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string Id { get; set; }

    [JsonProperty("meta", NullValueHandling = NullValueHandling.Ignore)]
    public ScimMeta Meta { get; set; }

    [JsonProperty("schemas")]
    public IEnumerable<string> Schemas { get; set; }
}
