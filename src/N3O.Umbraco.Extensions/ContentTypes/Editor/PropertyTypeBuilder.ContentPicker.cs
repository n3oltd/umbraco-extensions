using N3O.Umbraco.DataTypes;
using System;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using UmbracoDataTypes = Umbraco.Cms.Core.Constants.DataTypes;

namespace N3O.Umbraco.ContentTypes;

public class ContentPickerPropertyTypeBuilder
    : ConfiguredPropertyTypeBuilder<ContentPickerPropertyTypeBuilder, ContentPickerDataTypeDesigner> {
    public ContentPickerPropertyTypeBuilder(IDataTypeService dataTypeService,
                                            ContentPickerDataTypeDesigner dataTypeDesigner)
        : base(dataTypeService, dataTypeDesigner) { }

    protected override IDataType GetDefaultDataType(PropertyTypeContext context) {
        if (HasConfiguration) {
            return BuildInlineDataType(context);
        } else {
            var key = Guid.Parse(UmbracoDataTypes.Guids.ContentPicker);

            return DataTypeService.GetAsync(key).GetAwaiter().GetResult();
        }
    }
}
