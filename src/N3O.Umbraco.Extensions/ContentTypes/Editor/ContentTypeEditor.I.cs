using N3O.Umbraco.Content;
using Umbraco.Cms.Core.Models;

namespace N3O.Umbraco.ContentTypes;

public interface IContentTypeEditor {
    IContentType Find(string alias);
    IContentTypeDesigner ForExisting(string alias);
    IDocumentTypeDesigner NewDocument(string name, string alias);
    IDocumentTypeDesigner<T> NewDocument<T>() where T : IUmbracoContent;
    IElementTypeDesigner NewElement(string name, string alias);
    IElementTypeDesigner<T> NewElement<T>() where T : IUmbracoElement;
}
