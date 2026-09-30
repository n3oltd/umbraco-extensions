using Typesense;

namespace N3O.Umbraco.Search.Typesense;

public abstract class Int32TypesenseConverter<T> : TypesenseConverter<T, int?> {
    public override FieldType FieldType => FieldType.Int32;
}
