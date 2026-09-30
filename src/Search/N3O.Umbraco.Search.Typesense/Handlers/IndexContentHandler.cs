using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Mediator;
using N3O.Umbraco.Search.Typesense.Commands;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Search.Typesense.Handlers;

public class IndexContentHandler : IRequestHandler<IndexContentCommand, None, None> {
    private readonly IContentLocator _contentLocator;
    private readonly IContentIndexer _contentIndexer;

    public IndexContentHandler(IContentLocator contentLocator, IContentIndexer contentIndexer) {
        _contentLocator = contentLocator;
        _contentIndexer = contentIndexer;
    }

    public async Task<None> Handle(IndexContentCommand req, CancellationToken cancellationToken) {
        var publishedContent = req.ContentId.Run(id => _contentLocator.ById(id), true);

        await _contentIndexer.IndexAsync(publishedContent);

        return None.Empty;
    }
}
