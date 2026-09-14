using N3O.Umbraco.Content;
using System;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Composing;

namespace N3O.Umbraco.ContentFinders;

public class FlushSpecialContentPathsComponent : IAsyncComponent {
    private readonly IContentCache _contentCache;

    public FlushSpecialContentPathsComponent(IContentCache contentCache) {
        _contentCache = contentCache;
    }

    public Task InitializeAsync(bool isRestarting, CancellationToken cancellationToken) {
        _contentCache.Flushed += ContentCacheOnFlushed;

        return Task.CompletedTask;
    }

    public Task TerminateAsync(bool isRestarting, CancellationToken cancellationToken) {
        _contentCache.Flushed -= ContentCacheOnFlushed;

        return Task.CompletedTask;
    }

    private void ContentCacheOnFlushed(object sender, EventArgs args) {
        SpecialContentPathParser.Flush();
    }
}
