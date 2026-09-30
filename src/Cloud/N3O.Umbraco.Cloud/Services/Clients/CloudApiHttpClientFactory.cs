using System;
using System.Net.Http;

namespace N3O.Umbraco.Cloud;

public class CloudApiHttpClientFactory : ICloudApiHttpClientFactory, IDisposable {
    private readonly ISubscriptionAccessor _subscriptionAccessor;
    private readonly SocketsHttpHandler _primaryHandler;

    public CloudApiHttpClientFactory(ISubscriptionAccessor subscriptionAccessor) {
        _subscriptionAccessor = subscriptionAccessor;

        _primaryHandler = new SocketsHttpHandler {
            PooledConnectionLifetime = CloudConstants.Clients.PooledConnectionLifetime,
            MaxConnectionsPerServer = CloudConstants.Clients.MaxConnectionsPerServer
        };
    }

    public HttpClient CreateClient(string bearerToken, string onBehalfOf) {
        var subscription = _subscriptionAccessor.GetSubscription();
        var cloudApiHandler = new CloudApiHandler(subscription.Id,
                                                  bearerToken,
                                                  onBehalfOf,
                                                  _primaryHandler);

        var httpClient = new HttpClient(cloudApiHandler, disposeHandler: false);

        httpClient.Timeout = CloudConstants.Clients.Timeout;

        return httpClient;
    }

    public void Dispose() {
        _primaryHandler.Dispose();
    }
}
