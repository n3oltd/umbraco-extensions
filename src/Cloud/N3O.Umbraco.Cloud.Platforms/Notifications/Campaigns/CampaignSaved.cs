using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class CampaignSaved : INotificationAsyncHandler<ContentSavedNotification> {
    private readonly ICrowdfundingCampaignContentCopier _contentCopier;
    private readonly Lazy<IContentEditor> _contentEditor;
    private readonly IContentHelper _contentHelper;
    private readonly IContentTypeService _contentTypeService;

    public CampaignSaved(ICrowdfundingCampaignContentCopier contentCopier,
                         Lazy<IContentEditor> contentEditor,
                         IContentHelper contentHelper,
                         IContentTypeService contentTypeService) {
        _contentCopier = contentCopier;
        _contentEditor = contentEditor;
        _contentHelper = contentHelper;
        _contentTypeService = contentTypeService;
    }

    public Task HandleAsync(ContentSavedNotification notification, CancellationToken cancellationToken) {
        foreach (var content in notification.SavedEntities) {
            if (content.IsCampaign(_contentTypeService)) {
                SyncCrowdfundingCampaigns(content);
            }
        }

        return Task.CompletedTask;
    }

    private void SyncCrowdfundingCampaigns(IContent campaign) {
        foreach (var crowdfundingCampaign in _contentHelper.GetCrowdfundingCampaigns()) {
            if (crowdfundingCampaign.GetCampaignKey() != campaign.Key) {
                continue;
            }

            var renaming = !crowdfundingCampaign.Name.EqualsInvariant(campaign.Name);
            var updates = _contentCopier.GetUpdatesFromCampaign(crowdfundingCampaign, campaign);

            if (!renaming && updates.None()) {
                continue;
            }

            var contentPublisher = _contentEditor.Value.ForExisting(crowdfundingCampaign.Key);

            if (renaming) {
                contentPublisher.SetName(campaign.Name);
            }

            foreach (var (propertyAlias, value) in updates) {
                contentPublisher.Content.Raw(propertyAlias).Set(value);
            }

            if (crowdfundingCampaign.Published) {
                contentPublisher.SaveAndPublish();
            } else {
                contentPublisher.SaveUnpublished();
            }
        }
    }
}
