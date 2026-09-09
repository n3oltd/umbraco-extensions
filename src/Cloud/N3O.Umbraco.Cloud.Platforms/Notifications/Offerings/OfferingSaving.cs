using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Cloud.Platforms.Lookups;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class OfferingSaving : INotificationAsyncHandler<ContentSavingNotification> {
    private readonly Lazy<IContentLocator> _contentLocator;
    private readonly IContentTypeService _contentTypeService;
    
    public OfferingSaving(Lazy<IContentLocator> contentLocator, IContentTypeService contentTypeService) {
        _contentLocator = contentLocator;
        _contentTypeService = contentTypeService;
    }

    public Task HandleAsync(ContentSavingNotification notification, CancellationToken cancellationToken) {
        foreach (var content in notification.SavedEntities) {
            if (content.IsOffering(_contentTypeService)) {
                var campaign = _contentLocator.Value.ById(content.ParentId).As<CampaignContent>();

                if (campaign.Type == CampaignTypes.Telethon) {
                    var endAt = campaign.Telethon.EndAt;

                    if (DateTime.Now > endAt) {
                        notification.CancelWithError("The telethon campaign has ended, an offering cannot be created or updated");
                    }
                }
            }
        }
        
        return Task.CompletedTask;
    }
}
