using N3O.Umbraco.Extensions;
using NodaTime;
using System;

namespace N3O.Umbraco.Search.Typesense;

public class LocalTimeTypesenseConverter : Int64TypesenseConverter<LocalTime?> {
    public override bool CanConvert(Type type) {
        return type.IsOfTypeOrNullableType<LocalTime>();
    }

    protected override LocalTime? FromTypesense(long? value) {
        return value.HasValue
                   ? LocalTime.FromNanosecondsSinceMidnight(value.GetValueOrThrow())
                   : null;
    }

    protected override long? ToTypesense(LocalTime? value) {
        return value?.NanosecondOfDay;
    }
}
