using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Scheduler;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class CampaignUnpublished : CloudContentUnpublished {
    private readonly IContentTypeService _contentTypeService;

    public CampaignUnpublished(ICloudUrl cloudUrl, IBackgroundJob backgroundJob, IContentTypeService contentTypeService)
        : base(cloudUrl, backgroundJob) {
        _contentTypeService = contentTypeService;
    }
    
    protected override bool CanProcess(IContent content) {
        return content.IsCampaign(_contentTypeService);
    }

    protected override object GetBody(IContent content) {
        var campaignReq = new CampaignWebhookBodyReq();
        campaignReq.Id = content.Key.ToString();
        campaignReq.Action = WebhookSyncAction.Deactivate;

        return campaignReq;
    }

    protected override string HookId => PlatformsConstants.Webhooks.HookIds.Campaigns;
}