using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Json;
using Newtonsoft.Json;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Net.Http;

namespace N3O.Umbraco.Cloud;

public class ClientFactory<T> {
    private const int RetryAttempts = 4;

    private readonly ICloudUrl _cloudUrl;
    private readonly ISubscriptionAccessor _subscriptionAccessor;
    private readonly ILogger<CloudApiClient<T>> _logger;
    private readonly IJsonProvider _jsonProvider;

    public ClientFactory(ICloudUrl cloudUrl,
                         ISubscriptionAccessor subscriptionAccessor,
                         ILogger<CloudApiClient<T>> logger,
                         IJsonProvider jsonProvider) {
        _cloudUrl = cloudUrl;
        _subscriptionAccessor = subscriptionAccessor;
        _logger = logger;
        _jsonProvider = jsonProvider;
    }

    public CloudApiClient<T> Create(CloudApiType apiType,
                                    string servicePath,
                                    string bearerToken,
                                    string onBehalfOf = null) {
        var httpClient = GetHttpClient(bearerToken, onBehalfOf);

        var baseUrl = _cloudUrl.ForApi(apiType, servicePath);

        var client = (T) Activator.CreateInstance(typeof(T), baseUrl, httpClient);

        _jsonProvider.ApplySettings((JsonSerializerSettings) client.GetPropertyInfo("JsonSerializerSettings").GetValue(client));

        return new CloudApiClient<T>(client, _jsonProvider, _logger);
    }

    private HttpClient GetHttpClient(string bearerToken, string onBehalfOf) {
        var transientErrorPolicyHandler = GetTransientErrorPolicyHttpMessageHandler();
        transientErrorPolicyHandler.InnerHandler = new HttpClientHandler();

        var subscription = _subscriptionAccessor.GetSubscription();
        var cloudApiHandler = new CloudApiHandler(subscription.Id,
                                                  bearerToken,
                                                  onBehalfOf,
                                                  transientErrorPolicyHandler);

        var httpClient = new HttpClient(cloudApiHandler);

        httpClient.Timeout = TimeSpan.FromMinutes(30);

        return httpClient;
    }

    private PolicyHttpMessageHandler GetTransientErrorPolicyHttpMessageHandler() {
        var policy = HttpPolicyExtensions.HandleTransientHttpError()
                                         .WaitAndRetryAsync(RetryAttempts,
                                                            retryAttempt => TimeSpan.FromSeconds(CloudConstants.Clients.HttpRetry.RetryIntervals[retryAttempt]));

        var handler = new PolicyHttpMessageHandler(policy);

        return handler;
    }
}