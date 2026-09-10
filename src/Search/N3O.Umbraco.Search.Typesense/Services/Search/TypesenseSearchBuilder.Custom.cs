using System;
using System.Collections.Generic;
using Typesense;

namespace N3O.Umbraco.Search.Typesense;

public partial class TypesenseSearchBuilder<T> {
    private readonly List<Action<SearchParameters>> _customActions = new();

    public ITypesenseSearchBuilder<T> Custom(Action<SearchParameters> action) {
        _customActions.Add(action);

        return this;
    }
}
