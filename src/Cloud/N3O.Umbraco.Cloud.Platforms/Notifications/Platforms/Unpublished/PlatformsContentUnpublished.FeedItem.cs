using N3O.Umbraco.Cloud.Content.Clients;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Scheduler;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class FeedItemUnpublished : CloudContentUnpublished {
    private readonly IContentTypeService _contentTypeService;

    public FeedItemUnpublished(ICloudUrl cloudUrl, IBackgroundJob backgroundJob, IContentTypeService contentTypeService)
        : base(cloudUrl, backgroundJob) {
        _contentTypeService = contentTypeService;
    }
    
    protected override bool CanProcess(IContent content) {
        return content.IsFeedItem(_contentTypeService);
    }

    protected override object GetBody(IContent content) {
        var req = new ManagedContentWebhookBodyReq();
        req.Id = content.Key.ToString();
        req.Action = WebhookSyncAction.Deactivate;

        return req;
    }

    protected override string HookId => PlatformsConstants.Webhooks.HookIds.ManagedContent;
}