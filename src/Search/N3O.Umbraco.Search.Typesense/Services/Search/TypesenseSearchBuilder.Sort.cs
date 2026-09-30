using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace N3O.Umbraco.Search.Typesense;

public partial class TypesenseSearchBuilder<T> {
    private const string Ascending = "asc";
    private const string Descending = "desc";

    private readonly List<(string Field, string Direction)> _orderBy = new();

    public ITypesenseSearchBuilder<T> OrderBy<TField>(Expression<Func<T, TField>> fieldSelector) {
        return ApplySort(fieldSelector, Ascending, false);
    }

    public ITypesenseSearchBuilder<T> OrderByDescending<TField>(Expression<Func<T, TField>> fieldSelector) {
        return ApplySort(fieldSelector, Descending, false);
    }

    public ITypesenseSearchBuilder<T> ThenBy<TField>(Expression<Func<T, TField>> fieldSelector) {
        return ApplySort(fieldSelector, Ascending, true);
    }

    public ITypesenseSearchBuilder<T> ThenByDescending<TField>(Expression<Func<T, TField>> fieldSelector) {
        return ApplySort(fieldSelector, Descending, true);
    }

    private ITypesenseSearchBuilder<T> ApplySort<TField>(Expression<Func<T, TField>> fieldSelector,
                                                         string direction,
                                                         bool preserveExistingSort) {
        if (!preserveExistingSort) {
            _orderBy.Clear();
        }

        var field = TypesenseField.Get(fieldSelector);

        _orderBy.Add((field, direction));

        return this;
    }

    private string GetSortBy() {
        if (_orderBy.Any()) {
            return _orderBy.Select(x => $"{x.Field}:{x.Direction}").ToCsv();
        } else {
            return null;
        }
    }
}
