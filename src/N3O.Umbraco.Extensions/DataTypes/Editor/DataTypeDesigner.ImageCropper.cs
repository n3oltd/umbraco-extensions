using System.Collections.Generic;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using UmbracoPropertyEditors = Umbraco.Cms.Core.Constants.PropertyEditors;

namespace N3O.Umbraco.DataTypes;

public class ImageCropperDataTypeDesigner : DataTypeDesigner {
    private readonly List<ImageCropperConfiguration.Crop> _crops = [];

    public ImageCropperDataTypeDesigner(IDataTypeService dataTypeService,
                                        IDataTypeContainerService dataTypeContainerService,
                                        PropertyEditorCollection propertyEditors,
                                        IConfigurationEditorJsonSerializer configurationEditorJsonSerializer)
        : base(dataTypeService, dataTypeContainerService, propertyEditors, configurationEditorJsonSerializer) { }

    public ImageCropperDataTypeDesigner AddCrop(string alias, int width, int height) {
        var crop = new ImageCropperConfiguration.Crop();

        crop.Alias = alias;
        crop.Width = width;
        crop.Height = height;

        _crops.Add(crop);

        return this;
    }

    protected override object BuildConfiguration(IDataType existing) {
        var configuration = new ImageCropperConfiguration();

        configuration.Crops = _crops.ToArray();

        return configuration;
    }

    protected override string EditorAlias => UmbracoPropertyEditors.Aliases.ImageCropper;
}
