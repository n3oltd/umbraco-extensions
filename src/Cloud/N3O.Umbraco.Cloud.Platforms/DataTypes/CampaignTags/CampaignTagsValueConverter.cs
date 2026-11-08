using N3O.Umbraco.Cloud.Platforms.Models;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.ValueConverters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;

namespace N3O.Umbraco.Cloud.Platforms.DataTypes;

public class CampaignTagsValueConverter : PropertyValueConverter {
    public override bool IsConverter(IPublishedPropertyType propertyType) {
        return propertyType.EditorAlias.EqualsInvariant(PlatformsSchemaConstants.PropertyEditorAliases.CampaignTags);
    }

    public override object ConvertSourceToIntermediate(IPublishedElement owner,
                                                       IPublishedPropertyType propertyType,
                                                       object source,
                                                       bool preview) {
        var campaignTags = default(List<CampaignTag>);
        var json = source.ToSourceValueJson();

        if (json.HasValue()) {
            campaignTags = JsonConvert.DeserializeObject<List<CampaignTag>>(json);
        }

        return campaignTags.OrEmpty().ToList();
    }

    public override Type GetPropertyValueType(IPublishedPropertyType propertyType) {
        return typeof(IEnumerable<CampaignTag>);
    }

    public override PropertyCacheLevel GetPropertyCacheLevel(IPublishedPropertyType propertyType) {
        return PropertyCacheLevel.Element;
    }
}
