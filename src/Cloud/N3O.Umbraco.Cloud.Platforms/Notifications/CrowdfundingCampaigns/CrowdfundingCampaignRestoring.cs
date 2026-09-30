using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class CrowdfundingCampaignRestoring : INotificationAsyncHandler<ContentMovingNotification> {
    private readonly IContentHelper _contentHelper;

    public CrowdfundingCampaignRestoring(IContentHelper contentHelper) {
        _contentHelper = contentHelper;
    }

    public Task HandleAsync(ContentMovingNotification notification, CancellationToken cancellationToken) {
        var restoring = notification.MoveInfoCollection.Select(x => x.Entity).Where(x => x.Trashed).ToList();

        if (restoring.Any(RestoresTakenCampaign)) {
            var message = PlatformsConstants.CrowdfundingCampaigns.CrowdfundingCampaign.CampaignTakenError;

            notification.CancelWithError(message);
        }

        return Task.CompletedTask;
    }

    private bool IsCampaignTaken(IContent crowdfundingCampaign) {
        var campaignKey = crowdfundingCampaign.GetCampaignKey();

        return campaignKey != null &&
               _contentHelper.AnotherCrowdfundingCampaignExistsFor(crowdfundingCampaign.Key, campaignKey.Value);
    }

    private bool RestoresTakenCampaign(IContent content) {
        return _contentHelper.GetDescendants(content)
                             .Concat(content)
                             .Where(x => x.IsCrowdfundingCampaign())
                             .Any(IsCampaignTaken);
    }
}
