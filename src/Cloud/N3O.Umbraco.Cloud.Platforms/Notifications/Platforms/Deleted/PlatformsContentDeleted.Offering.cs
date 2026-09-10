using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Scheduler;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class OfferingDeleted : PlatformsContentDeleted {
    private readonly IContentTypeService _contentTypeService;

    public OfferingDeleted(ICloudUrl cloudUrl, IBackgroundJob backgroundJob, IContentTypeService contentTypeService) 
        : base(cloudUrl, backgroundJob) {
        _contentTypeService = contentTypeService;
    }
    
    protected override bool CanProcess(IContent content) {
        return content.IsOffering(_contentTypeService);
    }

    protected override object GetBody(IContent content) {
        var offeringReq = new OfferingWebhookBodyReq();
        offeringReq.Id = content.Key.ToString();
        offeringReq.Action = WebhookSyncAction.Deactivate;

        return offeringReq;
    }

    protected override string HookId => PlatformsConstants.Webhooks.HookIds.Offerings;
}