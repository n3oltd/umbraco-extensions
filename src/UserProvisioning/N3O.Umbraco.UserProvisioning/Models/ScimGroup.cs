using N3O.Umbraco.UserProvisioning.Json;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace N3O.Umbraco.UserProvisioning.Models;

public class ScimGroup : ScimResource {
    [JsonProperty("displayName")]
    public string DisplayName { get; set; }

    // NullValueHandling.Ignore applies on read as well, so without Include an explicit null reads as absent
    [JsonConverter(typeof(ScimMembersConverter))]
    [JsonProperty("members", NullValueHandling = NullValueHandling.Include)]
    public IReadOnlyList<ScimMember> Members { get; set; }
}
