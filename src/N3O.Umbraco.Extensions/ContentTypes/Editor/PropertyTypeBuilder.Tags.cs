using N3O.Umbraco.DataTypes;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using UmbracoDataTypes = Umbraco.Cms.Core.Constants.DataTypes;

namespace N3O.Umbraco.ContentTypes;

public class TagsPropertyTypeBuilder : ConfiguredPropertyTypeBuilder<TagsPropertyTypeBuilder, TagsDataTypeDesigner> {
    public TagsPropertyTypeBuilder(IDataTypeService dataTypeService, TagsDataTypeDesigner dataTypeDesigner)
        : base(dataTypeService, dataTypeDesigner) { }

    protected override IDataType GetDefaultDataType(PropertyTypeContext context) {
        if (HasConfiguration) {
            return BuildInlineDataType(context);
        } else {
            return DataTypeService.GetDataType(UmbracoDataTypes.Tags);
        }
    }
}
