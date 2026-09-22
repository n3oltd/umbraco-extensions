using System.Collections.Generic;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.DataTypes;

public class ContentmentTemplatedLabelDataTypeDesigner : DataTypeDesigner {
    private bool _hideLabel;
    private bool _hidePropertyGroup;
    private string _template;
    private string _valueType = "TEXT";

    public ContentmentTemplatedLabelDataTypeDesigner(IDataTypeService dataTypeService,
                                                     PropertyEditorCollection propertyEditors,
                                                     IConfigurationEditorJsonSerializer serializer)
        : base(dataTypeService, propertyEditors, serializer) { }

    public ContentmentTemplatedLabelDataTypeDesigner HideLabel() {
        _hideLabel = true;

        return this;
    }

    public ContentmentTemplatedLabelDataTypeDesigner HidePropertyGroup() {
        _hidePropertyGroup = true;

        return this;
    }

    public ContentmentTemplatedLabelDataTypeDesigner Template(string template) {
        _template = template;

        return this;
    }

    public ContentmentTemplatedLabelDataTypeDesigner ValueType(string valueType) {
        _valueType = valueType;

        return this;
    }

    protected override object BuildConfiguration(IDataType existing) {
        var configuration = new Dictionary<string, object>();

        configuration["umbracoDataValueType"] = _valueType;
        configuration["_notes"] = null;
        configuration["notes"] = _template;
        configuration["hideLabel"] = _hideLabel ? "1" : "0";
        configuration["hidePropertyGroup"] = _hidePropertyGroup ? "1" : "0";
        configuration["enableDevMode"] = "0";

        return configuration;
    }

    protected override string EditorAlias => "Umbraco.Community.Contentment.TemplatedLabel";
}
