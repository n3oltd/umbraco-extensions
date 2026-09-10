using Typesense;

namespace N3O.Umbraco.Search.Typesense;

public abstract class Int64TypesenseConverter<T> : TypesenseConverter<T, long?> {
    public override FieldType FieldType => FieldType.Int64;
}
