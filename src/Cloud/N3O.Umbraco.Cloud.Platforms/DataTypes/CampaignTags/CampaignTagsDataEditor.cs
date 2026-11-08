using Umbraco.Cms.Core.PropertyEditors;

namespace N3O.Umbraco.Cloud.Platforms.DataTypes;

[DataEditor(PlatformsSchemaConstants.PropertyEditorAliases.CampaignTags, ValueType = ValueTypes.Json)]
public class CampaignTagsDataEditor : DataEditor {
    public CampaignTagsDataEditor(IDataValueEditorFactory dataValueEditorFactory) : base(dataValueEditorFactory) { }
}
