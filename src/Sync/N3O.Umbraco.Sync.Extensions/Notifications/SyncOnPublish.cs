using N3O.Umbraco.Attributes;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Scheduler;
using N3O.Umbraco.Scheduler.Extensions;
using N3O.Umbraco.Sync.Extensions.Attributes;
using N3O.Umbraco.Sync.Extensions.Commands;
using N3O.Umbraco.Sync.Extensions.Models;
using N3O.Umbraco.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Notifications;

namespace N3O.Umbraco.Sync.Extensions.Notifications;

[SkipDuringSync]
public class SyncOnPublish : INotificationAsyncHandler<ContentPublishedNotification> {
    private static readonly IReadOnlyDictionary<string, string> ContentTypeServerMap; 
    
    private readonly Lazy<IBackgroundJob> _backgroundJob;
    
    static SyncOnPublish() {
        ContentTypeServerMap = OurAssemblies.GetTypes(t => t.IsSubclassOfType(typeof(PublishedContentModel)) &&
                                                           t.HasAttribute<SyncOnPublishAttribute>() &&
                                                           t.HasAttribute<PublishedModelAttribute>())
                                            .ToDictionary(x => x.GetCustomAttribute<PublishedModelAttribute>().ContentTypeAlias,
                                                          x => x.GetCustomAttribute<SyncOnPublishAttribute>().ServerAlias,
                                                          StringComparer.InvariantCultureIgnoreCase);
    }

    public SyncOnPublish(Lazy<IBackgroundJob> backgroundJob) {
        _backgroundJob = backgroundJob;
    }
    
    public Task HandleAsync(ContentPublishedNotification notification, CancellationToken cancellationToken) {
        foreach (var content in notification.PublishedEntities) {
            if (ContentTypeServerMap.ContainsKey(content.ContentType.Alias)) {
                var req = new SyncContentReq();
                req.ContentId = content.Key;
                req.ServerAlias = ContentTypeServerMap[content.ContentType.Alias];
                
                _backgroundJob.Value.EnqueueCommand<SyncContentCommand, SyncContentReq>(req);
            }
        }

        return Task.CompletedTask;
    }
}