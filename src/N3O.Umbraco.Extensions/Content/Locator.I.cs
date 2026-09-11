using System;
using System.Collections.Generic;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace N3O.Umbraco.Content;

public interface ILocator {
    IReadOnlyList<IPublishedContent> All(Func<IPublishedContent, bool> predicate = null);
    IReadOnlyList<IPublishedContent> All(string contentTypeAlias, Func<IPublishedContent, bool> predicate = null);
    IReadOnlyList<T> All<T>(Func<T, bool> predicate = null);

    IPublishedContent ById(int id);
    T ById<T>(int id);

    IPublishedContent ById(Guid id);
    T ById<T>(Guid id);

    IPublishedContent Single(string contentTypeAlias, Func<IPublishedContent, bool> predicate = null);
    T Single<T>(Func<T, bool> predicate = null);
}
