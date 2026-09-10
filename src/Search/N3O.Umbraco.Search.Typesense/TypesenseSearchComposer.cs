using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using N3O.Umbraco.Composing;
using N3O.Umbraco.Extensions;
using System.Net;
using System.Net.Http;
using Typesense;
using Typesense.Setup;
using Umbraco.Cms.Core.DependencyInjection;

namespace N3O.Umbraco.Search.Typesense;

public class TypesenseSearchComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        builder.Services.AddSingleton<IConfigureOptions<Config>, TypesenseOptions>();
        builder.Services.AddTransient<IContentIndexer, ContentIndexer>();
        builder.Services.AddTransient(typeof(ISearchDocumentBuilder<>), typeof(SearchDocumentBuilder<>));
        builder.Services.AddTransient(typeof(ISearcher<>), typeof(Searcher<>));
        builder.Services.AddSingleton<ITypesenseJsonProvider, TypesenseJsonProvider>();
        builder.Services.AddTransient(typeof(ITypesenseSearchBuilder<>), typeof(TypesenseSearchBuilder<>));
        builder.Services.AddTransient<ITypesenseSearchFactory, TypesenseSearchFactory>();

        builder.Services.AddOpenApiDocument(TypesenseConstants.BackOfficeApiName);
        
        InitializeTypesenseCollections(builder);
        
        builder.Services
               .AddHttpClient(nameof(TypesenseClient), client => {
                   client.DefaultRequestVersion = HttpVersion.Version30;
               })
               .ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler {
                   AutomaticDecompression = DecompressionMethods.All
               });

        builder.Services.AddScoped<ITypesenseClient>(serviceProvider => {
            var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(TypesenseClient));
            var configOptions = serviceProvider.GetRequiredService<IOptionsSnapshot<Config>>();

            if (configOptions.Value.ApiKey.HasValue() && configOptions.Value.Nodes.HasAny()) {
                return new TypesenseClient(configOptions, httpClient);
            }

            return null;
        });
        
        RegisterAll(t => t.ImplementsInterface<ISearchIndexer>(),
                    t => builder.Services.AddTransient(typeof(ISearchIndexer), t));
    }

    private void InitializeTypesenseCollections(IUmbracoBuilder builder) {
        var collections = builder.Config.GetSection("Typesense").Get<TypesenseCollectionsOptions>();
        
        TypesenseCollections.Initialize(collections?.Collections);
    }
} 
