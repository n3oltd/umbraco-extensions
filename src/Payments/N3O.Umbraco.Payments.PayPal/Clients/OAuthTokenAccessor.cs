using Microsoft.Extensions.Caching.Memory;
using N3O.Umbraco.Payments.PayPal.Clients.Models;
using N3O.Umbraco.Payments.PayPal.Models;
using N3O.Umbraco.Utilities;
using Refit;
using System;
using System.Threading.Tasks;

namespace N3O.Umbraco.Payments.PayPal.Clients;

public class OAuthTokenAccessor {
    private const int ExpiryMarginSeconds = 30;

    private static readonly MemoryCache Tokens = new(new MemoryCacheOptions());

    public void Evict(PayPalApiSettings apiSettings) {
        Tokens.Remove(GetCacheKey(apiSettings));
    }

    public async Task<string> GetTokenAsync(PayPalApiSettings apiSettings) {
        var token = await Tokens.GetOrCreateAsync(GetCacheKey(apiSettings), async cacheEntry => {
            var res = await RequestAccessTokenAsync(apiSettings);

            cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(res.ExpiresIn - ExpiryMarginSeconds);

            return res.AccessToken;
        });

        return token;
    }

    private string GetCacheKey(PayPalApiSettings apiSettings) {
        return CacheKey.Generate<OAuthTokenAccessor>(apiSettings.BaseUrl, apiSettings.ClientId);
    }

    private async Task<ApiAccessTokenRes> RequestAccessTokenAsync(PayPalApiSettings apiSettings) {
        var refitSettings = new RefitSettings();
        refitSettings.ContentSerializer = new NewtonsoftJsonContentSerializer();
        refitSettings.HttpMessageHandlerFactory = () => new AddBasicAuthorizationHandler(apiSettings.ClientId,
                                                                                         apiSettings.Secret);

        var apiClient = RestService.For<IOAuthTokenClient>(apiSettings.BaseUrl, refitSettings);

        var req = new ApiAccessTokenReq();
        req.GrantType = "client_credentials";

        var res = await apiClient.GetAccessTokenAsync(req);

        return res;
    }
}
