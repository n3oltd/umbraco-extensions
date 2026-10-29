using N3O.Umbraco.Payments.Handlers;
using N3O.Umbraco.Payments.Models;
using N3O.Umbraco.Payments.PayPal.Clients;
using N3O.Umbraco.Payments.PayPal.Clients.Models;
using N3O.Umbraco.Payments.PayPal.Commands;
using N3O.Umbraco.Payments.PayPal.Models;
using Refit;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Payments.PayPal.Handlers;

public class CaptureSubscriptionHandler :
    PaymentsHandler<CaptureSubscriptionCommand, PayPalSubscriptionReq, PayPalCredential> {
    private readonly ISubscriptionsClient _subscriptionsClient;

    public CaptureSubscriptionHandler(IPaymentsScope paymentsScope, ISubscriptionsClient subscriptionsClient)
        : base(paymentsScope) {
        _subscriptionsClient = subscriptionsClient;
    }

    protected override async Task HandleAsync(CaptureSubscriptionCommand req,
                                              PayPalCredential credential,
                                              PaymentsParameters parameters,
                                              CancellationToken cancellationToken) {
        try {
            var apiReq = new ApiActivateSubscriptionReq();
            apiReq.Id = req.Model.SubscriptionId;
            apiReq.Reason = req.Model.Reason;
            
            await _subscriptionsClient.ActivateSubscriptionAsync(apiReq.Id, apiReq);

            credential.SubscriptionCreated(req.Model.SubscriptionId, req.Model.Reason);
        } catch (ApiException apiException) {
            credential.Error((int) apiException.StatusCode, apiException.Message);
        }
    }
}