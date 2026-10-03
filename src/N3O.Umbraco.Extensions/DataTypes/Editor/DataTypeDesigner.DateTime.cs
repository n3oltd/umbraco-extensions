using System.Collections.Generic;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using UmbracoPropertyEditors = Umbraco.Cms.Core.Constants.PropertyEditors;

namespace N3O.Umbraco.DataTypes;

public class DateTimeDataTypeDesigner : DataTypeDesigner {
    private string _format = "YYYY-MM-DD HH:mm:ss";

    public DateTimeDataTypeDesigner(IDataTypeService dataTypeService,
                                    IDataTypeContainerService dataTypeContainerService,
                                    PropertyEditorCollection propertyEditors,
                                    IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, dataTypeContainerService, propertyEditors, configurationEditorJsonSerializer) { }

    public DateTimeDataTypeDesigner Format(string format) {
        _format = format;

        return this;
    }

    protected override object BuildConfiguration(IDataType existing) {
        var configuration = new Dictionary<string, object>();

        configuration["format"] = _format;

        return configuration;
    }

    protected override string EditorAlias => UmbracoPropertyEditors.Aliases.DateTime;

    protected override string EditorUiAlias => "Umb.PropertyEditorUi.DatePicker";
}
