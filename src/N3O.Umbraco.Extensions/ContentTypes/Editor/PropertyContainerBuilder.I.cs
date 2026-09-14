using System;
using System.Linq.Expressions;

namespace N3O.Umbraco.ContentTypes;

public interface IPropertyContainerBuilder {
    IPropertyContainerBuilder Group(string name);
    TBuilder Property<TBuilder>(string propertyAlias) where TBuilder : IPropertyTypeBuilder;
}

public interface IPropertyContainerBuilder<T> : IPropertyContainerBuilder {
    new IPropertyContainerBuilder<T> Group(string name);

    TBuilder Property<TBuilder, TProperty>(Expression<Func<T, TProperty>> expression)
        where TBuilder : IPropertyTypeBuilder;
}
