using Humanizer;

namespace N3O.Umbraco.ContentTypes;

public class PropertyTypeContext {
    public PropertyTypeContext(string contentTypeAlias, string propertyAlias) {
        ContentTypeAlias = contentTypeAlias;
        PropertyAlias = propertyAlias;
    }

    public string ContentTypeAlias { get; }
    public string DataTypeName => $"{ContentTypeAlias.Titleize()} {PropertyAlias.Titleize()}";
    public string DataTypeSeed => $"{ContentTypeAlias}_{PropertyAlias}";
    public string PropertyAlias { get; }
}
