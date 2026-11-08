using N3O.Umbraco.Cloud.Platforms.DataTypes;
using N3O.Umbraco.DataTypes;

namespace N3O.Umbraco.Cloud.Platforms.Extensions;

public static class DataTypeEditorExtensions {
    public static CampaignTagsDataTypeDesigner NewCampaignTags(this IDataTypeEditor editor, string name) {
        return editor.New<CampaignTagsDataTypeDesigner>(name);
    }
}
