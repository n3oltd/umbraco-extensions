using N3O.Umbraco.Search.Typesense.Models;
using N3O.Umbraco.Search.Typesense.Queries;
using System;
using System.Linq.Expressions;

namespace N3O.Umbraco.Search.Typesense;

public interface ITypesenseClauseBuilder<T> where T : SearchDocument {
    ITypesenseClauseBuilder<T> And<TKey>(Expression<Func<T, TKey>> pathExpression,
                                         string expression,
                                         params Action<ParameterizedTypesenseText>[] filterActions);

    ITypesenseClauseBuilder<T> Or<TKey>(Expression<Func<T, TKey>> pathExpression,
                                        string expression,
                                        params Action<ParameterizedTypesenseText>[] filterActions);

    ITypesenseClauseBuilder<T> And(Action<ITypesenseClauseBuilder<T>> buildClause);

    ITypesenseClauseBuilder<T> Or(Action<ITypesenseClauseBuilder<T>> buildClause);

    ITypesenseClauseBuilder<T> And(ParameterizedTypesenseText other);

    ITypesenseClauseBuilder<T> Or(ParameterizedTypesenseText other);
}
