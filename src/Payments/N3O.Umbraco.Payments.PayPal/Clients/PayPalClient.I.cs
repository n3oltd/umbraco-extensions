using N3O.Umbraco.Payments.PayPal.Clients.Models;
using Refit;
using System.Threading.Tasks;

namespace N3O.Umbraco.Payments.PayPal.Clients;

public interface IPayPalClient {
    [Post("/v2/payments/authorizations/{authorizationId}/capture")]
    Task<ApiAuthorizePaymentRes> AuthorizePaymentAsync(string authorizationId, [Body] ApiAuthorizePaymentReq req);
}
