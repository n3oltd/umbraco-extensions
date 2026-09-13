using N3O.Umbraco.Cloud.Platforms.Extensions;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class CrowdfundingCampaignScaffolded : INotificationAsyncHandler<ContentScaffoldedNotification> {
    public Task HandleAsync(ContentScaffoldedNotification notification, CancellationToken cancellationToken) {
        if (notification.Scaffold.IsCrowdfundingCampaign()) {
            notification.Scaffold.SetContentSyncStamp(null);
        }

        return Task.CompletedTask;
    }
}
