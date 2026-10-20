using N3O.Umbraco.Payments.PayPal.Models;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Payments.PayPal.Clients;

public class AuthorizationHandler : DelegatingHandler {
    private readonly PayPalApiSettings _apiSettings;
    private readonly OAuthTokenAccessor _oAuthTokenAccessor;

    public AuthorizationHandler(OAuthTokenAccessor oAuthTokenAccessor, PayPalApiSettings apiSettings) {
        _apiSettings = apiSettings;
        _oAuthTokenAccessor = oAuthTokenAccessor;

        InnerHandler = new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                                 CancellationToken cancellationToken) {
        var response = await SendWithTokenAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized) {
            return response;
        }

        response.Dispose();

        _oAuthTokenAccessor.Evict(_apiSettings);

        return await SendWithTokenAsync(request, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendWithTokenAsync(HttpRequestMessage request,
                                                               CancellationToken cancellationToken) {
        var token = await _oAuthTokenAccessor.GetTokenAsync(_apiSettings);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}
