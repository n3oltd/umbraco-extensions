using System;
using System.Collections.Generic;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace N3O.Umbraco.Content;

public interface IContentCache {
    IReadOnlyList<T> All<T>(Func<T, bool> predicate = null);
    IReadOnlyList<IPublishedContent> All(string contentTypeAlias, Func<IPublishedContent, bool> predicate = null);
    IReadOnlyList<T> AllInDefaultCulture<T>();
    bool CanCache();
    void Flush();
    T Single<T>(Func<T, bool> predicate = null);
    IPublishedContent Single(string contentTypeAlias, Func<IPublishedContent, bool> predicate = null);
    
    public event EventHandler Flushed;
}
