using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

namespace N3O.Umbraco.ContentTypes;

public class PropertyContainerBuilder : IPropertyContainerBuilder {
    private readonly IServiceProvider _serviceProvider;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly PropertyContainerBuilder _parent;
    private readonly List<PropertyContainerBuilder> _children = [];
    private readonly List<(string Alias, IPropertyTypeBuilder Builder)> _properties = [];

    public PropertyContainerBuilder(IServiceProvider serviceProvider,
                                    IShortStringHelper shortStringHelper,
                                    string name,
                                    bool isTab,
                                    PropertyContainerBuilder parent) {
        _serviceProvider = serviceProvider;
        _shortStringHelper = shortStringHelper;
        Name = name;
        IsTab = isTab;
        _parent = parent;
    }

    public IPropertyContainerBuilder Group(string name) {
        EnsureCanNest();

        var child = new PropertyContainerBuilder(_serviceProvider, _shortStringHelper, name, false, this);

        RegisterChild(child);

        return child;
    }

    public TBuilder Property<TBuilder>(string propertyAlias) where TBuilder : IPropertyTypeBuilder {
        var builder = _serviceProvider.GetRequiredService<TBuilder>();

        _properties.Add((propertyAlias, builder));

        return builder;
    }

    public string Alias => _parent == null
                           ? Name.ToSafeAlias(_shortStringHelper)
                           : $"{_parent.Alias}/{Name.ToSafeAlias(_shortStringHelper)}";

    public IReadOnlyList<PropertyContainerBuilder> Children => _children;
    public bool IsTab { get; }
    public string Name { get; }
    public IReadOnlyList<(string Alias, IPropertyTypeBuilder Builder)> Properties => _properties;

    protected void EnsureCanNest() {
        if (!IsTab) {
            throw new Exception($"Groups can only be nested under tabs so cannot add a group to {Name.Quote()}");
        }
    }

    protected void RegisterChild(PropertyContainerBuilder child) {
        _children.Add(child);
    }

    protected IServiceProvider ServiceProvider => _serviceProvider;
    protected IShortStringHelper ShortStringHelper => _shortStringHelper;
}

public class PropertyContainerBuilder<T> : PropertyContainerBuilder, IPropertyContainerBuilder<T> {
    public PropertyContainerBuilder(IServiceProvider serviceProvider,
                                    IShortStringHelper shortStringHelper,
                                    string name,
                                    bool isTab,
                                    PropertyContainerBuilder parent)
        : base(serviceProvider, shortStringHelper, name, isTab, parent) { }

    public new IPropertyContainerBuilder<T> Group(string name) {
        EnsureCanNest();

        var child = new PropertyContainerBuilder<T>(ServiceProvider, ShortStringHelper, name, false, this);

        RegisterChild(child);

        return child;
    }

    public TBuilder Property<TBuilder, TProperty>(Expression<Func<T, TProperty>> expression)
        where TBuilder : IPropertyTypeBuilder {
        return Property<TBuilder>(AliasHelper<T>.PropertyAlias(expression));
    }
}
