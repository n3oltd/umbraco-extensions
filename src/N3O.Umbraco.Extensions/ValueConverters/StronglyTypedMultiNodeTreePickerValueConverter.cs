using Humanizer;
using Microsoft.Extensions.Options;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;

using Umbraco.Extensions;
using UmbracoUdiEntityType = Umbraco.Cms.Core.Constants.UdiEntityType;

namespace N3O.Umbraco.ValueConverters;

public class StronglyTypedMultiNodeTreePickerValueConverter : MultiNodeTreePickerValueConverter {
    private readonly IContentTypeService _contentTypeService;
    private readonly IMediaTypeService _mediaTypeService;
    private readonly IMemberTypeService _memberTypeService;
    private readonly ModelsBuilderSettings _modelBuilderSettings;

    public StronglyTypedMultiNodeTreePickerValueConverter(IUmbracoContextAccessor umbracoContextAccessor,
                                                          IMemberService memberService,
                                                          IApiContentBuilder apiContentBuilder,
                                                          IApiMediaBuilder apiMediaBuilder,
                                                          IPublishedContentCache contentCache,
                                                          IPublishedMediaCache mediaCache,
                                                          IPublishedMemberCache memberCache,
                                                          IContentTypeService contentTypeService,
                                                          IMediaTypeService mediaTypeService,
                                                          IMemberTypeService memberTypeService,
                                                          IOptions<ModelsBuilderSettings> modelBuilderSettings)
        : base(umbracoContextAccessor, memberService, apiContentBuilder, apiMediaBuilder, contentCache, mediaCache, memberCache) {
        _contentTypeService = contentTypeService;
        _mediaTypeService = mediaTypeService;
        _memberTypeService = memberTypeService;
        _modelBuilderSettings = modelBuilderSettings.Value;
    }

    public override Type GetPropertyValueType(IPublishedPropertyType propertyType) {
        var config = propertyType.DataType.ConfigurationAs<MultiNodePickerConfiguration>();

        if (config.MaxNumber == 1) {
            return GetElementType(propertyType);
        } else {
            return GetCollectionType(propertyType);
        }
    }

    public override object ConvertIntermediateToObject(IPublishedElement owner,
                                                       IPublishedPropertyType propertyType,
                                                       PropertyCacheLevel cacheLevel,
                                                       object inter,
                                                       bool preview) {
        var value = base.ConvertIntermediateToObject(owner, propertyType, cacheLevel, inter, preview);

        if (value?.GetType().IsEnumerable() ?? false) {
            var elementType = GetElementType(propertyType);

            var valueListType = typeof(List<>).MakeGenericType(elementType);
            var valueList = (IList) Activator.CreateInstance(valueListType);

            foreach (var item in (IEnumerable) value) {
                valueList.Add(item);
            }

            return valueList;
        }

        return value;
    }

    private Type GetElementType(IPublishedPropertyType propertyType) {
        var config = propertyType.DataType.ConfigurationAs<MultiNodePickerConfiguration>();
        var contentType = config.Filter.HasValue()
                              ? GetPickerContentTypeName(config.TreeSource?.ObjectType, config.Filter)
                              : null;

        if (!contentType.HasValue() || contentType == nameof(IPublishedContent)) {
            return typeof(IPublishedContent);
        }

        return ModelsHelper.GetOrCreateModelsBuilderType(_modelBuilderSettings.ModelsNamespace, contentType);
    }

    private Type GetCollectionType(IPublishedPropertyType propertyType) {
        var elementType = GetElementType(propertyType);

        return typeof(IEnumerable<>).MakeGenericType(elementType);
    }

    private string GetContentTypeAlias(string objectType, string filterEntry) {
        var key = Guid.Parse(filterEntry);

        var alias = objectType switch {
            UmbracoUdiEntityType.Media => _mediaTypeService.Get(key)?.Alias,
            UmbracoUdiEntityType.Member => _memberTypeService.Get(key)?.Alias,
            _ => _contentTypeService.Get(key)?.Alias
        };

        return alias ?? filterEntry;
    }

    private string GetPickerContentTypeName(string objectType, string filter) {
        var contentTypes = filter.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(x => GetContentTypeAlias(objectType, x))
                                 .Select(x => x.Pascalize())
                                 .ToList();

        if (contentTypes.IsSingle()) {
            return contentTypes.First();
        }

        var commonInterfaces = new List<Type>();

        foreach (var contentType in contentTypes) {
            var type = ModelsHelper.GetOrCreateModelsBuilderType(_modelBuilderSettings.ModelsNamespace, contentType);

            var typeInterfaces = type.GetInterfaces()
                                     .Where(x => x.FullName.StartsWith(_modelBuilderSettings.ModelsNamespace))
                                     .ToList();

            if (contentType == contentTypes.First()) {
                commonInterfaces.AddRange(typeInterfaces);

                continue;
            }

            commonInterfaces.RemoveAll(x => !typeInterfaces.Contains(x));
        }

        if (commonInterfaces.Count != 1) {
            return nameof(IPublishedContent);
        }

        return commonInterfaces.Single().FullName.Substring(_modelBuilderSettings.ModelsNamespace.Length + 1);
    }
}
