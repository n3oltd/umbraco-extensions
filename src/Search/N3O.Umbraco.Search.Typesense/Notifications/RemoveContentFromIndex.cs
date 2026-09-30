using N3O.Umbraco.Parameters;
using N3O.Umbraco.Scheduler;
using N3O.Umbraco.Scheduler.Extensions;
using N3O.Umbraco.Search.Typesense.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using ContentType = N3O.Umbraco.Parameters.ContentType;

namespace N3O.Umbraco.Search.Typesense.Notifications;

public class RemoveContentFromIndex : INotificationAsyncHandler<ContentUnpublishedNotification>,
                                      INotificationAsyncHandler<ContentMovedToRecycleBinNotification>,
                                      INotificationAsyncHandler<ContentDeletedNotification> {
    private readonly IEnumerable<ISearchIndexer> _searchIndexers;
    private readonly IBackgroundJob _backgroundJob;

    public RemoveContentFromIndex(IEnumerable<ISearchIndexer> searchIndexers, IBackgroundJob backgroundJob) {
        _searchIndexers = searchIndexers;
        _backgroundJob = backgroundJob;
    }

    public Task HandleAsync(ContentUnpublishedNotification notification, CancellationToken cancellationToken) {
        EnqueueRemoval(notification.UnpublishedEntities);

        return Task.CompletedTask;
    }

    public Task HandleAsync(ContentMovedToRecycleBinNotification notification, CancellationToken cancellationToken) {
        EnqueueRemoval(notification.MoveInfoCollection.Select(x => x.Entity));

        return Task.CompletedTask;
    }

    public Task HandleAsync(ContentDeletedNotification notification, CancellationToken cancellationToken) {
        EnqueueRemoval(notification.DeletedEntities);

        return Task.CompletedTask;
    }

    private void EnqueueRemoval(IEnumerable<IContent> entities) {
        foreach (var content in entities) {
            if (_searchIndexers.Any(x => x.CanIndex(content.ContentType.Alias))) {
                _backgroundJob.EnqueueCommand<RemoveContentCommand>(m => m.Add<ContentId>(content.Key.ToString())
                                                                          .Add<ContentType>(content.ContentType.Alias),
                                                                    content.Key);
            }
        }
    }
}
