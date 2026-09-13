using Flurl;
using Microsoft.AspNetCore.Hosting;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using System;

namespace N3O.Umbraco.Utilities;

public class UrlBuilder : IUrlBuilder {
    private readonly IContentCache _contentCache;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public UrlBuilder(IContentCache contentCache, IWebHostEnvironment webHostEnvironment) {
        _contentCache = contentCache;
        _webHostEnvironment = webHostEnvironment;
    }
    
    public Url ProductionUrl(string url) {
        var productionBaseUrl = _contentCache.Single<UrlSettingsContent>()?.ProductionBaseUrl;

        if (!productionBaseUrl.HasValue()) {
            throw new Exception($"Could not build a production URL for {url.Quote()} as no production base URL is set");
        }

        var src = new Url(url);
        var productionUrl = new Url(productionBaseUrl);

        productionUrl.AppendPathSegment(src.Path);
        productionUrl.Query = src.Query;

        return productionUrl;
    }

    public Url Root() {
        var urlSettings = _contentCache.Single<UrlSettingsContent>();

        if (urlSettings == null) {
            return null;
        }

        return urlSettings.BaseUrl(_webHostEnvironment);
    }
}
