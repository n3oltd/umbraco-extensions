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
            PooledConnectionLifetime = CloudConstants.Clients.PooledConnectionLifetime,
            // Waiting for a connection happens inside the send, so the client timeout covers it.
            MaxConnectionsPerServer = CloudConstants.Clients.MaxConnectionsPerServer
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

        var httpClient = new HttpClient(cloudApiHandler, disposeHandler: false);

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
