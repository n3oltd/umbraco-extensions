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
    private readonly Lazy<ILanguageService> _languageService;

    public ContentHelper(Lazy<IServiceProvider> serviceProvider,
                         Lazy<IContentService> contentService,
                         Lazy<IContentTypeService> contentTypeService,
                         Lazy<IContentLocator> contentLocator,
                         Lazy<IPublishedContentTypeCache> publishedContentTypeCache,
                         Lazy<ILanguageService> languageService) {
        _serviceProvider = serviceProvider;
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _contentLocator = contentLocator;
        _publishedContentTypeCache = publishedContentTypeCache;
        _languageService = languageService;
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
        var ownerVariesByCulture = contentType != null && contentType.VariesByCulture();

        foreach (var property in properties) {
            if (property.Type.IsBlockList() || property.Type.IsBlockGrid()) {
                var (blockListOrGrid, json) = GetJsonPropertyValue(property.Value);

                var contentElements = GetContentPropertiesForBlockListOrGrid((JObject) blockListOrGrid,
                                                                             "contentData",
                                                                             ownerVariesByCulture,
                                                                             culture);
                var settingsElements = GetContentPropertiesForBlockListOrGrid((JObject) blockListOrGrid,
                                                                              "settingsData",
                                                                              ownerVariesByCulture,
                                                                              culture);

                var elementsProperty = new ElementsProperty(contentType,
                                                            property.Type,
                                                            contentElements,
                                                            settingsElements,
                                                            json);

                elementsProperties.Add(elementsProperty);
            } else if (property.Type.IsPerplexBlocks()) {
                var (blockContent, json) = GetJsonPropertyValue(property.Value);

                var elements = GetContentPropertiesForBlockContent(blockContent, ownerVariesByCulture, culture);

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
                                                              IPublishedElement owner = null,
                                                              bool preview = false)
        where TConverter : class, IPropertyValueConverter {
        return GetConvertedValue<TProperty>(typeof(TConverter),
                                            contentTypeAlias,
                                            propertyTypeAlias,
                                            propertyValue,
                                            owner,
                                            preview);
    }

    public TProperty GetConvertedValue<TProperty>(Type converterType,
                                                  string contentTypeAlias,
                                                  string propertyTypeAlias,
                                                  object propertyValue,
                                                  IPublishedElement owner = null,
                                                  bool preview = false) {
        var converter = (IPropertyValueConverter) _serviceProvider.Value.GetRequiredService(converterType);
        var publishedContentType = _publishedContentTypeCache.Value.Get(_contentTypeService.Value, contentTypeAlias);
        var publishedPropertyType = publishedContentType?.GetPropertyType(propertyTypeAlias);
        
        var source = propertyValue;

        if (source == null || source.ToString() == "null" || publishedPropertyType == null) {
            return default;
        }

        owner ??= new PublishedElement(publishedContentType,
                                       Guid.NewGuid(),
                                       new Dictionary<string, object>(),
                                       preview,
                                       new VariationContext());

        var intermediate = converter.ConvertSourceToIntermediate(owner, publishedPropertyType, source, preview);
        var result = (TProperty) converter.ConvertIntermediateToObject(owner,
                                                                       publishedPropertyType,
                                                                       PropertyCacheLevel.None,
                                                                       intermediate,
                                                                       preview);

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
                                                                                 bool ownerVariesByCulture,
                                                                                 string culture) {
        var contentProperties = new List<ContentProperties>();

        if (blockContent == null) {
            return contentProperties;
        }

        if (blockContent["header"] is JObject header) {
            contentProperties.AddRange(GetContentPropertiesForPerplexBlock(header, ownerVariesByCulture, culture));
        }

        if (blockContent["blocks"] is JArray blocks) {
            foreach (var block in blocks) {
                contentProperties.AddRange(GetContentPropertiesForPerplexBlock(block, ownerVariesByCulture, culture));
            }
        }

        return contentProperties;
    }

    // Block content is stored either as a Block Editor element carrying a contentTypeKey, or as a
    // NestedContent array.
    private IReadOnlyList<ContentProperties> GetContentPropertiesForPerplexBlock(JToken block,
                                                                                 bool ownerVariesByCulture,
                                                                                 string culture) {
        var content = block?["content"];

        if (content == null) {
            return [];
        } else if (content is JObject element && element["contentTypeKey"] != null) {
            var elementProperties = GetContentPropertiesForBlockListOrGridElement(element,
                                                                                  ownerVariesByCulture,
                                                                                  null,
                                                                                  culture);

            return elementProperties == null ? [] : [elementProperties];
        } else {
            return GetContentPropertiesForNestedContent(content, culture);
        }
    }
    
    private IReadOnlyList<ContentProperties> GetContentPropertiesForBlockListOrGrid(JObject blockListOrGrid,
                                                                                    string dataPropertyName,
                                                                                    bool ownerVariesByCulture,
                                                                                    string culture) {
        var contentProperties = new List<ContentProperties>();

        if (blockListOrGrid == null) {
            return contentProperties;
        }

        // Umbraco renders only the content blocks exposed in the requested culture. Settings blocks are never
        // exposed; they follow their content block.
        JArray expose = null;

        if (dataPropertyName.EqualsInvariant("contentData")) {
            blockListOrGrid.TryGetValue("expose", StringComparison.InvariantCultureIgnoreCase, out var exposeToken);

            expose = exposeToken as JArray ?? [];
        }

        if (blockListOrGrid.TryGetValue(dataPropertyName, StringComparison.InvariantCultureIgnoreCase, out var data)) {
            foreach (var block in data.OrEmpty()) {
                if (block is JObject jObject) {
                    var elementProperties = GetContentPropertiesForBlockListOrGridElement(jObject,
                                                                                          ownerVariesByCulture,
                                                                                          expose,
                                                                                          culture);

                    if (elementProperties != null) {
                        contentProperties.Add(elementProperties);
                    }
                }
            }
        }

        return contentProperties;
    }
    
    private ContentProperties GetContentPropertiesForBlockListOrGridElement(JObject element,
                                                                            bool ownerVariesByCulture,
                                                                            JArray expose,
                                                                            string culture) {
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

        var blockVariesByCulture = ownerVariesByCulture && contentType.VariesByCulture();

        if (expose != null && !IsExposed(expose, id, blockVariesByCulture, culture)) {
            return null;
        }

        var valuesByAlias = GetBlockElementValuesByAlias(element, contentType, ownerVariesByCulture, culture);

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

    private bool IsExposed(JArray expose, Guid key, bool blockVariesByCulture, string culture) {
        culture = culture.NullOrWhiteSpaceAsNull();

        // The structured export requests no culture, and keeps every block rather than dropping the varying ones.
        if (blockVariesByCulture && culture == null) {
            return true;
        }

        var variations = expose.OfType<JObject>()
                               .Where(x => Guid.TryParse((string) x["contentKey"], out var contentKey) &&
                                           contentKey == key)
                               .Select(x => (Culture: ((string) x["culture"]).NullOrWhiteSpaceAsNull(),
                                             Segment: ((string) x["segment"]).NullOrWhiteSpaceAsNull()))
                               .ToList();

        // Umbraco first aligns the stored variations with the current variation settings: an invariant set is
        // read as the default culture, and a varying set as invariant through its default-culture entries.
        if (blockVariesByCulture && variations.All(x => x.Culture == null)) {
            var defaultCulture = _languageService.Value.GetDefaultCultureCode();

            variations = variations.Select(x => (defaultCulture, x.Segment)).ToList();
        } else if (!blockVariesByCulture && variations.All(x => x.Culture != null)) {
            var defaultCulture = _languageService.Value.GetDefaultCultureCode();

            variations = variations.Where(x => x.Culture.EqualsInvariant(defaultCulture))
                                   .Select(x => ((string) null, x.Segment))
                                   .ToList();
        }

        var expectedCulture = blockVariesByCulture ? culture : null;

        return variations.Any(x => x.Culture.EqualsInvariant(expectedCulture) && x.Segment == null);
    }

    private IReadOnlyDictionary<string, JToken> GetBlockElementValuesByAlias(JObject element,
                                                                             IContentType elementType,
                                                                             bool ownerVariesByCulture,
                                                                             string culture) {
        var valuesByAlias = new Dictionary<string, JToken>(StringComparer.InvariantCultureIgnoreCase);

        if (element["values"] is not JArray values) {
            return valuesByAlias;
        }

        var elementVariesByCulture = elementType.VariesByCulture();
        var defaultCulture = new Lazy<string>(() => _languageService.Value.GetDefaultCultureCode());

        foreach (var value in values.OfType<JObject>()) {
            var alias = (string) value["alias"];
            var propertyType = elementType.CompositionPropertyTypes
                                          .FirstOrDefault(x => x.Alias.EqualsInvariant(alias));

            if (propertyType == null) {
                continue;
            }

            var propertyVariesByCulture = ownerVariesByCulture && propertyType.VariesByCulture();
            var valueCulture = ((string) value["culture"]).NullOrWhiteSpaceAsNull();
            var valueSegment = ((string) value["segment"]).NullOrWhiteSpaceAsNull();

            // Umbraco first aligns a value stored under different variation settings: a varying property reads an
            // invariant value as the default culture, and an invariant property reads only the default culture.
            if (propertyVariesByCulture != (valueCulture != null)) {
                if (propertyVariesByCulture) {
                    valueCulture = defaultCulture.Value;
                } else if (valueCulture.EqualsInvariant(defaultCulture.Value)) {
                    valueCulture = null;
                } else {
                    continue;
                }
            }

            var expectedCulture = propertyVariesByCulture && elementVariesByCulture
                                      ? culture.NullOrWhiteSpaceAsNull()
                                      : null;

            // No fallback to another culture, and no segment is ever requested here.
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
