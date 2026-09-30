using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Search.Typesense.Models;
using System;

namespace N3O.Umbraco.Search.Typesense;

public class TypesenseSearchFactory : ITypesenseSearchFactory {
    private readonly IServiceProvider _serviceProvider;

    public TypesenseSearchFactory(IServiceProvider serviceProvider) {
        _serviceProvider = serviceProvider;
    }

    public TSearch GetSearch<TDocument, TSearch>(params object[] criteria)
        where TDocument : SearchDocument
        where TSearch : ITypesenseSearch<TDocument> {
        return (TSearch) ActivatorUtilities.CreateInstance(_serviceProvider, typeof(TSearch), criteria);
    }
}
