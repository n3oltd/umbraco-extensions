using Refit;

namespace N3O.Umbraco.Payments.PayPal.Clients.Models;

public class ApiAccessTokenReq {
    [AliasAs("grant_type")]
    public string GrantType { get; set; }
}
