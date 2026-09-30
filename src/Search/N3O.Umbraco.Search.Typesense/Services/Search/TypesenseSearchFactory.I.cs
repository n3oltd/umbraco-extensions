using N3O.Umbraco.Search.Typesense.Models;

namespace N3O.Umbraco.Search.Typesense;

public interface ITypesenseSearchFactory {
    TSearch GetSearch<TDocument, TSearch>(params object[] criteria)
        where TDocument : SearchDocument
        where TSearch : ITypesenseSearch<TDocument>;
}
