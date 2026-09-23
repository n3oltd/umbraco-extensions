using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.Security.Cryptography;
using System.Text;

namespace N3O.Umbraco.UserProvisioning.Security;

public class BearerTokenAuthorizer : IBearerTokenAuthorizer {
    private const string Scheme = "Bearer ";

    private readonly UserProvisioningSettings _settings;

    public BearerTokenAuthorizer(UserProvisioningSettings settings) {
        _settings = settings;
    }

    public bool IsAuthorized(HttpContext context) {
        var header = context.Request.Headers[HeaderNames.Authorization].ToString();

        if (string.IsNullOrEmpty(header) || !header.StartsWith(Scheme, StringComparison.Ordinal)) {
            return false;
        }

        return FixedTimeEquals(header.Substring(Scheme.Length), _settings.BearerToken);
    }

    // Hashed first because FixedTimeEquals short circuits on unequal lengths, which leaks the token's length
    private static bool FixedTimeEquals(string presented, string expected) {
        var presentedHash = SHA256.HashData(Encoding.UTF8.GetBytes(presented));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));

        return CryptographicOperations.FixedTimeEquals(presentedHash, expectedHash);
    }
}
