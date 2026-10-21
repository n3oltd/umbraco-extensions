using Flurl;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Extensions;

namespace N3O.Umbraco.UrlProviders;

public abstract class UrlProvider : IUrlProvider {
    private readonly ILogger<UrlProvider> _logger;
    private readonly NewDefaultUrlProvider _defaultUrlProvider;
    private readonly IContentCache _contentCache;

    protected UrlProvider(ILogger<UrlProvider> logger,
                          NewDefaultUrlProvider defaultUrlProvider,
                          IContentCache contentCache) {
        _logger = logger;
        _defaultUrlProvider = defaultUrlProvider;
        _contentCache = contentCache;
    }

    public abstract string Alias { get; }
    
    public UrlInfo GetUrl(IPublishedContent content, UrlMode mode, string culture, Uri current) {
        try {
            return ResolveUrl(content, mode, culture, current);
        } catch (Exception ex) {
            _logger.LogError(ex, "Error executing GetUrl for URL provider");
            
            return null;
        }
    }

    public IEnumerable<UrlInfo> GetOtherUrls(int id, Uri current) {
        try {
            return ResolveOtherUrls(id, current);
        } catch (Exception ex) {
            _logger.LogError(ex, "Error executing GetOtherUrls for URL provider");
            
            return [];
        }
    }
    
    public virtual Task<UrlInfo> GetPreviewUrlAsync(IContent content, string culture, string segment) {
        return _defaultUrlProvider.GetPreviewUrlAsync(content, culture, segment);
    }

    protected virtual IEnumerable<UrlInfo> ResolveOtherUrls(int id, Uri current) => [];
    
    protected abstract UrlInfo ResolveUrl(IPublishedContent content, UrlMode mode, string culture, Uri current);
    
    protected UrlInfo TryGetRelocatedUrl(string pageTypeAlias,
                                         string contentTypeAlias,
                                         IPublishedContent content,
                                         UrlMode mode,
                                         string culture,
                                         Uri current) {
        if (content != null && content.ContentType.Alias.EqualsInvariant(contentTypeAlias)) {
            var pages = _contentCache.All(pageTypeAlias);
            
            if (pages.None()) {
                return null;
            }
            
            if (!pages.IsSingle()) {
                throw new Exception($"Found multiple pages for {pageTypeAlias}: {pages.Select(x => x.Id).ToCsv(true)}");
            }

            var urlSegment = GetUrlSegment(content, culture);

            if (!urlSegment.HasValue()) {
                return null;
            }

            var defaultUrl = _defaultUrlProvider.GetUrl(pages.Single(), mode, culture, current);
            var url = new Url(defaultUrl.Url);

            url.AppendPathSegment(urlSegment);

            return UrlInfo.AsUrl(url.ToString(), Alias, culture);
        }

        return null;
    }

    protected UrlInfo TryGetRelocatedUrl(string pageTypeAlias,
                                         string contentTypeAlias,
                                         string contentCollectionTypeAlias,
                                         IPublishedContent content,
                                         UrlMode mode,
                                         string culture,
                                         Uri current) {
        if (content != null && content.ContentType.Alias.EqualsInvariant(contentTypeAlias)) {
            var collection = _contentCache.Single(contentCollectionTypeAlias);
            var page = _contentCache.Single(pageTypeAlias);

            if (collection == null || page == null) {
                return null;
            }

            var contentDefaultUrl = _defaultUrlProvider.GetUrl(content, mode, culture, current);

            return UrlInfo.AsUrl(contentDefaultUrl.Url?.ToString().Replace(collection.Url(), page.Url()), Alias, culture);
        }

        return null;
    }

    private string GetUrlSegment(IPublishedContent content, string culture) {
        if (!culture.HasValue() || !content.ContentType.VariesByCulture()) {
            return content.UrlSegment;
        }

        return content.Cultures.TryGetValue(culture, out var cultureInfo) ? cultureInfo.UrlSegment : null;
    }
}
