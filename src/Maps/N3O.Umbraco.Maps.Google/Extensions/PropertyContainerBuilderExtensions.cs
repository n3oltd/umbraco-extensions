using N3O.Umbraco.ContentTypes;
using N3O.Umbraco.Maps.ContentTypes;
using System;
using System.Linq.Expressions;

namespace N3O.Umbraco.Maps.Extensions;

public static class PropertyContainerBuilderExtensions {
    public static GoogleMapsPropertyTypeBuilder GoogleMaps(this IPropertyContainerBuilder builder,
                                                           string propertyAlias) {
        return builder.Property<GoogleMapsPropertyTypeBuilder>(propertyAlias);
    }

    public static GoogleMapsPropertyTypeBuilder GoogleMaps<T, TProperty>(this IPropertyContainerBuilder<T> builder,
                                                                         Expression<Func<T, TProperty>> expression) {
        return builder.Property<GoogleMapsPropertyTypeBuilder, TProperty>(expression);
    }
}
