using N3O.Umbraco.Extensions;
using NodaTime;
using System;

namespace N3O.Umbraco.Search.Typesense;

public class LocalDateTypesenseConverter : Int64TypesenseConverter<LocalDate?> {
    public override bool CanConvert(Type type) {
        return type.IsOfTypeOrNullableType<LocalDate>();
    }

    protected override LocalDate? FromTypesense(long? value) {
        return value.HasValue
                   ? Instant.FromUnixTimeMilliseconds(value.GetValueOrThrow()).InUtc().Date
                   : null;
    }

    protected override long? ToTypesense(LocalDate? value) {
        return value?.AtMidnight().InUtc().ToInstant().ToUnixTimeMilliseconds();
    }
}
