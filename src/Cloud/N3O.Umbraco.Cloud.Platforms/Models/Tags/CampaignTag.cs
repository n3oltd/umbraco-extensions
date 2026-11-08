using Newtonsoft.Json;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class CampaignTag {
    [JsonProperty("key")]
    public string Key { get; set; }

    [JsonProperty("value")]
    public string Value { get; set; }
}
