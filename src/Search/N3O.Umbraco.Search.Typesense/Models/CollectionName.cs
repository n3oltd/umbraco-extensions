namespace N3O.Umbraco.Search.Typesense.Models;

public class CollectionName : Value {
    public CollectionName(string @base) {
        Base = @base;
    }

    public string Base { get; }
}
