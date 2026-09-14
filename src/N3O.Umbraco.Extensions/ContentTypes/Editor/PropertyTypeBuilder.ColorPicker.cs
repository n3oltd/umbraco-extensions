using N3O.Umbraco.DataTypes;
using System;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using UmbracoDataTypes = Umbraco.Cms.Core.Constants.DataTypes;

namespace N3O.Umbraco.ContentTypes;

public class ColorPickerPropertyTypeBuilder
    : ConfiguredPropertyTypeBuilder<ColorPickerPropertyTypeBuilder, ColorPickerDataTypeDesigner> {
    public ColorPickerPropertyTypeBuilder(IDataTypeService dataTypeService,
                                          ColorPickerDataTypeDesigner dataTypeDesigner)
        : base(dataTypeService, dataTypeDesigner) { }

    protected override IDataType GetDefaultDataType(PropertyTypeContext context) {
        if (HasConfiguration) {
            return BuildInlineDataType(context);
        } else {
            var key = Guid.Parse(UmbracoDataTypes.Guids.ApprovedColor);

            return DataTypeService.GetAsync(key).GetAwaiter().GetResult();
        }
    }
}
