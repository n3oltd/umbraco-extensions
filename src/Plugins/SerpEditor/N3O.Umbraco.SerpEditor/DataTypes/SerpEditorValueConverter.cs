using N3O.Umbraco.Extensions;
using N3O.Umbraco.SerpEditor.Models;
using N3O.Umbraco.ValueConverters;
using Newtonsoft.Json;
using System;
using System.Text.Json.Nodes;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;

namespace N3O.Umbraco.SerpEditor.DataTypes;

public class SerpEditorValueConverter : PropertyValueConverter {
    public override bool IsConverter(IPublishedPropertyType propertyType) {
        return propertyType.EditorAlias.EqualsInvariant(SerpEditorConstants.PropertyEditorAlias);
    }

    public override object ConvertSourceToIntermediate(IPublishedElement owner,
                                                       IPublishedPropertyType propertyType,
                                                       object source,
                                                       bool preview) {
        SerpEntry entry;
        string json = null;
        
        if (source is string str) {
            json = str;
        } else if (source is JsonNode jsonNode) {
            json = jsonNode.ToJsonString();
        }

        if (json.HasValue()) {
            entry = JsonConvert.DeserializeObject<SerpEntry>(json);
        } else {
            entry = new SerpEntry();
        }

        return entry;
    }

    public override Type GetPropertyValueType(IPublishedPropertyType propertyType) {
        return typeof(SerpEntry);
    }

    public override PropertyCacheLevel GetPropertyCacheLevel(IPublishedPropertyType propertyType) {
        return PropertyCacheLevel.Element;
    }
}
