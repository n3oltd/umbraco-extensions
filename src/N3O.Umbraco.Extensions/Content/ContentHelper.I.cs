using System;
using System.Collections.Generic;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Persistence.Querying;
using Umbraco.Cms.Core.PropertyEditors;

namespace N3O.Umbraco.Content;

public interface IContentHelper {
    IReadOnlyList<IContent> GetAncestors(IContent content);
    
    IReadOnlyList<IContent> GetChildren(IContent content);

    ContentProperties GetContentProperties(IContent content, string culture = null);

    ContentProperties GetContentProperties(Guid contentId,
                                           int? parentId,
                                           int level,
                                           string contentTypeAlias,
                                           IEnumerable<(IPropertyType Type, object Value)> properties);

    TProperty GetConvertedValue<TProperty>(Type converterType,
                                           string contentTypeAlias,
                                           string propertyTypeAlias,
                                           object propertyValue,
                                           IPublishedElement owner = null);
    
    TProperty GetConvertedValue<TConverter, TProperty>(string contentTypeAlias,
                                                       string propertyTypeAlias,
                                                       object propertyValue,
                                                       IPublishedElement owner = null)
        where TConverter : class, IPropertyValueConverter;
    
    IReadOnlyList<IContent> GetDescendants(IContent content, IQuery<IContent> query = null);

    IReadOnlyList<T> GetPublishedAncestors<T>(IContent content) where T : IPublishedContent;
    
    IReadOnlyList<T> GetPublishedChildren<T>(IContent content) where T : IPublishedContent;
    
    IReadOnlyList<T> GetPublishedDescendants<T>(IContent content) where T : IPublishedContent;
}
