using N3O.Umbraco.Search.Typesense.Models;
using N3O.Umbraco.Search.Typesense.Queries;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Typesense;

namespace N3O.Umbraco.Search.Typesense;

public interface ITypesenseSearchBuilder<T> where T : SearchDocument {
    ITypesenseSearchBuilder<T> Compose<TSearch, TCriteria>(TCriteria criteria)
        where TSearch : ITypesenseSearch<T>;

    ITypesenseSearchBuilder<T> Compose<TSearch>(params object[] criteria)
        where TSearch : ITypesenseSearch<T>;

    ITypesenseSearchBuilder<T> Custom(Action<SearchParameters> action);

    ITypesenseSearchBuilder<T> Filter(ParameterizedTypesenseText parameterizedTypesenseText);

    ITypesenseSearchBuilder<T> Filter<TKey>(Expression<Func<T, TKey>> pathExpression,
                                            string expression,
                                            params Action<ParameterizedTypesenseText>[] filterActions);

    ITypesenseSearchBuilder<T> Filter(Action<ITypesenseClauseBuilder<T>> buildClause);

    ITypesenseSearchBuilder<T> Query<TField>(string text,
                                             params Expression<Func<T, TField>>[] fieldSelectors);

    ITypesenseSearchBuilder<T> Query(string text,
                                     params (Expression<Func<T, object>> field, int weight)[] weightedFields);

    ITypesenseSearchBuilder<T> OrderBy<TField>(Expression<Func<T, TField>> fieldSelector);

    ITypesenseSearchBuilder<T> OrderByDescending<TField>(Expression<Func<T, TField>> fieldSelector);

    ITypesenseSearchBuilder<T> ThenBy<TField>(Expression<Func<T, TField>> fieldSelector);

    ITypesenseSearchBuilder<T> ThenByDescending<TField>(Expression<Func<T, TField>> fieldSelector);

    Task<SearchParameters> BuildAsync();
}
