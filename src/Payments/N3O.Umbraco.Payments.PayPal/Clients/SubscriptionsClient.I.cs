using N3O.Umbraco.Payments.PayPal.Clients.Models;
using Refit;
using System.Threading.Tasks;

namespace N3O.Umbraco.Payments.PayPal.Clients;

public interface ISubscriptionsClient {
    [Post("/v1/billing/subscriptions/{subscriptionId}/activate")]
    Task ActivateSubscriptionAsync(string subscriptionId, [Body] ApiActivateSubscriptionReq req);
}