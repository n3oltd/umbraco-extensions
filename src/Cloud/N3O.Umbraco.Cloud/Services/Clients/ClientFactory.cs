using Microsoft.Extensions.Logging;
using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Json;
using Newtonsoft.Json;
using System;

namespace N3O.Umbraco.Cloud;

public class ClientFactory<T> {
    private const string BaseUrl = nameof(BaseUrl);

    private readonly ICloudUrl _cloudUrl;
    private readonly ICloudApiHttpClientFactory _httpClientFactory;
    private readonly ILogger<CloudApiClient<T>> _logger;
    private readonly IJsonProvider _jsonProvider;

    public ClientFactory(ICloudUrl cloudUrl,
                         ICloudApiHttpClientFactory httpClientFactory,
                         ILogger<CloudApiClient<T>> logger,
                         IJsonProvider jsonProvider) {
        _cloudUrl = cloudUrl;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _jsonProvider = jsonProvider;
    }

    public CloudApiClient<T> Create(CloudApiType apiType,
                                    string bearerToken,
                                    string onBehalfOf = null) {
        var httpClient = _httpClientFactory.CreateClient(bearerToken, onBehalfOf);

        var client = (T) Activator.CreateInstance(typeof(T), httpClient);

        var baseUrl = _cloudUrl.ForApi(apiType, (string) typeof(T).GetProperty(BaseUrl).GetValue(client));

        _jsonProvider.ApplySettings((JsonSerializerSettings) client.GetPropertyInfo("JsonSerializerSettings").GetValue(client));

        client.SetPropertyValue(BaseUrl, baseUrl);

        return new CloudApiClient<T>(client, _jsonProvider, _logger);
    }
}