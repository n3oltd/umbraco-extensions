namespace N3O.Umbraco.ContentTypes;

public interface IElementTypeDesigner : IContentTypeDesigner {
    void AllowInLibrary();
}

public interface IElementTypeDesigner<T> : IElementTypeDesigner, IContentTypeDesigner<T> { }
