using N3O.Umbraco.Cloud.Extensions;
using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Content;
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
using Umbraco.Extensions;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class CampaignEmbedCodes :
    INotificationAsyncHandler<ContentPublishingNotification>,
    INotificationAsyncHandler<ContentUnpublishedNotification> {
    private static readonly string DonationButtonEmbedCodeAlias = AliasHelper<CampaignContent>.PropertyAlias(x => x.DonationButtonEmbedCode);
    private static readonly string DonationFormEmbedCodeAlias = AliasHelper<CampaignContent>.PropertyAlias(x => x.DonationFormEmbedCode);
    private static readonly string DonationPopupEmbedCodeAlias = AliasHelper<CampaignContent>.PropertyAlias(x => x.DonationPopupEmbedCode);

    private readonly IContentTypeService _contentTypeService;
    private readonly IContentService _contentService;

    public CampaignEmbedCodes(IContentTypeService contentTypeService, IContentService contentService) {
        _contentTypeService = contentTypeService;
        _contentService = contentService;
    }

    public Task HandleAsync(ContentPublishingNotification notification, CancellationToken cancellationToken) {
        foreach (var content in notification.PublishedEntities) {
            if (content.IsCampaign(_contentTypeService)) {
                content.SetValue(DonationButtonEmbedCodeAlias, EmbedCode(PlatformsElementTagName.DonationButton.ToEnumString(), content.Key, ElementKind.DonationButtonCampaign));
                content.SetValue(DonationFormEmbedCodeAlias, EmbedCode(PlatformsElementTagName.DonationForm.ToEnumString(), content.Key, ElementKind.DonationFormCampaign));
                content.SetValue(DonationPopupEmbedCodeAlias, EmbedCode(PlatformsElementTagName.DonationPopup.ToEnumString(), content.Key, ElementKind.DonationPopupCampaign));
            }
        }

        return Task.CompletedTask;
    }

    public Task HandleAsync(ContentUnpublishedNotification notification, CancellationToken cancellationToken) {
        foreach (var content in notification.UnpublishedEntities) {
            if (content.IsCampaign(_contentTypeService)) {
                content.SetValue(DonationButtonEmbedCodeAlias, null);
                content.SetValue(DonationFormEmbedCodeAlias, null);
                content.SetValue(DonationPopupEmbedCodeAlias, null);

                _contentService.Save(content);
            }
        }

        return Task.CompletedTask;
    }

    private string EmbedCode(string tagName, Guid contentId, ElementKind elementKind) {
        return $"""<{tagName} element-id="{contentId}" element-kind="{elementKind.ToEnumString()}"></{tagName}>""";
    }
}
