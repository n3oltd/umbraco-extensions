using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using Slugify;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Notifications;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class CrowdfundingCampaignSending : INotificationAsyncHandler<SendingContentNotification> {
    private readonly Lazy<IContentCache> _contentCache;
    private readonly Lazy<ISlugHelper> _slugHelper;

    public CrowdfundingCampaignSending(Lazy<IContentCache> contentCache, Lazy<ISlugHelper> slugHelper) {
        _contentCache = contentCache;
        _slugHelper = slugHelper;
    }

    public Task HandleAsync(SendingContentNotification notification, CancellationToken cancellationToken) {
        var alias = AliasHelper<CrowdfundingCampaignContent>.ContentTypeAlias();

        if (notification.Content.ContentTypeAlias.EqualsInvariant(alias)) {
            foreach (var variant in notification.Content.Variants) {
                HideContentSyncStamp(variant);

                if (IsCreating(notification.Content)) {
                    ShowCampaignOnly(variant);
                }

                SetUrl(notification, variant);
            }
        }

        return Task.CompletedTask;
    }

    private bool HasValue(ContentPropertyDisplay property) {
        return property.Value != null && (property.Value is not string text || text.HasValue());
    }

    private void HideContentSyncStamp(ContentVariantDisplay variant) {
        var stampAlias = PlatformsConstants.CrowdfundingCampaigns.CrowdfundingCampaign.Properties.ContentSyncStamp;

        foreach (var tab in variant.Tabs.OrEmpty()) {
            tab.Properties = tab.Properties.OrEmpty()
                                           .Where(x => !x.Alias.EqualsInvariant(stampAlias))
                                           .ToList();
        }
    }

    // A variant reports NotCreated for a language not yet added to a saved node
    private bool IsCreating(ContentItemDisplay content) {
        return content.Id is int id && id <= 0;
    }

    private void SetUrl(SendingContentNotification notification, ContentVariantDisplay variant) {
        if (variant.State == ContentSavedState.Published) {
            var path = _contentCache.Value.GetCrowdfundingCampaignPath(_slugHelper.Value, variant.Name);

            if (path.HasValue()) {
                notification.SetPlatformsUrls(_contentCache.Value, path);
            }
        }
    }

    private void ShowCampaignOnly(ContentVariantDisplay variant) {
        var campaignAlias = PlatformsConstants.CrowdfundingCampaigns.CrowdfundingCampaign.Properties.Campaign;
        var properties = variant.Tabs.SelectMany(x => x.Properties.OrEmpty()).ToList();
        var campaign = properties.SingleOrDefault(x => x.Alias.EqualsInvariant(campaignAlias));

        if (campaign == null || properties.Any(x => x != campaign && HasValue(x))) {
            return;
        }

        if (!variant.Name.HasValue()) {
            variant.Name = PlatformsConstants.CrowdfundingCampaigns.CrowdfundingCampaign.NewContentName;
        }

        var tabs = variant.Tabs.Where(x => x.Properties.HasAny(y => y.Alias.EqualsInvariant(campaignAlias)))
                               .ToList();

        foreach (var tab in tabs) {
            tab.Properties = tab.Properties.Where(x => x.Alias.EqualsInvariant(campaignAlias)).ToList();
        }

        variant.Tabs = tabs;
    }
}
