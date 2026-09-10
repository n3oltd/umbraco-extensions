using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Search.Typesense.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Typesense;

namespace N3O.Umbraco.Search.Typesense;

public class Searcher<TDocument> : ISearcher<TDocument> where TDocument : SearchDocument {
    private readonly ITypesenseClient _typesenseClient;
    private readonly ITypesenseJsonProvider _typesenseJsonProvider;
    private readonly ITypesenseSearchFactory _typesenseSearchFactory;
    private readonly IServiceProvider _serviceProvider;

    public Searcher(ITypesenseClient typesenseClient,
                    ITypesenseJsonProvider typesenseJsonProvider,
                    ITypesenseSearchFactory typesenseSearchFactory,
                    IServiceProvider serviceProvider) {
        _typesenseClient = typesenseClient;
        _typesenseJsonProvider = typesenseJsonProvider;
        _typesenseSearchFactory = typesenseSearchFactory;
        _serviceProvider = serviceProvider;
    }

    public async Task<SearchResult<TDocument>> SearchAsync(SearchParameters searchParameters,
                                                           CancellationToken cancellationToken = default) {
        if (!_typesenseClient.HasValue()) {
            throw new Exception("Typesense is not configured");
        }

        var collectionInfo = TypesenseHelper.GetCollection<TDocument>();

        var results = await _typesenseClient.Search<object>(collectionInfo.Name.Resolve(), searchParameters, cancellationToken);

        return ToTypedResults(results);
    }

    public Task<SearchResult<TDocument>> SearchAsync<TSearch>(CancellationToken cancellationToken = default,
                                                              params object[] criteria)
        where TSearch : ITypesenseSearch<TDocument> {
        return SearchAsync(async builder => {
            var search = _typesenseSearchFactory.GetSearch<TDocument, TSearch>(criteria);

            await search.ApplyAsync(builder);
        }, cancellationToken);
    }

    public Task<SearchResult<TDocument>> SearchAsync(Action<ITypesenseSearchBuilder<TDocument>> buildSearchParameters,
                                                     CancellationToken cancellationToken = default) {
        return SearchAsync(x => {
            buildSearchParameters(x);

            return Task.CompletedTask;
        }, cancellationToken);
    }

    public async Task<SearchResult<TDocument>> SearchAsync(Func<ITypesenseSearchBuilder<TDocument>, Task> buildSearchParametersAsync,
                                                           CancellationToken cancellationToken = default) {
        var searchBuilder = _serviceProvider.GetRequiredService<ITypesenseSearchBuilder<TDocument>>();

        await buildSearchParametersAsync(searchBuilder);

        var searchParameters = await searchBuilder.BuildAsync();

        return await SearchAsync(searchParameters, cancellationToken);
    }

    private SearchResult<TDocument> ToTypedResults(SearchResult<object> results) {
        var typedHits = results.Hits.Select(ToTypedHit).ToList();

        return new SearchResult<TDocument>(results.FacetCounts,
                                           results.Found,
                                           results.OutOf,
                                           results.Page,
                                           results.SearchTimeMs,
                                           results.TookMs,
                                           typedHits);
    }

    private Hit<TDocument> ToTypedHit(Hit<object> hit) {
        var typedHit = hit.Document.IfNotNull(x => _typesenseJsonProvider.DeserializeObject<TDocument>(x.ToString()));

        return new Hit<TDocument>(hit.Highlights, typedHit, hit.TextMatch, hit.VectorDistance, hit.GeoDistanceMeters);
    }
}
