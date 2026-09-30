using N3O.Umbraco.Extensions;
using N3O.Umbraco.Search.Typesense.Models;
using N3O.Umbraco.Search.Typesense.Queries;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace N3O.Umbraco.Search.Typesense;

public class TypesenseClauseBuilder<T> : ITypesenseClauseBuilder<T> where T : SearchDocument {
    private readonly ParameterizedTypesenseText _clause = ParameterizedTypesenseText.Empty();

    public ITypesenseClauseBuilder<T> And<TKey>(Expression<Func<T, TKey>> pathExpression,
                                                string expression,
                                                params Action<ParameterizedTypesenseText>[] filterActions) {
        _clause.And(BuildLeaf(pathExpression, expression, filterActions));

        return this;
    }

    public ITypesenseClauseBuilder<T> Or<TKey>(Expression<Func<T, TKey>> pathExpression,
                                               string expression,
                                               params Action<ParameterizedTypesenseText>[] filterActions) {
        _clause.Or(BuildLeaf(pathExpression, expression, filterActions));

        return this;
    }

    public ITypesenseClauseBuilder<T> And(Action<ITypesenseClauseBuilder<T>> buildClause) {
        _clause.And(BuildNested(buildClause));

        return this;
    }

    public ITypesenseClauseBuilder<T> Or(Action<ITypesenseClauseBuilder<T>> buildClause) {
        _clause.Or(BuildNested(buildClause));

        return this;
    }

    public ITypesenseClauseBuilder<T> And(ParameterizedTypesenseText other) {
        _clause.And(other);

        return this;
    }

    public ITypesenseClauseBuilder<T> Or(ParameterizedTypesenseText other) {
        _clause.Or(other);

        return this;
    }

    public ParameterizedTypesenseText Build() {
        return _clause;
    }

    private ParameterizedTypesenseText BuildLeaf<TKey>(Expression<Func<T, TKey>> pathExpression,
                                                       string expression,
                                                       IEnumerable<Action<ParameterizedTypesenseText>> filterActions) {
        var leaf = ParameterizedTypesenseText.Create(pathExpression, expression);

        filterActions.Do(e => e?.Invoke(leaf));

        return leaf;
    }

    private ParameterizedTypesenseText BuildNested(Action<ITypesenseClauseBuilder<T>> buildClause) {
        var inner = new TypesenseClauseBuilder<T>();

        buildClause(inner);

        return inner.Build();
    }
}
