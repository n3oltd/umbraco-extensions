using N3O.Umbraco.Extensions;
using N3O.Umbraco.Search.Typesense.Models;
using N3O.Umbraco.Search.Typesense.Queries;
using System;
using System.Threading.Tasks;
using Typesense;

namespace N3O.Umbraco.Search.Typesense;

public partial class TypesenseSearchBuilder<T> : ITypesenseSearchBuilder<T> where T : SearchDocument {
    private readonly ITypesenseSearchFactory _typesenseSearchFactory;
    private bool _built;

    public TypesenseSearchBuilder(ITypesenseSearchFactory typesenseSearchFactory) {
        _typesenseSearchFactory = typesenseSearchFactory;
    }

    public async Task<SearchParameters> BuildAsync() {
        if (_built) {
            throw new Exception("A search builder can only be built once");
        }

        _built = true;

        for (var i = 0; i < _appliedSearches.Count; i++) {
            await _appliedSearches[i].ApplyAsync(this);
        }

        var searchParameters = new SearchParameters(_queryText, _queryFields ?? string.Empty);

        searchParameters.SortBy = GetSortBy();

        var filterBy = ParameterizedTypesenseText.Empty();

        foreach (var filterText in _filterTexts) {
            filterBy.And(filterText);
        }

        if (!filterBy.IsEmpty) {
            searchParameters.FilterBy = filterBy.ToString();
        }

        _customActions.Do(x => x(searchParameters));

        return searchParameters;
    }
}
