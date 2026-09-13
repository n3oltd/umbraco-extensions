using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Extensions;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class CrowdfundingCampaignRollingBack :
    INotificationAsyncHandler<ContentRollingBackNotification>,
    INotificationAsyncHandler<ContentRolledBackNotification> {
    private const string ContentSyncStampKey = nameof(CrowdfundingCampaignRollingBack);

    private readonly IContentService _contentService;

    public CrowdfundingCampaignRollingBack(IContentService contentService) {
        _contentService = contentService;
    }

    public Task HandleAsync(ContentRollingBackNotification notification, CancellationToken cancellationToken) {
        if (notification.Entity.IsCrowdfundingCampaign()) {
            notification.State[ContentSyncStampKey] = notification.Entity.GetContentSyncStamp();
        }

        return Task.CompletedTask;
    }

    public Task HandleAsync(ContentRolledBackNotification notification, CancellationToken cancellationToken) {
        if (notification.State.TryGetValue(ContentSyncStampKey, out var value)) {
            var stamp = (string) value;

            if (!stamp.EqualsInvariant(notification.Entity.GetContentSyncStamp())) {
                notification.Entity.SetContentSyncStamp(stamp);

                _contentService.Save(notification.Entity);
            }
        }

        return Task.CompletedTask;
    }
}
