using N3O.Umbraco.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace N3O.Umbraco.Cdn.Cloudflare.Notifications;

// Every server raises cache refresher notifications as it applies a change, unlike the publish notifications raised
// only by the server that publishes, so the last server to refresh purges last and nothing refills the edge from a
// server still serving the old content. The whole host's HTML is purged because shared blocks, navigation and
// listings put any node on many pages.
public class PurgeEdgeCacheHandlers :
    INotificationAsyncHandler<ContentCacheRefresherNotification>,
    INotificationAsyncHandler<DictionaryCacheRefresherNotification>,
    INotificationAsyncHandler<DomainCacheRefresherNotification>,
    INotificationAsyncHandler<MediaCacheRefresherNotification>,
    INotificationAsyncHandler<PublicAccessCacheRefresherNotification> {
    private readonly Lazy<EdgeCachePurger> _edgeCachePurger;

    public PurgeEdgeCacheHandlers(Lazy<EdgeCachePurger> edgeCachePurger) {
        _edgeCachePurger = edgeCachePurger;
    }

    public Task HandleAsync(ContentCacheRefresherNotification notification, CancellationToken cancellationToken) {
        return PurgeAsync();
    }

    public Task HandleAsync(DictionaryCacheRefresherNotification notification, CancellationToken cancellationToken) {
        return PurgeAsync();
    }

    public Task HandleAsync(DomainCacheRefresherNotification notification, CancellationToken cancellationToken) {
        return PurgeAsync();
    }

    public Task HandleAsync(MediaCacheRefresherNotification notification, CancellationToken cancellationToken) {
        return PurgeAsync();
    }

    public Task HandleAsync(PublicAccessCacheRefresherNotification notification, CancellationToken cancellationToken) {
        return PurgeAsync();
    }

    private Task PurgeAsync() {
        if (EdgeCaching.IsEnabled()) {
            _edgeCachePurger.Value.Schedule();
        }

        return Task.CompletedTask;
    }
}
