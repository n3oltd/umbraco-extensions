using Microsoft.AspNetCore.Http;
using System;
using System.Security.Cryptography;
using System.Text;

namespace N3O.Umbraco.Extensions;

public static class HttpRequestExtensions {
    public static bool HasKey(this HttpRequest request, string headerName, string expectedKey) {
        if (!expectedKey.HasValue()) {
            return false;
        }

        request.Headers.TryGetValue(headerName, out var suppliedKey);

        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expectedKey),
                                                       Encoding.UTF8.GetBytes(suppliedKey.ToString()));
    }

    public static Uri Uri(this HttpRequest request) {
        var builder = new UriBuilder();
        builder.Scheme = request.Scheme;
        builder.Host = request.Host.Host;
        builder.Path = request.Path;
        builder.Query = request.QueryString.ToUriComponent();

        if (request.Host.Port.HasValue()) {
            builder.Port = request.Host.Port.GetValueOrThrow();
        }

        return builder.Uri;
    }
}
