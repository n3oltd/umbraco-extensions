using Newtonsoft.Json;
using System.Collections.Generic;

namespace N3O.Umbraco.UserProvisioning.Models;

public class ScimListResponse<T> {
    [JsonProperty("itemsPerPage")]
    public int ItemsPerPage { get; set; }

    [JsonProperty("Resources")]
    public IReadOnlyList<T> Resources { get; set; }

    [JsonProperty("schemas")]
    public IEnumerable<string> Schemas { get; set; } = [ScimConstants.Schemas.ListResponse];

    [JsonProperty("startIndex")]
    public int StartIndex { get; set; }

    [JsonProperty("totalResults")]
    public int TotalResults { get; set; }
}
