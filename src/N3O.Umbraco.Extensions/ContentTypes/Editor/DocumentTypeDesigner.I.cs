namespace N3O.Umbraco.ContentTypes;

public interface IDocumentTypeDesigner : IContentTypeDesigner {
    void AllowAtRoot();
    void AllowChildren(params string[] contentTypeAliases);
}

public interface IDocumentTypeDesigner<T> : IDocumentTypeDesigner, IContentTypeDesigner<T> { }
