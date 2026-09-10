using N3O.Umbraco.Extensions;
using System;

namespace N3O.Umbraco.Search.Typesense;

public class UriTypesenseConverter : StringTypesenseConverter<Uri> {
    public override bool CanConvert(Type type) {
        return type == typeof(Uri);
    }

    protected override Uri FromTypesense(string value) {
        return value.HasValue() ? new Uri(value, UriKind.RelativeOrAbsolute) : null;
    }

    protected override string ToTypesense(Uri value) {
        return value?.OriginalString;
    }
}
