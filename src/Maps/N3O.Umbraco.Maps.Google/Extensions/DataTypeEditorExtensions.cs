using N3O.Umbraco.DataTypes;
using N3O.Umbraco.Maps.DataTypes;

namespace N3O.Umbraco.Maps.Extensions;

public static class DataTypeEditorExtensions {
    public static GoogleMapsDataTypeDesigner NewGoogleMaps(this IDataTypeEditor editor, string name) {
        return editor.New<GoogleMapsDataTypeDesigner>(name);
    }
}
