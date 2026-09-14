using N3O.Umbraco.ContentTypes;
using N3O.Umbraco.SerpEditor.DataTypes;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.SerpEditor.ContentTypes;

public class SerpEditorPropertyTypeBuilder
    : ConfiguredPropertyTypeBuilder<SerpEditorPropertyTypeBuilder, SerpEditorDataTypeDesigner> {
    public SerpEditorPropertyTypeBuilder(IDataTypeService dataTypeService, SerpEditorDataTypeDesigner dataTypeDesigner)
        : base(dataTypeService, dataTypeDesigner) { }

    protected override IDataType GetDefaultDataType(PropertyTypeContext context) {
        return BuildInlineDataType(context);
    }
}
