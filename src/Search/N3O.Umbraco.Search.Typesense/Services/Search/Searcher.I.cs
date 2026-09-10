using N3O.Umbraco.Search.Typesense.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using Typesense;

namespace N3O.Umbraco.Search.Typesense;

public interface ISearcher<TDocument> where TDocument : SearchDocument {
    Task<SearchResult<TDocument>> SearchAsync(SearchParameters searchParameters,
                                              CancellationToken cancellationToken = default);

    Task<SearchResult<TDocument>> SearchAsync<TSearch>(CancellationToken cancellationToken = default,
                                                       params object[] criteria)
        where TSearch : ITypesenseSearch<TDocument>;

    Task<SearchResult<TDocument>> SearchAsync(Action<ITypesenseSearchBuilder<TDocument>> buildSearchParameters,
                                              CancellationToken cancellationToken = default);

    Task<SearchResult<TDocument>> SearchAsync(Func<ITypesenseSearchBuilder<TDocument>, Task> buildSearchParametersAsync,
                                              CancellationToken cancellationToken = default);
}
