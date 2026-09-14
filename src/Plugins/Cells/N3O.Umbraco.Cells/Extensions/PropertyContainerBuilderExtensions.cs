using N3O.Umbraco.Cells.ContentTypes;
using N3O.Umbraco.ContentTypes;
using System;
using System.Linq.Expressions;

namespace N3O.Umbraco.Cells.Extensions;

public static class PropertyContainerBuilderExtensions {
    public static CellsPropertyTypeBuilder Cells(this IPropertyContainerBuilder builder, string propertyAlias) {
        return builder.Property<CellsPropertyTypeBuilder>(propertyAlias);
    }

    public static CellsPropertyTypeBuilder Cells<T, TProperty>(this IPropertyContainerBuilder<T> builder,
                                                               Expression<Func<T, TProperty>> expression) {
        return builder.Property<CellsPropertyTypeBuilder, TProperty>(expression);
    }
}
