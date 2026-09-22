namespace N3O.Umbraco.Search.Typesense;

public interface ICollectionNameResolver {
    string Resolve(string name);
}
