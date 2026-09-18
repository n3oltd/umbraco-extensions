using Flurl;
using Microsoft.Extensions.Configuration;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Utilities;
using System;

namespace N3O.Umbraco.Cloud.Platforms;

public class PlatformsMediaUrlBuilder : IPlatformsMediaUrlBuilder {
    private readonly IConfiguration _configuration;
    private readonly IContentCache _contentCache;

    public PlatformsMediaUrlBuilder(IConfiguration configuration, IContentCache contentCache) {
        _configuration = configuration;
        _contentCache = contentCache;
    }

    public Url Build(string url) {
        if (!url.HasValue()) {
            throw new Exception("Could not build a media URL as no URL was given");
        }

        var section = _configuration.GetSection(PlatformsSchemaConstants.ConfigurationSection);
        var mediaBaseUrl = section[nameof(PlatformsFeatureSettings.MediaUrl)];

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
}
