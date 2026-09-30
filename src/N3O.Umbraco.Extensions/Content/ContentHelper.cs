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
        return GetAllPagedContent(content.Id, GetPagedChildren);
    }

    public ContentProperties GetContentProperties(IContent content, string culture = null) {
        var properties = content.Properties.Select(x => (x.PropertyType, x.GetValue(x.PropertyType.VariesByCulture() ? culture : null)));
        
        return GetContentProperties(content.Key,
                                    content.ParentId,
                                    content.Level,
                                    content.ContentType.Alias,
                                    properties,
                                    culture);
    }
    
    public ContentProperties GetContentProperties(Guid contentId,
                                                  int? parentId,
                                                  int level,
                                                  string contentTypeAlias,
                                                  IEnumerable<(IPropertyType Type, object Value)> properties) {
        return GetContentProperties(contentId, parentId, level, contentTypeAlias, properties, null);
    }
    
    private ContentProperties GetContentProperties(Guid contentId,
                                                   int? parentId,
                                                   int level,
                                                   string contentTypeAlias,
                                                   IEnumerable<(IPropertyType Type, object Value)> properties,
                                                   string culture) {
        var contentProperties = new List<ContentProperty>();
        var elementsProperties = new List<ElementsProperty>();
        var contentType = _contentTypeService.Value.Get(contentTypeAlias);
        var compositionAliases = contentType.OrEmpty(x => x.CompositionAliases());
        var elementsCulture = contentType != null && contentType.VariesByCulture() ? culture : null;

        foreach (var property in properties) {
            if (property.Type.IsBlockList() || property.Type.IsBlockGrid()) {
                var (blockListOrGrid, json) = GetJsonPropertyValue(property.Value);
                    
                var contentElements = GetContentPropertiesForBlockListOrGrid((JObject) blockListOrGrid,
                                                                             "contentData",
                                                                             elementsCulture);
                var settingsElements = GetContentPropertiesForBlockListOrGrid((JObject) blockListOrGrid,
                                                                              "settingsData",
                                                                              elementsCulture);

                var elementsProperty = new ElementsProperty(contentType,
                                                            property.Type,
                                                            contentElements,
                                                            settingsElements,
                                                            json);

                elementsProperties.Add(elementsProperty);
            } else if (property.Type.IsPerplexBlocks()) {
                var (blockContent, json) = GetJsonPropertyValue(property.Value);

                var elements = GetContentPropertiesForBlockContent(blockContent, elementsCulture);

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

        // Block editor converters dereference owner.ContentType for every block, and the content type is all they
        // read from it.
        owner ??= new PublishedElement(publishedContentType,
                                       Guid.NewGuid(),
                                       new Dictionary<string, object>(),
                                       false,
                                       new VariationContext());

        var intermediate = converter.ConvertSourceToIntermediate(owner, publishedPropertyType, source, false);
        var result = (TProperty) converter.ConvertIntermediateToObject(owner,
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

    private IReadOnlyList<ContentProperties> GetContentPropertiesForBlockContent(JToken blockContent,
                                                                                 string culture) {
        var contentProperties = new List<ContentProperties>();
        
        if (blockContent == null) {
            return contentProperties;
        }
        
        if (blockContent["header"] is JObject header) {
            contentProperties.AddRange(GetContentPropertiesForPerplexBlock(header, culture));
        }

        if (blockContent["blocks"] is JArray blocks) {
            foreach (var block in blocks) {
                contentProperties.AddRange(GetContentPropertiesForPerplexBlock(block, culture));
            }
        }

        return contentProperties;
    }

    // Block content is stored either as a Block Editor element carrying a contentTypeKey, or as a
    // NestedContent array.
    private IReadOnlyList<ContentProperties> GetContentPropertiesForPerplexBlock(JToken block, string culture) {
        var content = block?["content"];

        if (content == null) {
            return [];
        } else if (content is JObject element && element["contentTypeKey"] != null) {
            var elementProperties = GetContentPropertiesForBlockListOrGridElement(element, culture);

            return elementProperties == null ? [] : [elementProperties];
        } else {
            return GetContentPropertiesForNestedContent(content, culture);
        }
    }
    
    private IReadOnlyList<ContentProperties> GetContentPropertiesForBlockListOrGrid(JObject blockListOrGrid,
                                                                                    string dataPropertyName,
                                                                                    string culture) {
        var contentProperties = new List<ContentProperties>();

        if (blockListOrGrid == null) {
            return contentProperties;
        }

        if (blockListOrGrid.TryGetValue(dataPropertyName, StringComparison.InvariantCultureIgnoreCase, out var data)) {
            foreach (var block in data.OrEmpty()) {
                if (block is JObject jObject) {
                    var elementProperties = GetContentPropertiesForBlockListOrGridElement(jObject, culture);

                    if (elementProperties != null) {
                        contentProperties.Add(elementProperties);
                    }
                }
            }
        }

        return contentProperties;
    }
    
    private ContentProperties GetContentPropertiesForBlockListOrGridElement(JObject element, string culture) {
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

        var valuesByAlias = GetBlockElementValuesByAlias(element, contentType, culture);

        var properties = new List<(IPropertyType, object)>();

        foreach (var propertyType in contentType.CompositionPropertyTypes) {
            valuesByAlias.TryGetValue(propertyType.Alias, out var propertyValue);

            properties.Add((propertyType, propertyValue?.ConvertToObject()));
        }

        return GetContentProperties(id, null, -1, contentType.Alias, properties, culture);
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

    private static IReadOnlyDictionary<string, JToken> GetBlockElementValuesByAlias(JObject element,
                                                                                    IContentType elementType,
                                                                                    string culture) {
        var valuesByAlias = new Dictionary<string, JToken>(StringComparer.InvariantCultureIgnoreCase);

        if (element["values"] is not JArray values) {
            return valuesByAlias;
        }

        var elementVariesByCulture = elementType.VariesByCulture();

        foreach (var value in values.OfType<JObject>()) {
            var alias = (string) value["alias"];

            if (!alias.HasValue()) {
                continue;
            }

            var propertyType = elementType.CompositionPropertyTypes
                                          .FirstOrDefault(x => x.Alias.EqualsInvariant(alias));

            var expectedCulture = elementVariesByCulture && propertyType != null && propertyType.VariesByCulture()
                                      ? culture.NullOrWhiteSpaceAsNull()
                                      : null;

            var valueCulture = ((string) value["culture"]).NullOrWhiteSpaceAsNull();
            var valueSegment = ((string) value["segment"]).NullOrWhiteSpaceAsNull();

            // Culture and segment must match exactly with no fallback to another culture, as Umbraco's own
            // BlockEditorConverter does, and no segment is ever requested here.
            if (valueCulture.EqualsInvariant(expectedCulture) && valueSegment == null) {
                valuesByAlias[alias] = value["value"];
            }
        }

        return valuesByAlias;
    }

    private IReadOnlyList<ContentProperties> GetContentPropertiesForNestedContent(JToken nestedContent,
                                                                                  string culture) {
        var contentProperties = new List<ContentProperties>();

        if (nestedContent == null) {
            return contentProperties;
        } else if (nestedContent is JValue jValue) {
            if (jValue.Value is string json && json.HasValue()) {
                return GetContentPropertiesForNestedContent((JToken) JsonConvert.DeserializeObject(json), culture);
            }
        } else if (nestedContent is JArray jArray) {
            foreach (var element in jArray.OrEmpty()) {
                AddNestedContentElement(contentProperties, element, culture);
            }
        } else {
            AddNestedContentElement(contentProperties, nestedContent, culture);
        }

        return contentProperties;
    }

    private void AddNestedContentElement(List<ContentProperties> contentProperties,
                                         JToken element,
                                         string culture) {
        if (element is not JObject jObject) {
            return;
        }

        var elementProperties = GetContentPropertiesForNestedContentElement(jObject, culture);

        if (elementProperties != null) {
            contentProperties.Add(elementProperties);
        }
    }

    private ContentProperties GetContentPropertiesForNestedContentElement(JObject element, string culture) {
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

        return GetContentProperties(id, null, -1, contentTypeAlias, properties, culture);
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
