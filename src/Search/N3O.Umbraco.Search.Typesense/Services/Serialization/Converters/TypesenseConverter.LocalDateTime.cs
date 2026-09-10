using N3O.Umbraco.Extensions;
using NodaTime;
using System;

namespace N3O.Umbraco.Search.Typesense;

public class LocalDateTimeTypesenseConverter : Int64TypesenseConverter<LocalDateTime?> {
    public override bool CanConvert(Type type) {
        return type.IsOfTypeOrNullableType<LocalDateTime>();
    }

    protected override LocalDateTime? FromTypesense(long? value) {
        return value.HasValue
                   ? Instant.FromUnixTimeMilliseconds(value.GetValueOrThrow()).InUtc().LocalDateTime
                   : null;
    }

    protected override long? ToTypesense(LocalDateTime? value) {
        return value?.InUtc().ToInstant().ToUnixTimeMilliseconds();
    }
}
