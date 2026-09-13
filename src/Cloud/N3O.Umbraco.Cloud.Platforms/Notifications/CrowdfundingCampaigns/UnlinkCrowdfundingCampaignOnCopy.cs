using N3O.Umbraco.Cloud.Platforms.Extensions;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class UnlinkCrowdfundingCampaignOnCopy : INotificationAsyncHandler<ContentCopyingNotification> {
    public Task HandleAsync(ContentCopyingNotification notification, CancellationToken cancellationToken) {
        var alias = PlatformsConstants.CrowdfundingCampaigns.CrowdfundingCampaign.Properties.Campaign;

        if (notification.Copy.IsCrowdfundingCampaign() && CanUnlink(notification.Copy, alias)) {
            notification.Copy.SetValue(alias, null);
        }

        return Task.CompletedTask;
    }

    private bool CanUnlink(IContent content, string alias) {
        var property = content.HasProperty(alias) ? content.Properties[alias] : null;

        return property != null && !property.PropertyType.VariesByCulture();
    }
}
