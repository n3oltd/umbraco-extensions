using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using UmbracoPropertyEditors = Umbraco.Cms.Core.Constants.PropertyEditors;

namespace N3O.Umbraco.DataTypes;

public class LabelDataTypeDesigner : DataTypeDesigner {
    private string _valueType = ValueTypes.String;

    public LabelDataTypeDesigner(IDataTypeService dataTypeService,
                                 IDataTypeContainerService dataTypeContainerService,
                                 PropertyEditorCollection propertyEditors,
                                 IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, dataTypeContainerService, propertyEditors, configurationEditorJsonSerializer) { }

    public LabelDataTypeDesigner ValueType(string valueType) {
        _valueType = valueType;

        return this;
    }

    protected override object BuildConfiguration(IDataType existing) {
        var configuration = new LabelConfiguration();

        configuration.ValueType = _valueType;

        return configuration;
    }

    protected override string EditorAlias => UmbracoPropertyEditors.Aliases.Label;
}
