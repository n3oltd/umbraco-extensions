using Newtonsoft.Json;
using System.Collections.Generic;

namespace N3O.Umbraco.UserProvisioning.Models;

public class ScimGroup : ScimResource {
    [JsonProperty("displayName")]
    public string DisplayName { get; set; }

    [JsonProperty("members", NullValueHandling = NullValueHandling.Ignore)]
    public IReadOnlyList<ScimMember> Members { get; set; }
}
