using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using Slugify;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class EvictPlatformsPagesHandlers :
    INotificationAsyncHandler<ContentPublishedNotification>,
    INotificationAsyncHandler<ContentUnpublishedNotification> {
    private readonly ICdnClient _cdnClient;
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly Lazy<ISlugHelper> _slugHelper;

    public EvictPlatformsPagesHandlers(ICdnClient cdnClient,
                                       IContentService contentService,
                                       IContentTypeService contentTypeService,
                                       Lazy<ISlugHelper> slugHelper) {
        _cdnClient = cdnClient;
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _slugHelper = slugHelper;
    }

    public Task HandleAsync(ContentPublishedNotification notification, CancellationToken cancellationToken) {
        Evict(notification.PublishedEntities);

        return Task.CompletedTask;
    }

    public Task HandleAsync(ContentUnpublishedNotification notification, CancellationToken cancellationToken) {
        Evict(notification.UnpublishedEntities);

        return Task.CompletedTask;
    }

    private void Evict(IEnumerable<IContent> entities) {
        foreach (var content in entities) {
            if (content.IsCampaign(_contentTypeService)) {
                _cdnClient.EvictPlatformsPage(PublishedFileKinds.CampaignPage, GetSlug(content));
            } else if (content.IsOffering(_contentTypeService)) {
                var campaign = _contentService.GetParent(content.Id);

                if (campaign != null) {
                    _cdnClient.EvictPlatformsPage(PublishedFileKinds.OfferingPage, GetSlug(campaign), GetSlug(content));
                }
            } else if (content.IsCrowdfundingCampaign()) {
                _cdnClient.EvictPlatformsPage(PublishedFileKinds.CrowdfundingCampaignPage, GetSlug(content));
            }
        }
    }

    private string GetSlug(IContent content) {
        return _slugHelper.Value.GenerateSlug(content.Name);
    }
}
