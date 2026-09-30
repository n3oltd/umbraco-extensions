using N3O.Umbraco.Parameters;
using N3O.Umbraco.Scheduler;
using N3O.Umbraco.Scheduler.Extensions;
using N3O.Umbraco.Search.Typesense.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace N3O.Umbraco.Search.Typesense.Notifications;

public class IndexContentOnPublish : INotificationAsyncHandler<ContentPublishedNotification> {
    private readonly IEnumerable<ISearchIndexer> _searchIndexers;
    private readonly IBackgroundJob _backgroundJob;

    public IndexContentOnPublish(IEnumerable<ISearchIndexer> searchIndexers, IBackgroundJob backgroundJob) {
        _searchIndexers = searchIndexers;
        _backgroundJob = backgroundJob;
    }

    public Task HandleAsync(ContentPublishedNotification notification, CancellationToken cancellationToken) {
        foreach (var content in notification.PublishedEntities) {
            if (_searchIndexers.Any(x => x.CanIndex(content.ContentType.Alias))) {
                _backgroundJob.EnqueueCommand<IndexContentCommand>(m => m.Add<ContentId>(content.Key.ToString()),
                                                                   content.Key);
            }
        }

        return Task.CompletedTask;
    }
}