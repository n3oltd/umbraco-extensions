using N3O.Umbraco.Extensions;
using System.Collections.Generic;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using UmbracoPropertyEditors = Umbraco.Cms.Core.Constants.PropertyEditors;

namespace N3O.Umbraco.DataTypes;

public class NumericDataTypeDesigner : DataTypeDesigner {
    private int? _max;
    private int? _min;
    private int? _step;

    public NumericDataTypeDesigner(IDataTypeService dataTypeService,
                                   IDataTypeContainerService dataTypeContainerService,
                                   PropertyEditorCollection propertyEditors,
                                   IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, dataTypeContainerService, propertyEditors, configurationEditorJsonSerializer) { }

    public NumericDataTypeDesigner Max(int max) {
        _max = max;

        return this;
    }

    public NumericDataTypeDesigner Min(int min) {
        _min = min;

        return this;
    }

    public NumericDataTypeDesigner Step(int step) {
        _step = step;

        return this;
    }

    protected override object BuildConfiguration(IDataType existing) {
        var configuration = new Dictionary<string, object>();

        if (_min.HasValue()) {
            configuration["min"] = _min.GetValueOrThrow();
        }

        if (_step.HasValue()) {
            configuration["step"] = _step.GetValueOrThrow();
        }

        if (_max.HasValue()) {
            configuration["max"] = _max.GetValueOrThrow();
        }

        return configuration;
    }

    protected override string EditorAlias => UmbracoPropertyEditors.Aliases.Integer;
}
