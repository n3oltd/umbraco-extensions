using N3O.Umbraco.Cells.DataTypes;
using N3O.Umbraco.ContentTypes;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cells.ContentTypes;

public class CellsPropertyTypeBuilder : ConfiguredPropertyTypeBuilder<CellsPropertyTypeBuilder, CellsDataTypeDesigner> {
    public CellsPropertyTypeBuilder(IDataTypeService dataTypeService, CellsDataTypeDesigner dataTypeDesigner)
        : base(dataTypeService, dataTypeDesigner) { }

    protected override IDataType GetDefaultDataType(PropertyTypeContext context) {
        return BuildInlineDataType(context);
    }
}
