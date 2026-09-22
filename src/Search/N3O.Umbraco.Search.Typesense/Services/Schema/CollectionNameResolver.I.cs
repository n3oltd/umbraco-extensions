using N3O.Umbraco.Search.Typesense.Models;

namespace N3O.Umbraco.Search.Typesense;

public interface ICollectionNameResolver {
    string Resolve(CollectionName name);
}
