using N3O.Umbraco.ContentTypes;
using N3O.Umbraco.SerpEditor.ContentTypes;
using System;
using System.Linq.Expressions;

namespace N3O.Umbraco.SerpEditor.Extensions;

public static class PropertyContainerBuilderExtensions {
    public static SerpEditorPropertyTypeBuilder SerpEditor(this IPropertyContainerBuilder builder,
                                                           string propertyAlias) {
        return builder.Property<SerpEditorPropertyTypeBuilder>(propertyAlias);
    }

    public static SerpEditorPropertyTypeBuilder SerpEditor<T, TProperty>(this IPropertyContainerBuilder<T> builder,
                                                                         Expression<Func<T, TProperty>> expression) {
        return builder.Property<SerpEditorPropertyTypeBuilder, TProperty>(expression);
    }
}
