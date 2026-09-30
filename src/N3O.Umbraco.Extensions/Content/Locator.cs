using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Extensions;

namespace N3O.Umbraco.Content;

public abstract class Locator : ILocator {
    public IReadOnlyList<IPublishedContent> All(Func<IPublishedContent, bool> predicate = null) {
        return All(null, predicate);
    }

    public IReadOnlyList<IPublishedContent> All(string contentTypeAlias,
                                                Func<IPublishedContent, bool> predicate = null) {
        var allContent = GetAllContent(contentTypeAlias, null);
        var filteredContent = allContent.Where(x => predicate?.Invoke(x) ?? true).ToList();

        return filteredContent;
    }

    public IReadOnlyList<T> All<T>(Func<T, bool> predicate = null) {
        if (predicate == null) {
            return All(AliasHelper<T>.ContentTypeAlias()).Select(x => x.As<T>()).ToList();
        } else {
            return All(AliasHelper<T>.ContentTypeAlias(), x => predicate(x.As<T>())).Select(x => x.As<T>()).ToList();
        }
    }

    public IReadOnlyList<IPublishedContent> AllInCulture(string contentTypeAlias, string culture) {
        return GetAllContent(contentTypeAlias, culture);
    }

    public IPublishedContent ById(int id) {
        return PublishedCache.GetById(id);
    }

    public T ById<T>(int id) {
        return ById(id).As<T>();
    }

    public IPublishedContent ById(Guid id) {
        return PublishedCache.GetById(id);
    }

    public T ById<T>(Guid id) {
        return ById(id).As<T>();
    }

    public IPublishedContent Single(string contentTypeAlias, Func<IPublishedContent, bool> predicate = null) {
        return All(contentTypeAlias, predicate).SingleOrDefault();
    }

    public T Single<T>(Func<T, bool> predicate = null) {
        if (predicate == null) {
            return Single(AliasHelper<T>.ContentTypeAlias()).As<T>();
        } else {
            return Single(AliasHelper<T>.ContentTypeAlias(), x => predicate(x.As<T>())).As<T>();
        }
    }

    private IReadOnlyList<IPublishedContent> GetAllContent(string contentTypeAlias, string culture) {
        var allContent = new List<IPublishedContent>();

        foreach (var rootContent in GetRootContents()) {
            if (rootContent == null) {
                continue;
            }

            if (culture != null && !rootContent.IsInvariantOrHasCulture(culture)) {
                continue;
            }

            if (contentTypeAlias == null) {
                allContent.AddRange(rootContent.DescendantsOrSelf(culture));
            } else {
                if (rootContent.ContentType.Alias.EqualsInvariant(contentTypeAlias)) {
                    allContent.Add(rootContent);
                }

                allContent.AddRange(rootContent.DescendantsOfType(contentTypeAlias, culture));
            }
        }

        return allContent;
    }

    protected abstract IPublishedCache PublishedCache { get; }
    protected abstract IEnumerable<IPublishedContent> GetRootContents();
}
