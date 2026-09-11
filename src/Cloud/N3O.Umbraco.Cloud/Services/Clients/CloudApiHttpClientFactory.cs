using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Net.Http;

namespace N3O.Umbraco.Cloud;

public class CloudApiHttpClientFactory : ICloudApiHttpClientFactory, IDisposable {
    private const int RetryAttempts = 4;

    private readonly ISubscriptionAccessor _subscriptionAccessor;
    private readonly SocketsHttpHandler _primaryHandler;
    private readonly IAsyncPolicy<HttpResponseMessage> _transientErrorPolicy;

    public CloudApiHttpClientFactory(ISubscriptionAccessor subscriptionAccessor) {
        _subscriptionAccessor = subscriptionAccessor;

        _primaryHandler = new SocketsHttpHandler {
            // Connections belong to the primary handler, so every caller shares this one instance. Capping
            // their lifetime is what lets a DNS change be picked up without recycling the handler itself.
            PooledConnectionLifetime = CloudConstants.Clients.PooledConnectionLifetime
        };

        _transientErrorPolicy = HttpPolicyExtensions.HandleTransientHttpError()
                                                    .WaitAndRetryAsync(RetryAttempts, GetRetryInterval);
    }

    public HttpClient CreateClient(string bearerToken, string onBehalfOf) {
        var transientErrorPolicyHandler = new PolicyHttpMessageHandler(_transientErrorPolicy);
        transientErrorPolicyHandler.InnerHandler = _primaryHandler;

        var subscription = _subscriptionAccessor.GetSubscription();
        var cloudApiHandler = new CloudApiHandler(subscription.Id,
                                                  bearerToken,
                                                  onBehalfOf,
                                                  transientErrorPolicyHandler);

        // The chain bottoms out at the shared primary handler, so disposing the client must not dispose it.
        var httpClient = new HttpClient(cloudApiHandler, false);

        httpClient.Timeout = CloudConstants.Clients.Timeout;

        return httpClient;
    }

    public void Dispose() {
        _primaryHandler.Dispose();
    }

    private TimeSpan GetRetryInterval(int retryAttempt) {
        return TimeSpan.FromSeconds(CloudConstants.Clients.HttpRetry.RetryIntervals[retryAttempt]);
    }
}
