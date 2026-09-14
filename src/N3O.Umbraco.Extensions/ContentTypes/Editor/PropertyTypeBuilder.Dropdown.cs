using N3O.Umbraco.DataTypes;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.ContentTypes;

public class DropdownPropertyTypeBuilder
    : ConfiguredPropertyTypeBuilder<DropdownPropertyTypeBuilder, DropdownDataTypeDesigner> {
    public DropdownPropertyTypeBuilder(IDataTypeService dataTypeService, DropdownDataTypeDesigner dataTypeDesigner)
        : base(dataTypeService, dataTypeDesigner) { }

    protected override IDataType GetDefaultDataType(PropertyTypeContext context) {
        return BuildInlineDataType(context);
    }
}
