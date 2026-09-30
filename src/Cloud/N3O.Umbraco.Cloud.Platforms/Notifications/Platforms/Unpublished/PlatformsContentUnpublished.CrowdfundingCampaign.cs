using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Scheduler;
using Umbraco.Cms.Core.Models;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class CrowdfundingCampaignUnpublished : CloudContentUnpublished {
    public CrowdfundingCampaignUnpublished(ICloudUrl cloudUrl, IBackgroundJob backgroundJob)
        : base(cloudUrl, backgroundJob) { }

    protected override bool CanProcess(IContent content) {
        return content.IsCrowdfundingCampaign() && content.GetCampaignKey() != null;
    }

    protected override object GetBody(IContent content) {
        var req = new CrowdfundingCampaignWebhookBodyReq();
        req.CampaignId = content.GetCampaignKey()?.ToString();
        req.Action = WebhookSyncAction.Deactivate;

        return req;
    }

    protected override string HookId => PlatformsConstants.Webhooks.HookIds.CrowdfundingCampaigns;
}
