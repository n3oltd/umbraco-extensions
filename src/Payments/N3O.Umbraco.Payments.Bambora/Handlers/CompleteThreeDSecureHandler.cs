using N3O.Umbraco.Exceptions;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Payments.Bambora.Clients;
using N3O.Umbraco.Payments.Bambora.Commands;
using N3O.Umbraco.Payments.Bambora.Extensions;
using N3O.Umbraco.Payments.Bambora.Models;
using N3O.Umbraco.Payments.Handlers;
using N3O.Umbraco.Payments.Models;
using Newtonsoft.Json;
using Refit;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Payments.Bambora.Handlers;

public class CompleteThreeDSecureHandler :
    PaymentsHandler<CompleteThreeDSecureCommand, CompleteThreeDSecureReq, BamboraPayment> {
    private readonly IBamboraPaymentsClient _paymentsClient;

    public CompleteThreeDSecureHandler(IPaymentsScope paymentsScope, IBamboraPaymentsClient paymentsClient)
        : base(paymentsScope) {
        _paymentsClient = paymentsClient;
    }

    protected override async Task HandleAsync(CompleteThreeDSecureCommand req,
                                              BamboraPayment payment,
                                              PaymentsParameters parameters,
                                              CancellationToken cancellationToken) {
        try {
            var apiReq = new ThreeDSecureChallenge();
            apiReq.CardResponse = new ThreeDSecureCardResponse();
            apiReq.PaymentMethod = "token";
            apiReq.ThreeDSessionData = payment.Card.ThreeDSecureV2.SessionData;
            apiReq.CardResponse.Cres = req.Model.CRes;

            var apiPayment = await _paymentsClient.CompleteThreeDSecureAsync(apiReq.ThreeDSessionData, apiReq);

            payment.ThreeDSecureComplete(req.Model.CRes);

            if (apiPayment.IsAuthorised()) {
                payment.Paid(apiPayment.Id, apiPayment.MessageId.GetValueOrThrow(), apiPayment.Message);
            } else if (apiPayment.IsDeclined()) {
                payment.Declined(apiPayment.Id, apiPayment.MessageId.GetValueOrThrow(), apiPayment.Message);
            } else {
                throw UnrecognisedValueException.For(apiPayment.Message);
            }
        } catch (ApiException apiException) {
            var apiPaymentError = JsonConvert.DeserializeObject<ApiPaymentError>(apiException.Content);

            if (apiPaymentError.IsDeclined()) {
                payment.Declined(apiPaymentError.TransactionId, apiPaymentError.Code, apiPaymentError.Message);
            } else {
                payment.Error(apiPaymentError.TransactionId, apiPaymentError.Code, apiPaymentError.Message);
            }
        }
    }
}
