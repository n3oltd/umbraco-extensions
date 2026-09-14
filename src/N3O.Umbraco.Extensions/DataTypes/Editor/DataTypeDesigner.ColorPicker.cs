using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using UmbracoPropertyEditors = Umbraco.Cms.Core.Constants.PropertyEditors;

namespace N3O.Umbraco.DataTypes;

public class ColorPickerDataTypeDesigner : DataTypeDesigner {
    private readonly List<(string Color, string Label)> _colors = [];

    private bool _useLabel;

    public ColorPickerDataTypeDesigner(IDataTypeService dataTypeService,
                                       IDataTypeContainerService dataTypeContainerService,
                                       PropertyEditorCollection propertyEditors,
                                       IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, dataTypeContainerService, propertyEditors, configurationEditorJsonSerializer) { }

    public ColorPickerDataTypeDesigner AddColor(string color, string label) {
        _colors.Add((ToHex(color), label));
        _useLabel = true;

        return this;
    }

    public ColorPickerDataTypeDesigner Options(params string[] values) {
        foreach (var value in values) {
            _colors.Add((ToHex(value), null));
        }

        return this;
    }

    public ColorPickerDataTypeDesigner UseLabel() {
        _useLabel = true;

        return this;
    }

    protected override object BuildConfiguration(IDataType existing) {
        var configuration = new ColorPickerConfiguration();

        configuration.UseLabel = _useLabel;
        configuration.Items = _colors.Select(x => new ColorPickerConfiguration.ColorPickerItem {
                                          Value = x.Color,
                                          Label = x.Label ?? x.Color
                                      })
                                      .ToList();

        return configuration;
    }

    protected override string EditorAlias => UmbracoPropertyEditors.Aliases.ColorPicker;

    private string ToHex(string color) {
        return color.TrimStart('#');
    }
}
