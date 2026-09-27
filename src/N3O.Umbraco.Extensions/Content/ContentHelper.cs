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
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace N3O.Umbraco.Content;

public class ContentHelper : IContentHelper {
    private readonly Lazy<IServiceProvider> _serviceProvider;
    private readonly Lazy<IContentService> _contentService;
    private readonly Lazy<IContentTypeService> _contentTypeService;
    private readonly Lazy<IContentLocator> _contentLocator;
    private readonly Lazy<IPublishedContentTypeCache> _publishedContentTypeCache;

    public ContentHelper(Lazy<IServiceProvider> serviceProvider,
                         Lazy<IContentService> contentService,
                         Lazy<IContentTypeService> contentTypeService,
                         Lazy<IContentLocator> contentLocator,
                         Lazy<IPublishedContentTypeCache> publishedContentTypeCache) {
        _serviceProvider = serviceProvider;
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _contentLocator = contentLocator;
        _publishedContentTypeCache = publishedContentTypeCache;
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
        return GetAllPagedContent(content, GetPagedChildren);
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
                    
                var contentElements = GetContentPropertiesForBlockListOrGrid((JObject) blockListOrGrid, "contentData");
                var settingsElements = GetContentPropertiesForBlockListOrGrid((JObject) blockListOrGrid, "settingsData");

                var elementsProperty = new ElementsProperty(contentType,
                                                            property.Type,
                                                            contentElements,
                                                            settingsElements,
                                                            json);

                elementsProperties.Add(elementsProperty);
            } else if (property.Type.IsPerplexBlocks()) {
                var (blockContent, json) = GetJsonPropertyValue(property.Value);

                var elements = GetContentPropertiesForBlockContent(blockContent);

                var elementsProperty = new ElementsProperty(contentType, property.Type, elements, [], json);
                
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
                                                              object propertyValue,
                                                              IPublishedElement owner = null)
        where TConverter : class, IPropertyValueConverter {
        return GetConvertedValue<TProperty>(typeof(TConverter),
                                            contentTypeAlias,
                                            propertyTypeAlias,
                                            propertyValue,
                                            owner);
    }

    // The owner is the element the property belongs to. Block editors need it from v15: once a block value
    // carries an expose entry, BlockEditorVarianceHandler reads owner.ContentType.Variations to align block
    // variance, so converting a block value without one throws.
    public TProperty GetConvertedValue<TProperty>(Type converterType,
                                                  string contentTypeAlias,
                                                  string propertyTypeAlias,
                                                  object propertyValue,
                                                  IPublishedElement owner = null) {
        var converter = (IPropertyValueConverter) _serviceProvider.Value.GetRequiredService(converterType);
        var publishedContentType = _publishedContentTypeCache.Value.Get(_contentTypeService.Value, contentTypeAlias);
        var publishedPropertyType = publishedContentType?.GetPropertyType(propertyTypeAlias);
        
        var source = propertyValue;

        if (source == null || source.ToString() == "null" || publishedPropertyType == null) {
            return default;
        }

        var intermediate = converter.ConvertSourceToIntermediate(owner, publishedPropertyType, source, false);
        var result = (TProperty) converter.ConvertIntermediateToObject(owner,
                                                                       publishedPropertyType,
                                                                       PropertyCacheLevel.None,
                                                                       intermediate,
                                                                       false);

        return result;
    }
    
    public IReadOnlyList<IContent> GetDescendants(IContent content, IQuery<IContent> query = null) {
        return GetAllPagedContent(content, _contentService.Value.GetPagedDescendants, query);
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

    private IReadOnlyList<IContent> GetAllPagedContent(IContent content,
                                                       GetPagedContent getPagedContent,
                                                       IQuery<IContent> query = null) {
        var descendants = new List<IContent>();

        var pageIndex = 0;
        var pageSize = 100;

        while (true) {
            descendants.AddRange(getPagedContent(content.Id, pageIndex, pageSize, out var totalRecords, query));

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
        
        if (blockContent["header"] is JObject header) {
            contentProperties.AddRange(GetContentPropertiesForPerplexBlock(header));
        }

        if (blockContent["blocks"] is JArray blocks) {
            foreach (var block in blocks) {
                contentProperties.AddRange(GetContentPropertiesForPerplexBlock(block));
            }
        }

        return contentProperties;
    }

    // Block content is stored either as a Block Editor element carrying a contentTypeKey, or as a
    // NestedContent array.
    private IReadOnlyList<ContentProperties> GetContentPropertiesForPerplexBlock(JToken block) {
        var content = block?["content"];

        if (content == null) {
            return [];
        } else if (content is JObject element && element["contentTypeKey"] != null) {
            var elementProperties = GetContentPropertiesForBlockListOrGridElement(element);

            return elementProperties == null ? [] : [elementProperties];
        } else {
            return GetContentPropertiesForNestedContent(content);
        }
    }
    
    private IReadOnlyList<ContentProperties> GetContentPropertiesForBlockListOrGrid(JObject blockListOrGrid,
                                                                                    string dataPropertyName) {
        var contentProperties = new List<ContentProperties>();

        if (blockListOrGrid == null) {
            return contentProperties;
        }

        if (blockListOrGrid.TryGetValue(dataPropertyName, StringComparison.InvariantCultureIgnoreCase, out var data)) {
            foreach (var block in data.OrEmpty()) {
                if (block is JObject jObject) {
                    var elementProperties = GetContentPropertiesForBlockListOrGridElement(jObject);

                    if (elementProperties != null) {
                        contentProperties.Add(elementProperties);
                    }
                }
            }
        }

        return contentProperties;
    }
    
    private ContentProperties GetContentPropertiesForBlockListOrGridElement(JObject element) {
        if (!TryGetBlockElementKey(element, out var id)) {
            return null;
        }

        if (!Guid.TryParse((string) element["contentTypeKey"], out var contentTypeKey)) {
            return null;
        }

        var contentType = _contentTypeService.Value.Get(contentTypeKey);

        if (contentType == null) {
            return null;
        }

        var valuesByAlias = GetBlockElementValuesByAlias(element);

        var properties = new List<(IPropertyType, object)>();

        foreach (var propertyType in contentType.CompositionPropertyTypes) {
            valuesByAlias.TryGetValue(propertyType.Alias, out var propertyValue);

            properties.Add((propertyType, propertyValue?.ConvertToObject()));
        }

        return GetContentProperties(id, null, -1, contentType.Alias, properties);
    }

    private static bool TryGetBlockElementKey(JObject element, out Guid key) {
        var keyValue = (string) element["key"];

        if (keyValue.HasValue() && Guid.TryParse(keyValue, out key)) {
            return true;
        }

        var udi = (string) element["udi"];

        if (udi.HasValue() && UdiParser.TryParse(udi, out var parsedUdi) && parsedUdi is GuidUdi guidUdi) {
            key = guidUdi.Guid;

            return true;
        }

        key = Guid.Empty;

        return false;
    }

    private static IReadOnlyDictionary<string, JToken> GetBlockElementValuesByAlias(JObject element) {
        var valuesByAlias = new Dictionary<string, JToken>(StringComparer.InvariantCultureIgnoreCase);

        if (element["values"] is JArray values) {
            foreach (var value in values.OfType<JObject>()) {
                var alias = (string) value["alias"];

                if (alias.HasValue()) {
                    valuesByAlias[alias] = value["value"];
                }
            }
        }

        return valuesByAlias;
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
                AddNestedContentElement(contentProperties, element);
            }
        } else {
            AddNestedContentElement(contentProperties, nestedContent);
        }

        return contentProperties;
    }

    private void AddNestedContentElement(List<ContentProperties> contentProperties, JToken element) {
        if (element is not JObject jObject) {
            return;
        }

        var elementProperties = GetContentPropertiesForNestedContentElement(jObject);

        if (elementProperties != null) {
            contentProperties.Add(elementProperties);
        }
    }

    private ContentProperties GetContentPropertiesForNestedContentElement(JObject element) {
        if (!Guid.TryParse((string) element["key"], out var id)) {
            return null;
        }

        var contentTypeAlias = (string) element["ncContentTypeAlias"];

        if (!contentTypeAlias.HasValue()) {
            return null;
        }

        var contentType = _contentTypeService.Value.Get(contentTypeAlias);

        if (contentType == null) {
            return null;
        }

        var properties = new List<(IPropertyType, object)>();
            
        foreach (var propertyType in contentType.CompositionPropertyTypes) {
            element.TryGetValue(propertyType.Alias, StringComparison.InvariantCultureIgnoreCase, out var propertyValue);

            properties.Add((propertyType, propertyValue?.ConvertToObject()));
        }

        return GetContentProperties(id, null, -1, contentTypeAlias, properties);
    }
    
    private (JToken, string) GetJsonPropertyValue(object propertyValue) {
        if (propertyValue == null) {
            return (null, null);
        }
        
        if (propertyValue is string str) {
            return ((JToken) JsonConvert.DeserializeObject(str), str);
        }

        var obj = propertyValue is JToken jToken ? jToken : JToken.FromObject(propertyValue);

        return (obj, JsonConvert.SerializeObject(obj));
    }
    
    private IEnumerable<IContent> GetPagedChildren(int id,
                                                   long pageIndex,
                                                   int pageSize,
                                                   out long totalRecords,
                                                   IQuery<IContent> filter = null,
                                                   Ordering ordering = null) {
        return _contentService.Value.GetPagedChildren(id,
                                                      pageIndex,
                                                      pageSize,
                                                      out totalRecords,
                                                      propertyAliases: null,
                                                      filter: filter,
                                                      ordering: ordering,
                                                      loadTemplates: true);
    }

    private delegate IEnumerable<IContent> GetPagedContent(int id,
                                                           long pageIndex,
                                                           int pageSize,
                                                           out long totalRecords,
                                                           IQuery<IContent> filter = null,
                                                           Ordering ordering = null);
}
