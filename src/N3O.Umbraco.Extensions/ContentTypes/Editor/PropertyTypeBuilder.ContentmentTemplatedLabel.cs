using N3O.Umbraco.DataTypes;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.ContentTypes;

public class ContentmentTemplatedLabelPropertyTypeBuilder
    : ConfiguredPropertyTypeBuilder<ContentmentTemplatedLabelPropertyTypeBuilder,
                                    ContentmentTemplatedLabelDataTypeDesigner> {
    public ContentmentTemplatedLabelPropertyTypeBuilder(IDataTypeService dataTypeService,
                                                          ContentmentTemplatedLabelDataTypeDesigner dataTypeDesigner)
        : base(dataTypeService, dataTypeDesigner) { }

    protected override IDataType GetDefaultDataType(PropertyTypeContext context) {
        return BuildInlineDataType(context);
    }
}
