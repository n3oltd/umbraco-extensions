using N3O.Umbraco.Extensions;
using N3O.Umbraco.Search.Typesense.Queries;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace N3O.Umbraco.Search.Typesense;

public partial class TypesenseSearchBuilder<T> {
    private readonly List<ITypesenseSearch<T>> _appliedSearches = new();
    private readonly List<ParameterizedTypesenseText> _filterTexts = new();

    public ITypesenseSearchBuilder<T> Compose<TSearch, TCriteria>(TCriteria criteria)
        where TSearch : ITypesenseSearch<T> {
        return Compose<TSearch>(criteria);
    }

    public ITypesenseSearchBuilder<T> Compose<TSearch>(params object[] criteria)
        where TSearch : ITypesenseSearch<T> {
        var search = _typesenseSearchFactory.GetSearch<T, TSearch>(criteria);

        _appliedSearches.Add(search);

        return this;
    }

    public ITypesenseSearchBuilder<T> Filter(ParameterizedTypesenseText parameterizedTypesenseText) {
        return Filter(parameterizedTypesenseText, null);
    }

    public ITypesenseSearchBuilder<T> Filter<TKey>(Expression<Func<T, TKey>> pathExpression,
                                                   string expression,
                                                   params Action<ParameterizedTypesenseText>[] filterActions) {
        var parameterizedSearch = ParameterizedTypesenseText.Create(pathExpression, expression);

        return Filter(parameterizedSearch, filterActions);
    }

    public ITypesenseSearchBuilder<T> Filter(Action<ITypesenseClauseBuilder<T>> buildClause) {
        var clauseBuilder = new TypesenseClauseBuilder<T>();

        buildClause(clauseBuilder);

        return Filter(clauseBuilder.Build());
    }

    private ITypesenseSearchBuilder<T> Filter(ParameterizedTypesenseText parameterizedTypesenseText,
                                              IEnumerable<Action<ParameterizedTypesenseText>> filterActions) {
        if ((parameterizedTypesenseText?.Text).HasValue()) {
            filterActions.Do(e => e?.Invoke(parameterizedTypesenseText));

            _filterTexts.Add(parameterizedTypesenseText);
        }

        return this;
    }
}
