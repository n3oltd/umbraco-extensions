using N3O.Umbraco.Extensions;
using System;

namespace N3O.Umbraco.Search.Typesense;

public class DateTimeOffsetTypesenseConverter : Int64TypesenseConverter<DateTimeOffset?> {
    public override bool CanConvert(Type type) {
        return type.IsOfTypeOrNullableType<DateTimeOffset>();
    }

    protected override DateTimeOffset? FromTypesense(long? value) {
        return value.HasValue
                   ? DateTimeOffset.FromUnixTimeMilliseconds(value.GetValueOrThrow())
                   : null;
    }

    protected override long? ToTypesense(DateTimeOffset? value) {
        return value?.ToUnixTimeMilliseconds();
    }
}
