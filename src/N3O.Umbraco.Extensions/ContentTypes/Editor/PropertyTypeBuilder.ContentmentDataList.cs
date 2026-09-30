using N3O.Umbraco.DataTypes;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.ContentTypes;

public class ContentmentDataListPropertyTypeBuilder
    : ConfiguredPropertyTypeBuilder<ContentmentDataListPropertyTypeBuilder,
                                    ContentmentDataListDataTypeDesigner> {
    public ContentmentDataListPropertyTypeBuilder(IDataTypeService dataTypeService,
                                                    ContentmentDataListDataTypeDesigner dataTypeDesigner)
        : base(dataTypeService, dataTypeDesigner) { }

    protected override IDataType GetDefaultDataType(PropertyTypeContext context) {
        return BuildInlineDataType(context);
    }
}
