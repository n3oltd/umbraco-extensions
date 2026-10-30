using N3O.Umbraco.Blog.Content;
using N3O.Umbraco.Blog.Criteria;
using N3O.Umbraco.Blog.QueryFilters;
using N3O.Umbraco.Content;
using N3O.Umbraco.Context;
using N3O.Umbraco.Extensions;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace N3O.Umbraco.Blog;

public class BlogPostsFinder : IBlogPostsFinder {
    private readonly IContentCache _contentCache;
    private readonly ICultureAccessor _cultureAccessor;
    private readonly BlogPostQueryFilter _queryFilter;

    public BlogPostsFinder(IContentCache contentCache,
                           ICultureAccessor cultureAccessor,
                           BlogPostQueryFilter queryFilter) {
        _contentCache = contentCache;
        _cultureAccessor = cultureAccessor;
        _queryFilter = queryFilter;
    }

    public IReadOnlyList<T> FindPosts<T>(BlogPostCriteria criteria = null) where T : IPublishedContent {
        var culture = _cultureAccessor.GetCulture();
        var all = _contentCache.All<T>()
                               .Where(x => x.IsInvariantOrHasCulture(culture))
                               .Select(x => x.As<BlogPostContent>())
                               .OrderByDescending(x => x.Date)
                               .Select(x => (T) x.Content())
                               .ToList();
        
        var results = criteria == null ? all : _queryFilter.Apply(all, criteria).ToList();

        return results;
    }
}
