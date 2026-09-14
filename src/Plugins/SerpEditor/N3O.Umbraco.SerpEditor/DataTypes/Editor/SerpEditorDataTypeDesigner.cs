using N3O.Umbraco.DataTypes;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.SerpEditor.DataTypes;

public class SerpEditorDataTypeDesigner : DataTypeDesigner {
    public SerpEditorDataTypeDesigner(IDataTypeService dataTypeService,
                                      IDataTypeContainerService dataTypeContainerService,
                                      PropertyEditorCollection propertyEditors,
                                      IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, dataTypeContainerService, propertyEditors, configurationEditorJsonSerializer) { }

    protected override object BuildConfiguration(IDataType existing) {
        return new SerpEditorConfiguration();
    }

    protected override string EditorAlias => SerpEditorConstants.PropertyEditorAlias;
}
