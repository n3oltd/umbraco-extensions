using N3O.Umbraco.Extensions;
using NodaTime;
using System;

namespace N3O.Umbraco.Search.Typesense;

public class InstantTypesenseConverter : Int64TypesenseConverter<Instant?> {
    public override bool CanConvert(Type type) {
        return type.IsOfTypeOrNullableType<Instant>();
    }

    protected override Instant? FromTypesense(long? value) {
        return value.HasValue
                   ? Instant.FromUnixTimeMilliseconds(value.GetValueOrThrow())
                   : null;
    }

    protected override long? ToTypesense(Instant? value) {
        return value?.ToUnixTimeMilliseconds();
    }
}
