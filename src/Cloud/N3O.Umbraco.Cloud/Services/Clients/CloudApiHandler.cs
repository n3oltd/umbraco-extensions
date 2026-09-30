using N3O.Umbraco.Cloud.Models;
using StackExchange.Profiling.Internal;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud;

public class CloudApiHandler : DelegatingHandler {
    private readonly SubscriptionId _subscriptionId;
    private readonly string _bearerToken;
    private readonly string _onBehalfOf;

    public CloudApiHandler(SubscriptionId subscriptionId,
                           string bearerToken,
                           string onBehalfOf,
                           HttpMessageHandler innerHandler) {
        _subscriptionId = subscriptionId;
        _bearerToken = bearerToken;
        _onBehalfOf = onBehalfOf;

        InnerHandler = innerHandler;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                                 CancellationToken cancellationToken) {
        if (_bearerToken.HasValue()) {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
        }

        request.Headers.Add("N3O-Subscription-Id", _subscriptionId.Value);

        if (_onBehalfOf.HasValue()) {
            request.Headers.Add("N3O-OnBehalfOf", _onBehalfOf);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}