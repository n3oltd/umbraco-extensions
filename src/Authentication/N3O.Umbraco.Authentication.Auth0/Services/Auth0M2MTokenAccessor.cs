using Microsoft.Extensions.Caching.Memory;
using N3O.Umbraco.Authentication.Auth0.Clients;
using N3O.Umbraco.Authentication.Auth0.Options;
using N3O.Umbraco.Json;
using Refit;
using System;
using System.Threading.Tasks;

namespace N3O.Umbraco.Authentication.Auth0;

public class Auth0M2MTokenAccessor {
    private static readonly MemoryCache Tokens = new(new MemoryCacheOptions());
    
    private readonly IJsonProvider _jsonProvider;

    public Auth0M2MTokenAccessor(IJsonProvider jsonProvider) {
        _jsonProvider = jsonProvider;
    }

    public async Task<string> GetTokenAsync(Auth0Application auth0Application, string apiIdentifier) {
        var cacheKey = $"{nameof(Auth0M2MTokenAccessor)}{nameof(GetTokenAsync)}{auth0Application.Domain}" +
                       $"{auth0Application.ClientId}{apiIdentifier}";

        var token = await Tokens.GetOrCreateAsync(cacheKey, async cacheEntry => {
            var jsonSettings = _jsonProvider.GetSettings();

            var refitSettings = new RefitSettings();
            jsonSettings.ContractResolver = new SnakeCasePropertyNamesContractResolver();
            refitSettings.ContentSerializer = new NewtonsoftJsonContentSerializer(jsonSettings);

            var apiClient = RestService.For<IAuth0TokenClient>($"https://{auth0Application.Domain}/", refitSettings);
            
            var req = new OAuthTokenReq();

            req.Audience = apiIdentifier;
            req.GrantType = "client_credentials";
            req.ClientId = auth0Application.ClientId;
            req.ClientSecret = auth0Application.ClientSecret;

            var res = await apiClient.GetOAuthTokenAsync(req);

            cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(res.ExpiresIn - 30);

            return res.AccessToken;
        });

        return token;
    }
}
