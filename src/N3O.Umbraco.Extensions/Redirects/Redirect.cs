using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Linq;

namespace N3O.Umbraco.Redirects;

public class Redirect : Value {
    public Redirect(string urlOrPath, bool temporary) {
        UrlOrPath = urlOrPath;
        Temporary = temporary;
    }

    public string UrlOrPath { get; }
    public bool Temporary { get; }

    public string GetUrl(QueryString queryString) {
        var urlOrPath = UrlOrPath.Trim();
        var incoming = queryString.ToUriComponent();

        if (incoming.Length <= 1 || !CanForwardQuery(urlOrPath) || incoming.Any(x => x < 0x20 || x > 0x7E)) {
            return urlOrPath;
        }

        var fragmentIndex = urlOrPath.IndexOf('#');
        var fragment = fragmentIndex >= 0 ? urlOrPath.Substring(fragmentIndex) : string.Empty;
        var url = fragmentIndex >= 0 ? urlOrPath.Substring(0, fragmentIndex) : urlOrPath;
        var queryIndex = url.IndexOf('?');
        var path = queryIndex >= 0 ? url.Substring(0, queryIndex) : url;
        var targetQuery = queryIndex >= 0 ? new QueryString(url.Substring(queryIndex)) : QueryString.Empty;
        var targetKeys = QueryHelpers.ParseQuery(targetQuery.Value).Keys;

        var forwarded = incoming.Substring(1)
                                .Split('&')
                                .Where(x => !targetKeys.Contains(GetKey(x), StringComparer.OrdinalIgnoreCase))
                                .ToList();

        if (!forwarded.Any()) {
            return urlOrPath;
        }

        var query = targetQuery.Add(new QueryString($"?{string.Join("&", forwarded)}"));

        return $"{path}{query.Value}{fragment}";
    }

    private static bool CanForwardQuery(string urlOrPath) {
        if (urlOrPath.StartsWith('/')) {
            return true;
        }

        return Uri.TryCreate(urlOrPath, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static string GetKey(string pair) {
        var key = pair.Split('=')[0];

        return Uri.UnescapeDataString(key.Replace('+', ' '));
    }
}
