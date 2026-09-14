using N3O.Umbraco.Extensions;
using System.Collections.Generic;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using UmbracoPropertyEditors = Umbraco.Cms.Core.Constants.PropertyEditors;

namespace N3O.Umbraco.DataTypes;

public class DecimalDataTypeDesigner : DataTypeDesigner {
    private decimal? _max;
    private decimal? _min;
    private decimal? _step;

    public DecimalDataTypeDesigner(IDataTypeService dataTypeService,
                                   IDataTypeContainerService dataTypeContainerService,
                                   PropertyEditorCollection propertyEditors,
                                   IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, dataTypeContainerService, propertyEditors, configurationEditorJsonSerializer) { }

    public DecimalDataTypeDesigner Max(decimal max) {
        _max = max;

        return this;
    }

    public DecimalDataTypeDesigner Min(decimal min) {
        _min = min;

        return this;
    }

    public DecimalDataTypeDesigner Step(decimal step) {
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

    protected override string EditorAlias => UmbracoPropertyEditors.Aliases.Decimal;
}
