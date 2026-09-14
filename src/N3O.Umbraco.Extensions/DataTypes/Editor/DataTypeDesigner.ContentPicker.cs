using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using UmbracoPropertyEditors = Umbraco.Cms.Core.Constants.PropertyEditors;

namespace N3O.Umbraco.DataTypes;

public class ContentPickerDataTypeDesigner : DataTypeDesigner {
    public ContentPickerDataTypeDesigner(IDataTypeService dataTypeService,
                                         IDataTypeContainerService dataTypeContainerService,
                                         PropertyEditorCollection propertyEditors,
                                         IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, dataTypeContainerService, propertyEditors, configurationEditorJsonSerializer) { }

    protected override object BuildConfiguration(IDataType existing) {
        // v17 removed ContentPickerConfiguration.ShowOpenButton (only IgnoreUserStartNodes remains); nothing to configure
        return null;
    }

    protected override string EditorAlias => UmbracoPropertyEditors.Aliases.ContentPicker;
}
