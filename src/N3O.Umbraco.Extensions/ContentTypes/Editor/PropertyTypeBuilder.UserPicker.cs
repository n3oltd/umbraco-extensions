using N3O.Umbraco.DataTypes;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.ContentTypes;

public class UserPickerPropertyTypeBuilder
    : ConfiguredPropertyTypeBuilder<UserPickerPropertyTypeBuilder, UserPickerDataTypeDesigner> {
    public UserPickerPropertyTypeBuilder(IDataTypeService dataTypeService, UserPickerDataTypeDesigner dataTypeDesigner)
        : base(dataTypeService, dataTypeDesigner) { }

    protected override IDataType GetDefaultDataType(PropertyTypeContext context) {
        return BuildInlineDataType(context);
    }
}
