using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using UmbracoPropertyEditors = Umbraco.Cms.Core.Constants.PropertyEditors;

namespace N3O.Umbraco.DataTypes;

public class DropdownDataTypeDesigner : ValueListDataTypeDesigner<DropdownDataTypeDesigner> {
    private bool _multiple;

    public DropdownDataTypeDesigner(IDataTypeService dataTypeService,
                                    IDataTypeContainerService dataTypeContainerService,
                                    PropertyEditorCollection propertyEditors,
                                    IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, dataTypeContainerService, propertyEditors, configurationEditorJsonSerializer) { }

    public DropdownDataTypeDesigner AllowMultiple() {
        _multiple = true;

        return this;
    }

    protected override ValueListConfiguration CreateConfiguration() {
        var configuration = new DropDownFlexibleConfiguration();

        configuration.Multiple = _multiple;

        return configuration;
    }

    protected override string EditorAlias => UmbracoPropertyEditors.Aliases.DropDownListFlexible;
}
