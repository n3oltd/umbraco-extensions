using N3O.Umbraco.DataTypes;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.ContentTypes;

public class MemberGroupPickerPropertyTypeBuilder
    : ConfiguredPropertyTypeBuilder<MemberGroupPickerPropertyTypeBuilder, MemberGroupPickerDataTypeDesigner> {
    public MemberGroupPickerPropertyTypeBuilder(IDataTypeService dataTypeService,
                                                MemberGroupPickerDataTypeDesigner dataTypeDesigner)
        : base(dataTypeService, dataTypeDesigner) { }

    protected override IDataType GetDefaultDataType(PropertyTypeContext context) {
        return BuildInlineDataType(context);
    }
}
