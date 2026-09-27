using N3O.Umbraco.Data.Models;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.ValueConverters;
using Newtonsoft.Json;
using System;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;

namespace N3O.Umbraco.Data.DataTypes;

public class ImportNoticesViewerValueConverter : PropertyValueConverter {
    public override bool IsConverter(IPublishedPropertyType propertyType) {
        return propertyType.EditorAlias.EqualsInvariant(DataConstants.PropertyEditorAliases.ImportNoticesViewer);
    }

    public override object ConvertSourceToIntermediate(IPublishedElement owner,
                                                       IPublishedPropertyType propertyType,
                                                       object source,
                                                       bool preview) {
        var importNotices = default(ImportNotices);
        var json = source.ToSourceValueJson();

        if (json.HasValue()) {
            importNotices = JsonConvert.DeserializeObject<ImportNotices>(json);
        }

        return importNotices;
    }

    public override Type GetPropertyValueType(IPublishedPropertyType propertyType) {
        return typeof(ImportNotices);
    }

    public override PropertyCacheLevel GetPropertyCacheLevel(IPublishedPropertyType propertyType) {
        return PropertyCacheLevel.Element;
    }
}
