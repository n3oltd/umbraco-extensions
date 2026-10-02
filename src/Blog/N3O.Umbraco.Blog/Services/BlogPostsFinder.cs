using N3O.Umbraco.Blog.Content;
using N3O.Umbraco.Blog.Criteria;
using N3O.Umbraco.Blog.QueryFilters;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace N3O.Umbraco.Blog;

public class BlogPostsFinder : IBlogPostsFinder {
    private readonly IContentCache _contentCache;
    private readonly BlogPostQueryFilter _queryFilter;
    private readonly IVariationContextAccessor _variationContextAccessor;

    public BlogPostsFinder(IContentCache contentCache,
                           BlogPostQueryFilter queryFilter,
                           IVariationContextAccessor variationContextAccessor) {
        _contentCache = contentCache;
        _queryFilter = queryFilter;
        _variationContextAccessor = variationContextAccessor;
    }

    public IReadOnlyList<T> FindPosts<T>(BlogPostCriteria criteria = null) where T : IPublishedContent {
        var culture = _variationContextAccessor.VariationContext?.Culture;
        var all = _contentCache.All<T>()
                               .Where(x => !culture.HasValue() || x.IsInvariantOrHasCulture(culture))
                               .Select(x => x.As<BlogPostContent>())
                               .OrderByDescending(x => x.Date)
                               .Select(x => (T) x.Content())
                               .ToList();
        
        var results = criteria == null ? all : _queryFilter.Apply(all, criteria).ToList();

        return results;
    }
}
