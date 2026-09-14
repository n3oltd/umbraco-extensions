using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using UmbracoPropertyEditors = Umbraco.Cms.Core.Constants.PropertyEditors;

namespace N3O.Umbraco.DataTypes;

public class TextareaDataTypeDesigner : DataTypeDesigner {
    private int? _maxChars;

    public TextareaDataTypeDesigner(IDataTypeService dataTypeService,
                                    IDataTypeContainerService dataTypeContainerService,
                                    PropertyEditorCollection propertyEditors,
                                    IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, dataTypeContainerService, propertyEditors, configurationEditorJsonSerializer) { }

    public TextareaDataTypeDesigner MaxChars(int maxChars) {
        _maxChars = maxChars;

        return this;
    }

    protected override object BuildConfiguration(IDataType existing) {
        var configuration = new TextAreaConfiguration();

        configuration.MaxChars = _maxChars;

        return configuration;
    }

    protected override string EditorAlias => UmbracoPropertyEditors.Aliases.TextArea;
}
