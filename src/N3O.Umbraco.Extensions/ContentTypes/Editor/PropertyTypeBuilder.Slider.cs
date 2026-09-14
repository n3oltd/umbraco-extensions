using N3O.Umbraco.DataTypes;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.ContentTypes;

public class SliderPropertyTypeBuilder
    : ConfiguredPropertyTypeBuilder<SliderPropertyTypeBuilder, SliderDataTypeDesigner> {
    public SliderPropertyTypeBuilder(IDataTypeService dataTypeService, SliderDataTypeDesigner dataTypeDesigner)
        : base(dataTypeService, dataTypeDesigner) { }

    protected override IDataType GetDefaultDataType(PropertyTypeContext context) {
        return BuildInlineDataType(context);
    }
}
