using N3O.Umbraco.Extensions;
using N3O.Umbraco.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Collections;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace N3O.Umbraco.Content;

public class ContentCache : IContentCache {
    private readonly IContentLocator _contentLocator;
    private readonly ConcurrentDictionary<string, object> _typedStore = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly ConcurrentDictionary<string, IReadOnlyList<IPublishedContent>> _untypedStore = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly ConcurrentHashSet<string> _heldContentTypes = [];

    public ContentCache(IContentLocator contentLocator) {
        _contentLocator = contentLocator;
    }

    public IReadOnlyList<T> All<T>(Func<T, bool> predicate = null) {
        var cacheKey = GetCacheKey<T>();

        if (!_typedStore.TryGetValue(cacheKey, out var stored)) {
            var located = _contentLocator.All<T>();

            stored = CanCache(located) ? _typedStore.GetOrAdd(cacheKey, located) : located;
        }

        var all = (IReadOnlyList<T>) stored;

        _heldContentTypes.AddIfNotExists(AliasHelper<T>.ContentTypeAlias().ToLowerInvariant());

        IReadOnlyList<T> res;

        if (predicate == null) {
            res = all;
        } else {
            res = all.Where(predicate).ToList();
        }

        return res;
    }
    
    public IReadOnlyList<IPublishedContent> All(string contentTypeAlias,
                                                Func<IPublishedContent, bool> predicate = null) {
        var cacheKey = GetCacheKey(contentTypeAlias);

        if (!_untypedStore.TryGetValue(cacheKey, out var all)) {
            var located = _contentLocator.All(contentTypeAlias);

            all = CanCache(located) ? _untypedStore.GetOrAdd(cacheKey, located) : located;
        }

        if (contentTypeAlias.HasValue()) {
            _heldContentTypes.AddIfNotExists(contentTypeAlias.ToLowerInvariant());
        }

        IReadOnlyList<IPublishedContent> res;

        if (predicate == null) {
            res = all;
        } else {
            res = all.Where(predicate).ToList();
        }

        return res;
    }
    
    public bool ContainsContentType(string contentTypeAlias) {
        return _heldContentTypes.Contains(contentTypeAlias.ToLowerInvariant());
    }

    public void Flush() {
        _heldContentTypes.Clear();
        _typedStore.Clear();
        _untypedStore.Clear();
        
        Flushed?.Invoke(this, EventArgs.Empty);
    }

    public T Single<T>(Func<T, bool> predicate = null) {
        return All(predicate).SingleOrDefault();
    }

    public IPublishedContent Single(string contentTypeAlias, Func<IPublishedContent, bool> predicate = null) {
        return All(contentTypeAlias, predicate).SingleOrDefault();
    }

    public event EventHandler Flushed;

    // An empty result is only trustworthy when the published snapshot is readable. While a snapshot is
    // loading or rebuilding the tree reads as empty without throwing, and caching that would serve "no
    // content" from this singleton until the next publish happens to flush it.
    private bool CanCache<T>(IReadOnlyList<T> located) {
        return located.Any() || _contentLocator.AnyAtRoot();
    }

    private string GetCacheKey<T>() {
        // Not AliasHelper<T>.ContentTypeAlias() as need to distinguish T and TContent : UmbracoContent<TContent>
        return GetCacheKey(typeof(T).FullName);
    }
    
    private string GetCacheKey(string value) {
        var cacheKey = CacheKey.Generate<ContentCache>(value);
        
        return cacheKey;
    }
}
