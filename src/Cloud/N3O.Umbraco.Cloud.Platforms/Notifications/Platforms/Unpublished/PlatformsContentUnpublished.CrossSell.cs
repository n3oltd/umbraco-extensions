using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Scheduler;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class CrossSellUnpublished : CloudContentUnpublished {
    private readonly IContentTypeService _contentTypeService;

    public CrossSellUnpublished(ICloudUrl cloudUrl, IBackgroundJob backgroundJob, IContentTypeService contentTypeService)
        : base(cloudUrl, backgroundJob) {
        _contentTypeService = contentTypeService;
    }

    protected override bool CanProcess(IContent content) {
        return content.IsCrossSell(_contentTypeService);
    }

    protected override object GetBody(IContent content) {
        var crossSellReq = new CrossSellWebhookBodyReq();
        crossSellReq.Id = content.Key.ToString();
        crossSellReq.Action = WebhookSyncAction.Deactivate;

        return crossSellReq;
    }

    protected override string HookId => PlatformsConstants.Webhooks.HookIds.CrossSells;
}
