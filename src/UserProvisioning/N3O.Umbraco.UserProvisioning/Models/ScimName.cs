using Newtonsoft.Json;

namespace N3O.Umbraco.UserProvisioning.Models;

public class ScimName {
    [JsonProperty("familyName", NullValueHandling = NullValueHandling.Ignore)]
    public string FamilyName { get; set; }

    [JsonProperty("formatted", NullValueHandling = NullValueHandling.Ignore)]
    public string Formatted { get; set; }

    [JsonProperty("givenName", NullValueHandling = NullValueHandling.Ignore)]
    public string GivenName { get; set; }
}
