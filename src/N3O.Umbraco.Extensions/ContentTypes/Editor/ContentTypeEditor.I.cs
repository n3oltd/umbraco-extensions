using N3O.Umbraco.Content;

namespace N3O.Umbraco.ContentTypes;

public interface IContentTypeEditor {
    IContentTypeDesigner ForExisting(string alias);
    IDocumentTypeDesigner NewDocument(string name, string alias);
    IDocumentTypeDesigner<T> NewDocument<T>() where T : IUmbracoContent;
    IElementTypeDesigner NewElement(string name, string alias);
    IElementTypeDesigner<T> NewElement<T>() where T : IUmbracoElement;
}
