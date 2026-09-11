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
                if (variant.State == ContentSavedState.NotCreated) {
                    ShowCampaignOnly(variant);
                }

                SetUrl(notification, variant);
            }
        }

        return Task.CompletedTask;
    }

    private void ShowCampaignOnly(ContentVariantDisplay variant) {
        var campaignAlias = PlatformsConstants.CrowdfundingCampaigns.CrowdfundingCampaign.Properties.Campaign;

        variant.Name = PlatformsConstants.CrowdfundingCampaigns.CrowdfundingCampaign.NewContentName;

        var tabs = variant.Tabs.Where(x => HasProperty(x, campaignAlias)).ToList();

        foreach (var tab in tabs) {
            tab.Properties = tab.Properties.Where(x => x.Alias.EqualsInvariant(campaignAlias)).ToList();
        }

        variant.Tabs = tabs;
    }

    private bool HasProperty(Tab<ContentPropertyDisplay> tab, string alias) {
        return tab.Properties?.Any(x => x.Alias.EqualsInvariant(alias)) == true;
    }

    private void SetUrl(SendingContentNotification notification, ContentVariantDisplay variant) {
        if (variant.State == ContentSavedState.Published) {
            var path = _contentCache.Value.GetCrowdfundingCampaignPath(_slugHelper.Value, variant.Name);

            if (path.HasValue()) {
                notification.SetPlatformsUrls(_contentCache.Value, path);
            }
        }
    }
}
