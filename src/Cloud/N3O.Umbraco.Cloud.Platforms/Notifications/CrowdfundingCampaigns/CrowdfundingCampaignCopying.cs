using N3O.Umbraco.Cloud.Platforms.Extensions;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class CrowdfundingCampaignCopying : INotificationAsyncHandler<ContentCopyingNotification> {
    public Task HandleAsync(ContentCopyingNotification notification, CancellationToken cancellationToken) {
        if (notification.Copy.IsCrowdfundingCampaign()) {
            notification.Copy.SetContentSyncStamp(null);
        }

        return Task.CompletedTask;
    }
}
