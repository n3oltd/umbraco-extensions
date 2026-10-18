using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Utilities;

public class AddBasicAuthorizationHandler : DelegatingHandler {
    private readonly string _base64Credentials;

    public AddBasicAuthorizationHandler(string username, string password) {
        _base64Credentials = Base64.Encode($"{username}:{password}");

        InnerHandler = new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                                 CancellationToken cancellationToken) {
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _base64Credentials);

        return await base.SendAsync(request, cancellationToken);
    }
}
