using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;

namespace N3O.Umbraco.Exceptions;

public class ProblemDetails {
    public ProblemDetails(HttpStatusCode status, string title, string detail) {
        Status = (int) status;
        Title = title;
        Detail = detail;
    }

    public string Type { get; set; }
    public string Title { get; }
    public int Status { get; }
    public string Detail { get; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string Instance { get; set; }

    [JsonExtensionData]
    public IDictionary<string, object> Extensions { get; } = new J2N.Collections.Generic.Dictionary<string, object>();
}
