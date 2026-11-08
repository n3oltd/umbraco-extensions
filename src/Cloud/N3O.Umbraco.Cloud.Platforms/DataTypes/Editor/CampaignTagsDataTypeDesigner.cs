using N3O.Umbraco.DataTypes;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cloud.Platforms.DataTypes;

public class CampaignTagsDataTypeDesigner : DataTypeDesigner {
    public CampaignTagsDataTypeDesigner(IDataTypeService dataTypeService,
                                        IDataTypeContainerService dataTypeContainerService,
                                        PropertyEditorCollection propertyEditors,
                                        IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, dataTypeContainerService, propertyEditors, configurationEditorJsonSerializer) { }

    protected override object BuildConfiguration(IDataType existing) {
        return null;
    }

    protected override string EditorAlias => PlatformsSchemaConstants.PropertyEditorAliases.CampaignTags;
}
