using N3O.Umbraco.Mediator;
using N3O.Umbraco.Scheduler.Attributes;
using N3O.Umbraco.Search.Commands;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Search.Handlers;

[RecurringJob("Generate Sitemap", "0 * * * *")]
public class GenerateSitemapHandler : IRequestHandler<GenerateSitemapCommand, None, None> {
    private readonly ISitemap _sitemap;

    public GenerateSitemapHandler(ISitemap sitemap) {
        _sitemap = sitemap;
    }

    public async Task<None> Handle(GenerateSitemapCommand req, CancellationToken cancellationToken) {
        await _sitemap.PublishAsync();
        
        return None.Empty;
    }
}