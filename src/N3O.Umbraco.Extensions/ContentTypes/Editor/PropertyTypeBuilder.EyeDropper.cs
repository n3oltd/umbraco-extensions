using N3O.Umbraco.DataTypes;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.ContentTypes;

public class EyeDropperPropertyTypeBuilder
    : ConfiguredPropertyTypeBuilder<EyeDropperPropertyTypeBuilder, EyeDropperDataTypeDesigner> {
    public EyeDropperPropertyTypeBuilder(IDataTypeService dataTypeService, EyeDropperDataTypeDesigner dataTypeDesigner)
        : base(dataTypeService, dataTypeDesigner) { }

    protected override IDataType GetDefaultDataType(PropertyTypeContext context) {
        return BuildInlineDataType(context);
    }
}
