using Microsoft.Extensions.Logging;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class CrowdfundingCampaignRollingBack :
    INotificationAsyncHandler<ContentRollingBackNotification>,
    INotificationAsyncHandler<ContentRolledBackNotification> {
    private const string ContentSyncStampKey = nameof(CrowdfundingCampaignRollingBack);

    private readonly IContentService _contentService;
    private readonly ILogger<CrowdfundingCampaignRollingBack> _logger;

    public CrowdfundingCampaignRollingBack(IContentService contentService,
                                           ILogger<CrowdfundingCampaignRollingBack> logger) {
        _contentService = contentService;
        _logger = logger;
    }

    public Task HandleAsync(ContentRollingBackNotification notification, CancellationToken cancellationToken) {
        if (notification.Entity.IsCrowdfundingCampaign()) {
            notification.State[ContentSyncStampKey] = notification.Entity.GetContentSyncStamp();
        }

        return Task.CompletedTask;
    }

    public Task HandleAsync(ContentRolledBackNotification notification, CancellationToken cancellationToken) {
        if (notification.State.TryGetValue(ContentSyncStampKey, out var value)) {
            try {
                RestoreStamp(notification.Entity, (string) value);
            } catch (Exception ex) {
                _logger.LogError(ex,
                                 "Error restoring the sync stamp of crowdfunding campaign {CrowdfundingCampaignKey}",
                                 notification.Entity.Key);

                var message = $"The rolled back content of {notification.Entity.Name.Quote()} may be replaced the " +
                              "next time its campaign is published";

                notification.Messages.Add(new EventMessage("Warning", message, EventMessageType.Warning));
            }
        }

        return Task.CompletedTask;
    }

    private void RestoreStamp(IContent crowdfundingCampaign, string stamp) {
        if (stamp.EqualsInvariant(crowdfundingCampaign.GetContentSyncStamp())) {
            return;
        }

        crowdfundingCampaign.SetContentSyncStamp(stamp);

        var result = _contentService.Save(crowdfundingCampaign);

        if (!result.Success) {
            throw new Exception($"Saving crowdfunding campaign {crowdfundingCampaign.Key} failed: {result.Result}");
        }
    }
}
