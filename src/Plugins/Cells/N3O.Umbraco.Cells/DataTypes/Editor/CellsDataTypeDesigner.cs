using N3O.Umbraco.DataTypes;
using N3O.Umbraco.Extensions;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cells.DataTypes;

public class CellsDataTypeDesigner : DataTypeDesigner {
    private string _gridConfiguration;

    public CellsDataTypeDesigner(IDataTypeService dataTypeService,
                                 IDataTypeContainerService dataTypeContainerService,
                                 PropertyEditorCollection propertyEditors,
                                 IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, dataTypeContainerService, propertyEditors, configurationEditorJsonSerializer) { }

    public CellsDataTypeDesigner GridConfiguration(string gridConfiguration) {
        _gridConfiguration = gridConfiguration;

        return this;
    }

    protected override object BuildConfiguration(IDataType existing) {
        var configuration = new CellsConfiguration();

        if (_gridConfiguration.HasValue()) {
            configuration.GridConfiguration = _gridConfiguration;
        }

        return configuration;
    }

    protected override string EditorAlias => CellsConstants.PropertyEditorAlias;
}
