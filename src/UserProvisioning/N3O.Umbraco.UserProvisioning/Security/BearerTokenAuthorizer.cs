using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Rsk.AspNetCore.Scim.Hosting.Interfaces;
using Rsk.AspNetCore.Scim.Services.Interfaces;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace N3O.Umbraco.UserProvisioning.Security;

public class BearerTokenAuthorizer : IAuthorizeScimRequest {
    private const string Scheme = "Bearer ";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserProvisioningSettings _settings;

    public BearerTokenAuthorizer(IHttpContextAccessor httpContextAccessor, UserProvisioningSettings settings) {
        _httpContextAccessor = httpContextAccessor;
        _settings = settings;
    }

    public ValueTask<ScimAuthorizationResult> AuthorizeRequest(IScimActionContext context) {
        var header = _httpContextAccessor.HttpContext?.Request?.Headers[HeaderNames.Authorization].ToString();

        if (string.IsNullOrEmpty(header) || !header.StartsWith(Scheme, StringComparison.Ordinal)) {
            return ValueTask.FromResult(ScimAuthorizationResult.NotAuthenticated);
        }

        var presented = header.Substring(Scheme.Length);

        if (!FixedTimeEquals(presented, _settings.BearerToken)) {
            return ValueTask.FromResult(ScimAuthorizationResult.NotAuthorized);
        }

        return ValueTask.FromResult(ScimAuthorizationResult.Authorized);
    }

    // Compared over fixed-length hashes so the comparison cost does not reveal the token's length
    private static bool FixedTimeEquals(string presented, string expected) {
        var presentedHash = SHA256.HashData(Encoding.UTF8.GetBytes(presented));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));

        return CryptographicOperations.FixedTimeEquals(presentedHash, expectedHash);
    }
}
