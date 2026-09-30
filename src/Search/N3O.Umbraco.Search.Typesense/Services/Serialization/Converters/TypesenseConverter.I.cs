using System;
using Typesense;

namespace N3O.Umbraco.Search.Typesense;

public interface ITypesenseConverter {
    bool CanConvert(Type type);

    object FromTypesenseValue(object value);
    object ToTypesenseValue(object value);

    Type UnderlyingTypesenseType { get; }
    FieldType FieldType { get; }
}
