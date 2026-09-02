using N3O.Umbraco.EditorJs.Extensions;
using N3O.Umbraco.EditorJs.Models;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Json;
using N3O.Umbraco.Markup;
using System;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Extensions;

namespace N3O.Umbraco.EditorJs.DataTypes;

public class EditorJsValueConverter : PropertyValueConverterBase {
    private readonly IJsonProvider _jsonProvider;
    private readonly IMarkupEngine _markupEngine;

    public EditorJsValueConverter(IJsonProvider jsonProvider, IMarkupEngine markupEngine) {
        _jsonProvider = jsonProvider;
        _markupEngine = markupEngine;
    }

    public override bool IsConverter(IPublishedPropertyType propertyType) {
        return propertyType.EditorAlias.Equals(EditorJsConstants.PropertyEditorAlias);
    }

    public override Type GetPropertyValueType(IPublishedPropertyType propertyType) {
        return typeof(EditorJsModel);
    }

    public override PropertyCacheLevel GetPropertyCacheLevel(IPublishedPropertyType propertyType) {
        return PropertyCacheLevel.Element;
    }

    public override object ConvertSourceToIntermediate(IPublishedElement owner,
                                                       IPublishedPropertyType propertyType,
                                                       object source,
                                                       bool preview) {
        return source.ToSourceValueJson();
    }

    public override object ConvertIntermediateToObject(IPublishedElement owner,
                                                       IPublishedPropertyType propertyType,
                                                       PropertyCacheLevel referenceCacheLevel,
                                                       object inter,
                                                       bool preview) {
        if (inter is string strValue) {
            if (strValue.DetectIsJson() && !strValue.DetectIsEmptyJson()) {
                return _jsonProvider.DeserializeObject<EditorJsModel>(strValue);
            } else {
                return strValue.MarkdownToEditorJsModel(_markupEngine);
            }
        }

        return null;
    }
}