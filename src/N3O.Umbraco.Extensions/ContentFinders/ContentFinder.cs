using Microsoft.Extensions.Logging;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Extensions;

namespace N3O.Umbraco.ContentFinders;

public abstract class ContentFinder : IContentFinder {
    private readonly ILogger<ContentFinder> _logger;

    protected ContentFinder(ILogger<ContentFinder> logger, IContentCache contentCache) {
        _logger = logger;
        ContentCache = contentCache;
    }

    public async Task<bool> TryFindContent(IPublishedRequestBuilder request) {
        try {
            return await FindContentAsync(request);
        } catch (Exception ex) {
            _logger.LogError(ex, "Error executing content finder");
            
            return false;
        }
    }

    protected abstract Task<bool> FindContentAsync(IPublishedRequestBuilder request);

    protected Task<bool> TryFindRelocatedContentAsync(string pageTypeAlias,
                                                      string contentTypeAlias,
                                                      string contentCollectionTypeAlias,
                                                      IPublishedRequestBuilder request) {
        var pages = ContentCache.All(pageTypeAlias);
        var contentCollections = ContentCache.All(contentCollectionTypeAlias);
        
        if (pages.None() || contentCollections.None()) {
            return Task.FromResult(false);
        }
        
        if (!pages.IsSingle()) {
            throw new Exception($"Found multiple pages for {pageTypeAlias}: {pages.Select(x => x.Id).ToCsv(true)}");
        }
        
        if (!contentCollections.IsSingle()) {
            throw new Exception($"Found multiple content collections for {contentCollectionTypeAlias}: {contentCollections.Select(x => x.Id).ToCsv(true)}");
        }
    
        var culture = request.Culture;
        var pagePath = GetRelativePath(pages.Single(), culture);
    
        var path = GetRequestedPath(request.Uri, pagePath);

        if (path == null) {
            return Task.FromResult(false);
        }

        var match = contentCollections.Single()
                                      .Descendants(culture)
                                     .Where(x => x.ContentType.Alias.EqualsInvariant(contentTypeAlias))
                                     .FirstOrDefault(x => StrippedPath(x, pagePath, culture).EqualsInvariant(path));

        if (match != null) {
            request.SetPublishedContent(match);

            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    protected string StrippedPath(IPublishedContent content, string strip, string culture = null) {
        return GetRelativePath(content, culture).Substring(strip.Length).RemoveLeadingSlashes();
    }

    protected string GetRequestedPath(Uri url, string strip = null) {
        var path = url.GetAbsolutePathDecoded().ToLowerInvariant();
        
        strip ??= "";

        if (strip.HasValue() && !path.StartsWith(strip.ToLowerInvariant())) {
            return null;
        }
    
        return path.Substring(strip.Length).RemoveLeadingSlashes();
    }

    protected IContentCache ContentCache { get; }

    private string GetRelativePath(IPublishedContent content, string culture) {
        var url = culture.HasValue() ? content.Url(culture, UrlMode.Relative) : content.RelativeUrl();

        return Uri.UnescapeDataString(url);
    }
}
