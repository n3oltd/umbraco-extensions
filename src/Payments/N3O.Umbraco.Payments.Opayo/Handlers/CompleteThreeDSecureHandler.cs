using N3O.Umbraco.Payments.Handlers;
using N3O.Umbraco.Payments.Models;
using N3O.Umbraco.Payments.Opayo.Clients;
using N3O.Umbraco.Payments.Opayo.Commands;
using N3O.Umbraco.Payments.Opayo.Models;
using Refit;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Payments.Opayo.Handlers;

public class CompleteThreeDSecureHandler :
    PaymentsHandler<CompleteThreeDSecureCommand, CompleteThreeDSecureReq, OpayoPayment> {
    private readonly IOpayoHelper _opayoHelper;
    private readonly IOpayoClient _opayoClient;

    public CompleteThreeDSecureHandler(IPaymentsScope paymentsScope,
                                       IOpayoHelper opayoHelper,
                                       IOpayoClient opayoClient)
        : base(paymentsScope) {
        _opayoHelper = opayoHelper;
        _opayoClient = opayoClient;
    }

    protected override async Task HandleAsync(CompleteThreeDSecureCommand req,
                                              OpayoPayment payment,
                                              PaymentsParameters parameters,
                                              CancellationToken cancellationToken) {
        try {
            ApiTransactionRes transaction;
            
            if (req.Model.Version() == 1) {
                transaction = await CompleteV1Async(req.Model, payment);
            } else {
                transaction = await CompleteV2Async(req.Model, payment);
            }

            _opayoHelper.ApplyAuthorisation(payment, transaction);
        } catch (ApiException apiException) {
            _opayoHelper.ApplyException(payment, apiException);
        }
    }
    
    private async Task<ApiTransactionRes> CompleteV1Async(CompleteThreeDSecureReq req, OpayoPayment payment) {
        var apiReq = new ApiThreeDSecureFallbackResponse();
        apiReq.PaRes = req.PaRes;
        apiReq.TransactionId = payment.OpayoTransactionId;

        await _opayoClient.CompleteThreeDSecureFallbackResponseAsync(apiReq.TransactionId, apiReq);

        payment.ThreeDSecureComplete(req.PaRes);

        var transaction = await _opayoClient.GetTransactionByIdAsync(payment.OpayoTransactionId);

        return transaction;
    }

    private async Task<ApiTransactionRes> CompleteV2Async(CompleteThreeDSecureReq req,
                                                          OpayoPayment payment) {
        var apiReq = new ApiThreeDSecureChallengeResponse();
        apiReq.CRes = req.CRes;
        apiReq.TransactionId = payment.OpayoTransactionId;

        var transaction = await _opayoClient.CompleteThreeDSecureChallengeResponseAsync(apiReq.TransactionId, apiReq);

        payment.ThreeDSecureComplete(req.CRes);

        return transaction;
    }
}
