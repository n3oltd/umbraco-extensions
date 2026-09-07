using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class CampaignSaved : INotificationAsyncHandler<ContentSavedNotification> {
    private readonly Lazy<IContentEditor> _contentEditor;
    private readonly IContentHelper _contentHelper;
    private readonly IContentTypeService _contentTypeService;

    public CampaignSaved(Lazy<IContentEditor> contentEditor,
                         IContentHelper contentHelper,
                         IContentTypeService contentTypeService) {
        _contentEditor = contentEditor;
        _contentHelper = contentHelper;
        _contentTypeService = contentTypeService;
    }

    public Task HandleAsync(ContentSavedNotification notification, CancellationToken cancellationToken) {
        foreach (var content in notification.SavedEntities) {
            if (content.IsCampaign(_contentTypeService)) {
                SyncCrowdfundingCampaignNames(content.Key, content.Name);
            }
        }

        return Task.CompletedTask;
    }

    private void SyncCrowdfundingCampaignNames(Guid campaignKey, string campaignName) {
        foreach (var crowdfundingCampaign in _contentHelper.GetCrowdfundingCampaigns()) {
            if (crowdfundingCampaign.GetCampaignKey() != campaignKey ||
                crowdfundingCampaign.Name.EqualsInvariant(campaignName)) {
                continue;
            }

            var contentPublisher = _contentEditor.Value.ForExisting(crowdfundingCampaign.Key);

            contentPublisher.SetName(campaignName);

            if (crowdfundingCampaign.Published) {
                contentPublisher.SaveAndPublish();
            } else {
                contentPublisher.SaveUnpublished();
            }
        }
    }
}
