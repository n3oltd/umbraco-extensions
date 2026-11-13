using N3O.Umbraco.Payments.PayPal.Clients.Models;
using Refit;
using System.Threading.Tasks;

namespace N3O.Umbraco.Payments.PayPal.Clients;

public interface IOAuthTokenClient {
    [Headers("Content-Type: application/x-www-form-urlencoded")]
    [Post("/v1/oauth2/token")]
    Task<ApiAccessTokenRes> GetAccessTokenAsync([Body(BodySerializationMethod.UrlEncoded)] ApiAccessTokenReq req);
}
