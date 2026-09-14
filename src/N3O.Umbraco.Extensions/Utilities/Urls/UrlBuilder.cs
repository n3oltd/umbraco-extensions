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
    
    public Url MediaUrl(string url) {
        if (!url.HasValue()) {
            throw new Exception("Could not build a media URL as no URL was given");
        }

        var urlSettings = _contentCache.Single<UrlSettingsContent>();
        var mediaBaseUrl = urlSettings?.MediaBaseUrl;

        if (!mediaBaseUrl.HasValue()) {
            mediaBaseUrl = urlSettings?.ProductionBaseUrl;
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
