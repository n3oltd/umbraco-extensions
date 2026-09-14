using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using UmbracoPropertyEditors = Umbraco.Cms.Core.Constants.PropertyEditors;

namespace N3O.Umbraco.DataTypes;

public class MultiUrlPickerDataTypeDesigner : DataTypeDesigner {
    private int _maxNumber;
    private int _minNumber;

    public MultiUrlPickerDataTypeDesigner(IDataTypeService dataTypeService,
                                          IDataTypeContainerService dataTypeContainerService,
                                          PropertyEditorCollection propertyEditors,
                                          IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, dataTypeContainerService, propertyEditors, configurationEditorJsonSerializer) { }

    public MultiUrlPickerDataTypeDesigner Limit(int min, int max) {
        _minNumber = min;
        _maxNumber = max;

        return this;
    }

    protected override object BuildConfiguration(IDataType existing) {
        var configuration = new MultiUrlPickerConfiguration();

        configuration.MaxNumber = _maxNumber;
        configuration.MinNumber = _minNumber;

        return configuration;
    }

    protected override string EditorAlias => UmbracoPropertyEditors.Aliases.MultiUrlPicker;
}
