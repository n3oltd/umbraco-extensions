using N3O.Umbraco.Cloud.Platforms.Content;
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
    private readonly Lazy<IContentLocator> _contentLocator;
    private readonly IContentTypeService _contentTypeService;

    public CampaignSaved(Lazy<IContentEditor> contentEditor,
                         Lazy<IContentLocator> contentLocator,
                         IContentTypeService contentTypeService) {
        _contentEditor = contentEditor;
        _contentLocator = contentLocator;
        _contentTypeService = contentTypeService;
    }

    public Task HandleAsync(ContentSavedNotification notification, CancellationToken cancellationToken) {
        foreach (var content in notification.SavedEntities) {
            if (content.IsCampaign(_contentTypeService)) {
                SyncCrowdfunderNames(content.Key, content.Name);
            }
        }

        return Task.CompletedTask;
    }

    private void SyncCrowdfunderNames(Guid campaignKey, string campaignName) {
        var crowdfunders = _contentLocator.Value.All<CrowdfunderContent>(x => x.Campaign?.Id == campaignKey.ToString());

        foreach (var crowdfunder in crowdfunders) {
            if (!crowdfunder.Content().Name.EqualsInvariant(campaignName)) {
                var contentPublisher = _contentEditor.Value.ForExisting(crowdfunder.Key);

                contentPublisher.SetName(campaignName);

                if (crowdfunder.Content().IsPublished()) {
                    contentPublisher.SaveAndPublish();
                } else {
                    contentPublisher.SaveUnpublished();
                }
            }
        }
    }
}
