using Typesense;

namespace N3O.Umbraco.Search.Typesense;

public abstract class BoolTypesenseConverter<T> : TypesenseConverter<T, bool?> {
    public override FieldType FieldType => FieldType.Bool;
}
