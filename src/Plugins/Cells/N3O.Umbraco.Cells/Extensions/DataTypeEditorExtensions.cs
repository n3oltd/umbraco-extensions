using N3O.Umbraco.Cells.DataTypes;
using N3O.Umbraco.DataTypes;

namespace N3O.Umbraco.Cells.Extensions;

public static class DataTypeEditorExtensions {
    public static CellsDataTypeDesigner NewCells(this IDataTypeEditor editor, string name) {
        return editor.New<CellsDataTypeDesigner>(name);
    }
}
