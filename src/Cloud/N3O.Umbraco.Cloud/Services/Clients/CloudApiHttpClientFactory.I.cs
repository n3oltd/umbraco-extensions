using System.Net.Http;

namespace N3O.Umbraco.Cloud;

public interface ICloudApiHttpClientFactory {
    HttpClient CreateClient(string bearerToken, string onBehalfOf);
}
