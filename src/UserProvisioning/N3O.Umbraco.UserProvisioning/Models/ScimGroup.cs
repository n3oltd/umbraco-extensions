using Newtonsoft.Json;
using System.Collections.Generic;

namespace N3O.Umbraco.UserProvisioning.Models;

public class ScimGroup : ScimResource {
    [JsonProperty("displayName")]
    public string DisplayName { get; set; }

    // NullValueHandling.Ignore applies on read as well, so without DisallowNull an explicit null reads as absent
    [JsonProperty("members", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public IReadOnlyList<ScimMember> Members { get; set; }
}
