using Microsoft.Extensions.Logging;
using N3O.Umbraco.Cloud.Content.Clients;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Scheduler;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class FeedsPublished : CloudContentPublished {
    public FeedsPublished(ICloudUrl cloudUrl, IBackgroundJob backgroundJob, ILogger<FeedsPublished> logger)
        : base(cloudUrl, backgroundJob, logger) { }

    protected override bool CanProcess(IContent content) {
        return content.IsFeeds();
    }
    
    protected override Task<object> GetBodyAsync(IContent content) {
        var req = new ContentLibraryWebhookBodyReq();
        req.Id = content.Key.ToString();
        req.Action = WebhookSyncAction.AddOrUpdate;
        req.AddOrUpdate = new ContentLibraryReq();
        req.AddOrUpdate.Name = content.Name;

        return Task.FromResult<object>(req);
    }

    protected override string HookId => PlatformsConstants.Webhooks.HookIds.ContentLibrary;
}