using Flurl;
using Microsoft.AspNetCore.Hosting;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using System;
using System.Linq;

namespace N3O.Umbraco.Utilities;

public class UrlBuilder : IUrlBuilder {
    private readonly IContentCache _contentCache;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public UrlBuilder(IContentCache contentCache, IWebHostEnvironment webHostEnvironment) {
        _contentCache = contentCache;
        _webHostEnvironment = webHostEnvironment;
    }
    
    public Url Root() {
        var urlSettings = _contentCache.Single<UrlSettingsContent>();

        if (urlSettings == null) {
            return null;
        }

        var baseUrl = urlSettings.BaseUrl(_webHostEnvironment)?.ToString();

        if (!baseUrl.HasValue()) {
            return null;
        }

        if (!IsValidRoot(baseUrl)) {
            throw new Exception($"The configured base URL {baseUrl.Quote()} is not a valid site root");
        }

        return baseUrl;
    }

    private static bool IsValidRoot(string baseUrl) {
        if (!Url.IsValid(baseUrl)) {
            return false;
        }

        var url = new Url(baseUrl);
        var schemes = new[] { Uri.UriSchemeHttp, Uri.UriSchemeHttps };

        // Flurl accepts a value whose scheme is repeated, parsing the second one as the first
        // path segment, so an empty segment is what distinguishes it from a well formed URL
        return schemes.Contains(url.Scheme) &&
               Uri.CheckHostName(url.Host) == UriHostNameType.Dns &&
               url.PathSegments.All(x => x.HasValue());
    }
}
