using Typesense;

namespace N3O.Umbraco.Search.Typesense;

public abstract class ObjectTypesenseConverter<T> : TypesenseConverter<T, object> {
    public override FieldType FieldType => FieldType.Object;
}
