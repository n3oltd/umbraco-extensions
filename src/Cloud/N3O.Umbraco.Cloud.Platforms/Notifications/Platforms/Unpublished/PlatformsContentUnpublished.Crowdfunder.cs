using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Content;
using N3O.Umbraco.Scheduler;
using Umbraco.Cms.Core.Models;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class CrowdfunderUnpublished : CloudContentUnpublished {
    private readonly IContentHelper _contentHelper;

    public CrowdfunderUnpublished(ICloudUrl cloudUrl, IBackgroundJob backgroundJob, IContentHelper contentHelper)
        : base(cloudUrl, backgroundJob) {
        _contentHelper = contentHelper;
    }

    protected override bool CanProcess(IContent content) {
        return content.IsCrowdfunder() && content.GetCrowdfunderCampaignKey(_contentHelper) != null;
    }

    protected override object GetBody(IContent content) {
        var req = new CrowdfundingCampaignWebhookBodyReq();
        req.CampaignId = content.GetCrowdfunderCampaignKey(_contentHelper)?.ToString();
        req.Action = WebhookSyncAction.Deactivate;

        return req;
    }

    protected override string HookId => PlatformsConstants.Webhooks.HookIds.CrowdfundingCampaigns;
}
