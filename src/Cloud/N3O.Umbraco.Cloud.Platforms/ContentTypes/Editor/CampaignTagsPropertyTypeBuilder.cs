using N3O.Umbraco.Cloud.Platforms.DataTypes;
using N3O.Umbraco.ContentTypes;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cloud.Platforms.ContentTypes;

public class CampaignTagsPropertyTypeBuilder
    : ConfiguredPropertyTypeBuilder<CampaignTagsPropertyTypeBuilder, CampaignTagsDataTypeDesigner> {
    public CampaignTagsPropertyTypeBuilder(IDataTypeService dataTypeService,
                                           CampaignTagsDataTypeDesigner dataTypeDesigner)
        : base(dataTypeService, dataTypeDesigner) { }

    protected override IDataType GetDefaultDataType(PropertyTypeContext context) {
        return BuildInlineDataType(context);
    }
}
