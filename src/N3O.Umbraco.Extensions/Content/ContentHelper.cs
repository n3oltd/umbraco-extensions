using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Persistence.Querying;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace N3O.Umbraco.Content;

public class ContentHelper : IContentHelper {
    private readonly Lazy<IServiceProvider> _serviceProvider;
    private readonly Lazy<IContentService> _contentService;
    private readonly Lazy<IContentTypeService> _contentTypeService;
    private readonly Lazy<IContentLocator> _contentLocator;
    private readonly Lazy<IUmbracoContextAccessor> _umbracoContextAccessor;

    public ContentHelper(Lazy<IServiceProvider> serviceProvider,
                         Lazy<IContentService> contentService,
                         Lazy<IContentTypeService> contentTypeService,
                         Lazy<IContentLocator> contentLocator,
                         Lazy<IUmbracoContextAccessor> umbracoContextAccessor) {
        _serviceProvider = serviceProvider;
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _contentLocator = contentLocator;
        _umbracoContextAccessor = umbracoContextAccessor;
    }

    public IReadOnlyList<IContent> GetAllOfType(string contentTypeAlias) {
        var contentType = _contentTypeService.Value.Get(contentTypeAlias);

        if (contentType == null) {
            return [];
        }

        return GetAllPagedContent(contentType.Id, _contentService.Value.GetPagedOfType);
    }

    public IReadOnlyList<IContent> GetAncestors(IContent content) {
        var list = new List<IContent>();

        while (content.ParentId != -1) {
            content = _contentService.Value.GetById(content.ParentId);

            list.Add(content);
        }

        return list;
    }

    public IReadOnlyList<IContent> GetChildren(IContent content) {
        return GetAllPagedContent(content.Id, _contentService.Value.GetPagedChildren);
    }

    public ContentProperties GetContentProperties(IContent content, string culture = null) {
        var properties = content.Properties.Select(x => (x.PropertyType, x.GetValue(x.PropertyType.VariesByCulture() ? culture : null)));
        
        return GetContentProperties(content.Key,
                                    content.ParentId,
                                    content.Level,
                                    content.ContentType.Alias,
                                    properties);
    }
    
    public ContentProperties GetContentProperties(Guid contentId,
                                                  int? parentId,
                                                  int level,
                                                  string contentTypeAlias,
                                                  IEnumerable<(IPropertyType Type, object Value)> properties) {
        var contentProperties = new List<ContentProperty>();
        var elementsProperties = new List<ElementsProperty>();
        var contentType = _contentTypeService.Value.Get(contentTypeAlias);
        var compositionAliases = contentType.OrEmpty(x => x.CompositionAliases());

        foreach (var property in properties) {
            if (property.Type.IsBlockList() || property.Type.IsBlockGrid()) {
                var (blockListOrGrid, json) = GetJsonPropertyValue(property.Value);
                    
                var elements = GetContentPropertiesForBlockListOrGrid((JObject) blockListOrGrid);
                var elementsProperty = new ElementsProperty(contentType, property.Type, elements, json);
                
                elementsProperties.Add(elementsProperty);
            } else if (property.Type.IsNestedContent()) {
                var (nestedContents, json) = GetJsonPropertyValue(property.Value);
                    
                var elements = GetContentPropertiesForNestedContent(nestedContents);
                var elementsProperty = new ElementsProperty(contentType, property.Type, elements, json);
                
                elementsProperties.Add(elementsProperty);
            } else if (property.Type.IsPerplexBlocks()) {
                var (blockContent, json) = GetJsonPropertyValue(property.Value);

                var elements = GetContentPropertiesForBlockContent(blockContent);
                var elementsProperty = new ElementsProperty(contentType, property.Type, elements, json);
                
                elementsProperties.Add(elementsProperty);
            } else {
                contentProperties.Add(new ContentProperty(contentType, property.Type, property.Value));
            }
        }

        return new ContentProperties(contentId,
                                     parentId,
                                     level,
                                     contentTypeAlias,
                                     compositionAliases,
                                     contentProperties,
                                     elementsProperties);
    }
    
    public TProperty GetConvertedValue<TConverter, TProperty>(string contentTypeAlias,
                                                              string propertyTypeAlias,
                                                              object propertyValue)
        where TConverter : class, IPropertyValueConverter {
        return GetConvertedValue<TProperty>(typeof(TConverter), contentTypeAlias, propertyTypeAlias, propertyValue);
    }

    public TProperty GetConvertedValue<TProperty>(Type converterType,
                                                  string contentTypeAlias,
                                                  string propertyTypeAlias,
                                                  object propertyValue) {
        var converter = (IPropertyValueConverter) _serviceProvider.Value.GetRequiredService(converterType);
        var publishedContentType = _umbracoContextAccessor.Value.GetContentCache().GetContentType(contentTypeAlias);
        var publishedPropertyType = publishedContentType?.GetPropertyType(propertyTypeAlias);
        
        var source = propertyValue;

        if (source == null || source.ToString() == "null" || publishedPropertyType == null) {
            return default;
        }

        var intermediate = converter.ConvertSourceToIntermediate(null, publishedPropertyType, source, false);
        var result = (TProperty) converter.ConvertIntermediateToObject(null,
                                                                       publishedPropertyType,
                                                                       PropertyCacheLevel.None,
                                                                       intermediate,
                                                                       false);

        return result;
    }
    
    public IReadOnlyList<IContent> GetDescendants(IContent content, IQuery<IContent> query = null) {
        return GetAllPagedContent(content.Id, _contentService.Value.GetPagedDescendants, query);
    }

    public IReadOnlyList<T> GetPublishedAncestors<T>(IContent content) where T : IPublishedContent {
        return GetAncestors(content).Select(x => _contentLocator.Value.ById<T>(x.Key)).ToList();
    }
    
    public IReadOnlyList<T> GetPublishedChildren<T>(IContent content) where T : IPublishedContent {
        return GetChildren(content).Select(x => _contentLocator.Value.ById<T>(x.Key)).ToList();
    }
    
    public IReadOnlyList<T> GetPublishedDescendants<T>(IContent content) where T : IPublishedContent {
        return GetDescendants(content).Select(x => _contentLocator.Value.ById<T>(x.Key)).ToList();
    }
    
    private IReadOnlyList<IContent> GetAllPagedContent(int id,
                                                       GetPagedContent getPagedContent,
                                                       IQuery<IContent> query = null) {
        var descendants = new List<IContent>();

        var pageIndex = 0;
        var pageSize = 100;

        while (true) {
            descendants.AddRange(getPagedContent(id, pageIndex, pageSize, out var totalRecords, query));

            if ((pageIndex + 1) * pageSize >= totalRecords) {
                break;
            }

            pageIndex++;
        }

        return descendants;
    }

    private IReadOnlyList<ContentProperties> GetContentPropertiesForBlockContent(JToken blockContent) {
        var contentProperties = new List<ContentProperties>();
        
        if (blockContent == null) {
            return contentProperties;
        }
        
        foreach (var block in blockContent["blocks"]) {
            var content = block["content"];

            if (content is JArray jArray) {
                contentProperties.AddRange(GetContentPropertiesForNestedContent(jArray.Single()));
            } else {
                contentProperties.AddRange(GetContentPropertiesForNestedContent(content));
            }
        }

        return contentProperties;
    }
    
    private IReadOnlyList<ContentProperties> GetContentPropertiesForBlockListOrGrid(JObject blockListOrGrid) {
        var contentProperties = new List<ContentProperties>();
        
        if (blockListOrGrid?.TryGetValue("contentData", StringComparison.InvariantCultureIgnoreCase, out var contentData) == true) {
            foreach (var block in contentData.OrEmpty()) {
                if (block is JArray jArray) {
                    foreach (JObject jObject in jArray) {
                        contentProperties.Add(GetContentPropertiesForBlockListOrGridElement(jObject));
                    }
                }
            }
        }

        return contentProperties;
    }
    
    private ContentProperties GetContentPropertiesForBlockListOrGridElement(JObject element) {
        var id = UdiParser.Parse((string) element["udi"]).ToId().GetValueOrThrow();
        var contentTypeKey = Guid.Parse((string) element["contentTypeKey"]);
        var contentType = _contentTypeService.Value.Get(contentTypeKey);

        var properties = new List<(IPropertyType, object)>();
            
        foreach (var propertyGroup in contentType.PropertyGroups) {
            foreach (var propertyType in propertyGroup.PropertyTypes) {
                var propertyValue = element[propertyType.Alias];

                properties.Add((propertyType, propertyValue?.ConvertToObject()));
            }
        }
            
        return GetContentProperties(id, null, -1, contentType.Alias, properties);
    }

    private IReadOnlyList<ContentProperties> GetContentPropertiesForNestedContent(JToken nestedContent) {
        var contentProperties = new List<ContentProperties>();

        if (nestedContent == null) {
            return contentProperties;
        } else if (nestedContent is JValue jValue) {
            if (jValue.Value is string json && json.HasValue()) {
                return GetContentPropertiesForNestedContent((JToken) JsonConvert.DeserializeObject(json));
            }
        } else if (nestedContent is JArray jArray) {
            foreach (var element in jArray.OrEmpty()) {
                contentProperties.Add(GetContentPropertiesForNestedContentElement((JObject) element));
            }
        } else {
            contentProperties.Add(GetContentPropertiesForNestedContentElement((JObject) nestedContent));
        }

        return contentProperties;
    }

    private ContentProperties GetContentPropertiesForNestedContentElement(JObject element) {
        var id = Guid.Parse((string) element["key"]);
        var contentTypeAlias = (string) element["ncContentTypeAlias"];
        var contentType = _contentTypeService.Value.Get(contentTypeAlias);

        var properties = new List<(IPropertyType, object)>();
            
        foreach (var propertyType in contentType.CompositionPropertyTypes) {
            var propertyValue = element[propertyType.Alias];

            properties.Add((propertyType, propertyValue?.ConvertToObject()));
        }
            
        return GetContentProperties(id, null, -1, contentTypeAlias, properties);
    }
    
    private (JToken, string) GetJsonPropertyValue(object propertyValue) {
        if (propertyValue == null) {
            return (null, null);
        }
        
        JToken obj;

        if (propertyValue is string str) {
            obj = (JToken) JsonConvert.DeserializeObject(str);
        } else if (propertyValue is JToken jToken) {
            obj = jToken;
        } else {
            obj = JToken.FromObject(propertyValue);
        }

        return (obj, JsonConvert.SerializeObject(obj));
    }

    private delegate IEnumerable<IContent> GetPagedContent(int id,
                                                           long pageIndex,
                                                           int pageSize,
                                                           out long totalRecords,
                                                           IQuery<IContent> filter = null,
                                                           Ordering ordering = null);
}
