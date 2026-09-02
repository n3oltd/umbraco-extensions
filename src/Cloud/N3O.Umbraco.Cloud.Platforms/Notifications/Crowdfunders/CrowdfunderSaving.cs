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

public class CrowdfunderSaving : INotificationAsyncHandler<ContentSavingNotification> {
    private const int PageSize = 100;

    private readonly IContentService _contentService;

    public CrowdfunderSaving(IContentService contentService) {
        _contentService = contentService;
    }

    public Task HandleAsync(ContentSavingNotification notification, CancellationToken cancellationToken) {
        foreach (var content in notification.SavedEntities) {
            if (!content.IsCrowdfunder()) {
                continue;
            }

            var campaignKey = content.GetCrowdfunderCampaignKey();

            if (campaignKey == null) {
                continue;
            }

            if (AnotherCrowdfunderRaisesFor(content, campaignKey.Value)) {
                notification.CancelWithError("A crowdfunder already exists for this campaign");

                continue;
            }

            var campaign = _contentService.GetById(campaignKey.Value);

            if (campaign != null) {
                content.Name = campaign.Name;
            }
        }

        return Task.CompletedTask;
    }

    // Read through the content service rather than the published cache, so an unpublished crowdfunder
    // still counts against the campaign.
    private bool AnotherCrowdfunderRaisesFor(IContent crowdfunder, Guid campaignKey) {
        for (var pageIndex = 0; true; pageIndex++) {
            var page = _contentService.GetPagedOfType(crowdfunder.ContentTypeId,
                                                      pageIndex,
                                                      PageSize,
                                                      out var totalRecords,
                                                      null);

            foreach (var other in page) {
                if (other.Key != crowdfunder.Key && other.GetCrowdfunderCampaignKey() == campaignKey) {
                    return true;
                }
            }

            if ((pageIndex + 1) * (long) PageSize >= totalRecords) {
                return false;
            }
        }
    }
}