using Typesense;

namespace N3O.Umbraco.Search.Typesense;

public abstract class FloatTypesenseConverter<T> : TypesenseConverter<T, decimal?> {
    public override FieldType FieldType => FieldType.Float;
}
