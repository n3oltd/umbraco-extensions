using Typesense;

namespace N3O.Umbraco.Search.Typesense;

public abstract class StringTypesenseConverter<T> : TypesenseConverter<T, string> {
    public override FieldType FieldType => FieldType.String;
}
