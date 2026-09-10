using N3O.Umbraco.Extensions;
using N3O.Umbraco.Search.Typesense.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace N3O.Umbraco.Search.Typesense;

public class SearchDocumentBuilder<T> : ISearchDocumentBuilder<T> where T : SearchDocument, new() {
    private readonly List<Action<T>> _actions = [];
    
    private readonly ITypesenseJsonProvider _typesenseJsonProvider;

    public SearchDocumentBuilder(ITypesenseJsonProvider typesenseJsonProvider) {
        _typesenseJsonProvider = typesenseJsonProvider;
    }
    
    public ISearchDocumentBuilder<T> Set(Action<T> action) {
        _actions.Add(action);

        return this;
    }

    public object Build() {
        var searchDocument = new T();
        
        _actions.Do(x => x(searchDocument));
        _actions.Clear();

        return TransformObject(searchDocument);
    }

    private object TransformObject(T document) {
        var serializer = JsonSerializer.Create(_typesenseJsonProvider.GetSettings());

        return JObject.FromObject(document, serializer).ConvertToObject();
    }
}