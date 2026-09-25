using Newtonsoft.Json;
using System.Collections.Generic;

namespace N3O.Umbraco.UserProvisioning.Models;

public class ScimGroup : ScimResource {
    [JsonProperty("displayName")]
    public string DisplayName { get; set; }

    // Absent leaves the members alone, so an explicit null is refused rather than read as the same
    [JsonProperty("members", NullValueHandling = NullValueHandling.Ignore, Required = Required.DisallowNull)]
    public IReadOnlyList<ScimMember> Members { get; set; }
}
