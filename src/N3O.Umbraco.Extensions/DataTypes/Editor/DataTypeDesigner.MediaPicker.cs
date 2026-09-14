using N3O.Umbraco.Extensions;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using UmbracoConventions = Umbraco.Cms.Core.Constants.Conventions;
using UmbracoPropertyEditors = Umbraco.Cms.Core.Constants.PropertyEditors;

namespace N3O.Umbraco.DataTypes;

public class MediaPickerDataTypeDesigner : DataTypeDesigner {
    private bool _enableLocalFocalPoint;
    private string _filter;
    private int? _maxNumber;
    private int? _minNumber;
    private bool _multiple;

    public MediaPickerDataTypeDesigner(IDataTypeService dataTypeService,
                                       IDataTypeContainerService dataTypeContainerService,
                                       PropertyEditorCollection propertyEditors,
                                       IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, dataTypeContainerService, propertyEditors, configurationEditorJsonSerializer) { }

    public MediaPickerDataTypeDesigner AllowMultiple() {
        _multiple = true;

        return this;
    }

    public MediaPickerDataTypeDesigner EnableLocalFocalPoint() {
        _enableLocalFocalPoint = true;

        return this;
    }

    public MediaPickerDataTypeDesigner Filter(string filter) {
        _filter = filter;

        return this;
    }

    public MediaPickerDataTypeDesigner ImagesOnly() {
        _filter = UmbracoConventions.MediaTypes.Image;

        return this;
    }

    public MediaPickerDataTypeDesigner Limit(int? min, int? max) {
        _minNumber = min;
        _maxNumber = max;

        return this;
    }

    protected override object BuildConfiguration(IDataType existing) {
        var configuration = new MediaPicker3Configuration();

        configuration.EnableLocalFocalPoint = _enableLocalFocalPoint;
        configuration.Multiple = _multiple;

        if (_filter.HasValue()) {
            configuration.Filter = _filter;
        }

        if (_minNumber.HasValue() || _maxNumber.HasValue()) {
            configuration.ValidationLimit.Min = _minNumber;
            configuration.ValidationLimit.Max = _maxNumber;
        }

        return configuration;
    }

    protected override string EditorAlias => UmbracoPropertyEditors.Aliases.MediaPicker3;
}
