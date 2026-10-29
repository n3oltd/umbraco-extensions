using Refit;
using System.Threading.Tasks;

namespace N3O.Umbraco.Payments.Bambora.Clients;

public interface IBamboraPaymentsClient {
    [Post("/payments")]
    Task<ApiPaymentRes> CreatePaymentAsync(ApiPaymentReq req);
    
    [Post("/payments/{threeDSessionData}/continue")]
    Task<ApiPaymentRes> CompleteThreeDSecureAsync(string threeDSessionData, [Body] ThreeDSecureChallenge req);
}
