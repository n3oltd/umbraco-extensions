using N3O.Umbraco.Extensions;
using N3O.Umbraco.Search.Typesense.Models;
using System;
using System.Threading.Tasks;
using Typesense;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace N3O.Umbraco.Search.Typesense;

public abstract class SearchIndexer<TContent, TDocument> : ISearchIndexer
    where TContent : IPublishedContent
    where TDocument : SearchDocument, new() {
    private readonly ITypesenseClient _typesenseClient;
    private readonly ISearchDocumentBuilder<TDocument> _searchDocumentBuilder;
    private readonly IVariationContextAccessor _variationContextAccessor;

    protected SearchIndexer(ITypesenseClient typesenseClient,
                            ISearchDocumentBuilder<TDocument> searchDocumentBuilder,
                            IVariationContextAccessor variationContextAccessor) {
        _typesenseClient = typesenseClient;
        _searchDocumentBuilder = searchDocumentBuilder;
        _variationContextAccessor = variationContextAccessor;
    }
    
    public bool CanIndex(IPublishedContent content) {
        return content is TContent;
    }

    public bool CanIndex(string contentTypeAlias) {
        var collectionInfo = TypesenseHelper.GetCollection<TDocument>();

        return collectionInfo.ContentTypeAliases.OrEmpty().InvariantContains(contentTypeAlias);
    }

    public async Task DeleteAsync(Guid contentKey) {
        if (!_typesenseClient.HasValue()) {
            return;
        }

        var collectionInfo = TypesenseHelper.GetCollection<TDocument>();

        await _typesenseClient.DeleteDocuments(collectionInfo.Name.Resolve(), $"content_key:=`{contentKey}`");
    }

    public async Task IndexAsync(IPublishedContent content, string culture = null) {
        if (!_typesenseClient.HasValue()) {
            return;
        }

        if (culture.HasValue()) {
            _variationContextAccessor.VariationContext = new VariationContext(culture);
        }

        await ProcessContentAsync(_searchDocumentBuilder, (TContent) content);

        _searchDocumentBuilder.Set(searchDocument => {
            searchDocument.ContentKey = content.Key;
            searchDocument.Culture = culture;
            searchDocument.Id = SearchDocument.GetId(content.Key, culture);
        });

        var document = _searchDocumentBuilder.Build();
        var collectionInfo = TypesenseHelper.GetCollection<TDocument>();
        
        await _typesenseClient.UpsertDocument(collectionInfo.Name.Resolve(), document);
    }

    protected abstract Task ProcessContentAsync(ISearchDocumentBuilder<TDocument> builder, TContent content);
}