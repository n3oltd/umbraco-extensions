using N3O.Umbraco.DataTypes;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.ContentTypes;

public class NestedContentPropertyTypeBuilder
    : ConfiguredPropertyTypeBuilder<NestedContentPropertyTypeBuilder,
                                    NestedContentDataTypeDesigner> {
    public NestedContentPropertyTypeBuilder(IDataTypeService dataTypeService,
                                              NestedContentDataTypeDesigner dataTypeDesigner)
        : base(dataTypeService, dataTypeDesigner) { }

    protected override IDataType GetDefaultDataType(PropertyTypeContext context) {
        return BuildInlineDataType(context);
    }
}
