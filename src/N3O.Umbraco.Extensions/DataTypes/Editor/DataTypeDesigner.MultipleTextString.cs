using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using UmbracoPropertyEditors = Umbraco.Cms.Core.Constants.PropertyEditors;

namespace N3O.Umbraco.DataTypes;

public class MultipleTextStringDataTypeDesigner : DataTypeDesigner {
    private int _maximum;
    private int _minimum;

    public MultipleTextStringDataTypeDesigner(IDataTypeService dataTypeService,
                                              IDataTypeContainerService dataTypeContainerService,
                                              PropertyEditorCollection propertyEditors,
                                              IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, dataTypeContainerService, propertyEditors, configurationEditorJsonSerializer) { }

    public MultipleTextStringDataTypeDesigner Limit(int min, int max) {
        _minimum = min;
        _maximum = max;

        return this;
    }

    protected override object BuildConfiguration(IDataType existing) {
        var configuration = new MultipleTextStringConfiguration();

        configuration.Max = _maximum;
        configuration.Min = _minimum;

        return configuration;
    }

    protected override string EditorAlias =>
        UmbracoPropertyEditors.Aliases.MultipleTextstring;
}
