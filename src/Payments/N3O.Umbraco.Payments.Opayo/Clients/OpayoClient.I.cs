using Refit;
using System.Threading.Tasks;

namespace N3O.Umbraco.Payments.Opayo.Clients;

public interface IOpayoClient {
    [Post("/api/v1/transactions/{transactionId}/3d-secure-challenge/")]
    Task<ApiTransactionRes> CompleteThreeDSecureChallengeResponseAsync(string transactionId,
                                                                       [Body] ApiThreeDSecureChallengeResponse req);
    
    [Post("/api/v1/transactions/{transactionId}/3d-secure/")]
    Task<ApiThreeDSecure> CompleteThreeDSecureFallbackResponseAsync(string transactionId,
                                                                    [Body] ApiThreeDSecureFallbackResponse req);
    
    [Post("/api/v1/merchant-session-keys")]
    Task<ApiMerchantSessionKeyRes> GetMerchantSessionKeyAsync(ApiMerchantSessionKeyReq req);
    
    [Get("/api/v1/transactions/{transactionId}")]
    Task<ApiTransactionRes> GetTransactionByIdAsync(string transactionId);

    [Post("/api/v1/transactions")]
    Task<ApiTransactionRes> TransactionAsync(ApiPaymentTransactionReq req);
    
    [Post("/api/v1/applepay/sessions")]
    Task<ApiApplePaySessionRes> GetApplePaySessionAsync(ApiApplePaySessionReq req);
}
