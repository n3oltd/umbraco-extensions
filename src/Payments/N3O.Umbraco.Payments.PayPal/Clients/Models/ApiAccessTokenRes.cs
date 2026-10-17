using Newtonsoft.Json;

namespace N3O.Umbraco.Payments.PayPal.Clients.Models;

public class ApiAccessTokenRes {
    [JsonProperty("access_token")]
    public string AccessToken { get; set; }

    [JsonProperty("expires_in")]
    public int ExpiresIn { get; set; }
}
