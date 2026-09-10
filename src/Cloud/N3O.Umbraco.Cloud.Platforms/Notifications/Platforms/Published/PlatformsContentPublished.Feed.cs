using Microsoft.Extensions.Logging;
using N3O.Umbraco.Cloud.Content.Clients;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Content;
using N3O.Umbraco.Scheduler;
using System;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models;
using Umbraco.Extensions;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class FeedPublished : CloudContentPublished {
    private readonly Lazy<IContentLocator> _contentLocator;

    public FeedPublished(ICloudUrl cloudUrl,
                         IBackgroundJob backgroundJob,
                         Lazy<IContentLocator> contentLocator,
                         ILogger<FeedPublished> logger)
        : base(cloudUrl, backgroundJob, logger) {
        _contentLocator = contentLocator;
    }
    
    protected override bool CanProcess(IContent content) {
        return content.IsFeed();
    }

    protected override Task<object> GetBodyAsync(IContent content) {
        var feed = _contentLocator.Value.ById(content.Id);
        
        var req = new ContentCollectionWebhookBodyReq();
        req.Id = content.Key.ToString();
        req.ContentLibraryId = feed.Ancestors().Single(x => x.ContentType.Alias == PlatformsConstants.Feeds.Alias).Key.ToString();
        req.Action = WebhookSyncAction.AddOrUpdate;
        req.AddOrUpdate = new ContentCollectionReq();
        req.AddOrUpdate.Name = content.Name;

        return Task.FromResult<object>(req);
    }

    protected override string HookId => PlatformsConstants.Webhooks.HookIds.ContentCollection;
}