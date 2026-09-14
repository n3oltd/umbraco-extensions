using N3O.Umbraco.Extensions;
using Newtonsoft.Json;
using System;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;

namespace N3O.Umbraco.Cells.DataTypes;

public class CellsValueConverter : PropertyValueConverterBase {
    public override bool IsConverter(IPublishedPropertyType propertyType) {
        return propertyType.EditorAlias.EqualsInvariant(CellsConstants.PropertyEditorAlias);
    }

    public override object ConvertSourceToIntermediate(IPublishedElement owner,
                                                       IPublishedPropertyType propertyType,
                                                       object source,
                                                       bool preview) {
        object[][] cells = null;
        var json = source.ToSourceValueJson();

        if (json.HasValue()) {
            cells = JsonConvert.DeserializeObject<object[][]>(json);
        }

        return cells;
    }

    public override Type GetPropertyValueType(IPublishedPropertyType propertyType) {
        return typeof(object[][]);
    }

    public override PropertyCacheLevel GetPropertyCacheLevel(IPublishedPropertyType propertyType) {
        return PropertyCacheLevel.Element;
    }
}
