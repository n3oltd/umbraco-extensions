using N3O.Umbraco.Content;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace N3O.Umbraco.Notifications;

public class FlushContentCacheHandlers :
    INotificationAsyncHandler<ContentCacheRefresherNotification>,
    INotificationAsyncHandler<LanguageCacheRefresherNotification> {
    private readonly IContentCache _contentCache;

    public FlushContentCacheHandlers(IContentCache contentCache) {
        _contentCache = contentCache;
    }

    public Task HandleAsync(ContentCacheRefresherNotification notification, CancellationToken cancellationToken) {
        _contentCache.Flush();

        return Task.CompletedTask;
    }

    public Task HandleAsync(LanguageCacheRefresherNotification notification, CancellationToken cancellationToken) {
        _contentCache.Flush();

        return Task.CompletedTask;
    }
}
