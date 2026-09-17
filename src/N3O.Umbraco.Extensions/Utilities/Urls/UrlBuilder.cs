using Flurl;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using System;

namespace N3O.Umbraco.Utilities;

public class UrlBuilder : IUrlBuilder {
    private const string MediaUrlKey = "Platforms:MediaUrl";

    private readonly IConfiguration _configuration;
    private readonly IContentCache _contentCache;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public UrlBuilder(IConfiguration configuration,
                      IContentCache contentCache,
                      IWebHostEnvironment webHostEnvironment) {
        _configuration = configuration;
        _contentCache = contentCache;
        _webHostEnvironment = webHostEnvironment;
    }
    
    public Url MediaUrl(string url) {
        if (!url.HasValue()) {
            throw new Exception("Could not build a media URL as no URL was given");
        }

        var mediaBaseUrl = _configuration[MediaUrlKey];

        // TODO Drop this leg once every deployed site has the platforms media URL in its configuration.
        if (!mediaBaseUrl.HasValue()) {
            mediaBaseUrl = _contentCache.Single<UrlSettingsContent>()?.ProductionBaseUrl;
        }

        if (!mediaBaseUrl.HasValue()) {
            throw new Exception($"Could not build a media URL for {url.Quote()} as no media base URL is set");
        }

        var src = new Url(url);
        var mediaUrl = new Url(mediaBaseUrl);

        if (mediaUrl.IsRelative) {
            throw new Exception($"Could not build a media URL as {mediaBaseUrl.Quote()} is not absolute");
        }

        mediaUrl.AppendPathSegment(src.Path);
        mediaUrl.Query = src.Query;

        return mediaUrl;
    }

    public Url Root() {
        var urlSettings = _contentCache.Single<UrlSettingsContent>();

        if (urlSettings == null) {
            return null;
        }

        return urlSettings.BaseUrl(_webHostEnvironment);
    }
}
