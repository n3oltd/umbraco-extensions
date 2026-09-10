using N3O.Umbraco.Cloud.Content.Clients;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Content;
using N3O.Umbraco.Scheduler;
using System;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Extensions;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class FeedDeleted : PlatformsContentDeleted {
    private readonly Lazy<IContentLocator> _contentLocator;

    public FeedDeleted(ICloudUrl cloudUrl, IBackgroundJob backgroundJob, Lazy<IContentLocator> contentLocator)
        : base(cloudUrl, backgroundJob) {
        _contentLocator = contentLocator;
    }
    
    protected override bool CanProcess(IContent content) {
        return content.IsFeed();
    }

    protected override object GetBody(IContent content) {
        var feed = _contentLocator.Value.ById(content.Id);
        
        var req = new ContentCollectionWebhookBodyReq();
        req.Id = content.Key.ToString();
        req.ContentLibraryId = feed.Ancestors().Single(x => x.ContentType.Alias == PlatformsConstants.Feeds.Alias).Key.ToString();
        req.Action = WebhookSyncAction.Deactivate;

        return req;
    }

    protected override string HookId => PlatformsConstants.Webhooks.HookIds.ContentCollection;
}