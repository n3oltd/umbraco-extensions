using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Scheduler;
using Umbraco.Cms.Core.Models;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class QurbaniSeasonUnpublished : CloudContentUnpublished {
    public QurbaniSeasonUnpublished(ICloudUrl cloudUrl, IBackgroundJob backgroundJob)
        : base(cloudUrl, backgroundJob) {
    }
    
    protected override bool CanProcess(IContent content) {
        return content.IsQurbaniSeasonContent();
    }

    protected override object GetBody(IContent content) {
        var req = new QurbaniSeasonWebhookBodyReq();
        req.Id = content.Key.ToString();
        req.Action = WebhookSyncAction.Deactivate;

        return req;
    }

    protected override string HookId => PlatformsConstants.Webhooks.HookIds.QurbaniSeason;
}
