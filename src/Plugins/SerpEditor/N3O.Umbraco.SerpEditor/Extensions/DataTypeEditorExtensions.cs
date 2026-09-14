using N3O.Umbraco.DataTypes;
using N3O.Umbraco.SerpEditor.DataTypes;

namespace N3O.Umbraco.SerpEditor.Extensions;

public static class DataTypeEditorExtensions {
    public static SerpEditorDataTypeDesigner NewSerpEditor(this IDataTypeEditor editor, string name) {
        return editor.New<SerpEditorDataTypeDesigner>(name);
    }
}
