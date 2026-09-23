using Newtonsoft.Json;
using System.Collections.Generic;

namespace N3O.Umbraco.UserProvisioning.Models;

public class ScimUser : ScimResource {
    [JsonProperty("active")]
    public bool? Active { get; set; }

    [JsonProperty("displayName", NullValueHandling = NullValueHandling.Ignore)]
    public string DisplayName { get; set; }

    [JsonProperty("emails", NullValueHandling = NullValueHandling.Ignore)]
    public IEnumerable<ScimEmail> Emails { get; set; }

    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public ScimName Name { get; set; }

    [JsonProperty("userName")]
    public string UserName { get; set; }
}
