using N3O.Umbraco.Search.Typesense.Models;
using System;

namespace N3O.Umbraco.Search.Typesense;

public interface ISearchDocumentBuilder<TDocument> where TDocument : SearchDocument, new() {
    ISearchDocumentBuilder<TDocument> Set(Action<TDocument> action);
    object Build();
}
