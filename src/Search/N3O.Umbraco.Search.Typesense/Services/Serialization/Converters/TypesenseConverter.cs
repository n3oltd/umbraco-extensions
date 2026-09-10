using System;
using Typesense;

namespace N3O.Umbraco.Search.Typesense;

public abstract class TypesenseConverter<T, U> : ITypesenseConverter {
    public abstract bool CanConvert(Type type);

    public object FromTypesenseValue(object value) {
        if (value == null) {
            return FromTypesense(default(U));
        }

        if (value is U typed) {
            return FromTypesense(typed);
        }

        var targetType = Nullable.GetUnderlyingType(typeof(U)) ?? typeof(U);
        var converted = (U) Convert.ChangeType(value, targetType);

        return FromTypesense(converted);
    }

    public object ToTypesenseValue(object value) {
        if (value == null) {
            return null;
        } else {
            return ToTypesense((T) value);
        }
    }

    protected abstract T FromTypesense(U value);
    protected abstract U ToTypesense(T value);

    public Type UnderlyingTypesenseType => typeof(U);
    public abstract FieldType FieldType { get; }
}
