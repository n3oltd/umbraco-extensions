using Auth0.AuthenticationApi;
using Auth0.ManagementApi;
using Microsoft.Extensions.Options;
using N3O.Umbraco.Authentication.Auth0.Lookups;
using N3O.Umbraco.Authentication.Auth0.Options;
using N3O.Umbraco.Exceptions;
using System.Net.Http;
using System.Threading.Tasks;

namespace N3O.Umbraco.Authentication.Auth0;

public class Auth0ClientFactory : IAuth0ClientFactory {
    private readonly AuthenticationOptions _authenticationOptions;
    private readonly Auth0M2MTokenAccessor _tokenAccessor;
    private readonly IHttpClientFactory _httpClientFactory;

    public Auth0ClientFactory(IOptions<AuthenticationOptions> authenticationOptions,
                              Auth0M2MTokenAccessor tokenAccessor,
                              IHttpClientFactory httpClientFactory) {
        _authenticationOptions = authenticationOptions.Value;
        _tokenAccessor = tokenAccessor;
        _httpClientFactory = httpClientFactory;
    }
    
    public AuthenticationApiClient GetAuthenticationApiClient(UserDirectoryType userDirectoryType) {
        var loginOptions = GetClientOptions(userDirectoryType).Login;
        var httpClient = _httpClientFactory.CreateClient();
        var connection = new HttpClientAuthenticationConnection(httpClient);
            
        var auth0Client = new AuthenticationApiClient(loginOptions.Domain, connection);
            
        return auth0Client;
    }
    
    public async Task<IManagementApiClient> GetManagementApiClientAsync(UserDirectoryType userDirectoryType) {
        var managementOptions = GetClientOptions(userDirectoryType).Management;

        var tokenProvider = new DelegateTokenProvider(_ => {
            return _tokenAccessor.GetTokenAsync(managementOptions, managementOptions.ApiIdentifier);
        });

        return ManagementClientBuilder.Build(_httpClientFactory, tokenProvider, managementOptions.Domain);
    }
    
    private Auth0AuthenticationOptions GetClientOptions(UserDirectoryType userDirectoryType) {
        if (userDirectoryType == UserDirectoryTypes.BackOffice) {
            return _authenticationOptions.BackOffice.Auth0;
        } else if (userDirectoryType == UserDirectoryTypes.Members) {
            return _authenticationOptions.Members.Auth0;
        } else {
            throw UnrecognisedValueException.For(userDirectoryType);
        }
    }
}