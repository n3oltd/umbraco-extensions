using Microsoft.AspNetCore.Mvc.Rendering;
using N3O.Umbraco.Cloud.Extensions;
using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Cloud.Platforms.Lookups;
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
using Umbraco.Extensions;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class CampaignSending : INotificationAsyncHandler<SendingContentNotification> {
    private readonly Lazy<IContentCache> _contentCache;
    private readonly Lazy<ISlugHelper> _slugHelper;
    
    public CampaignSending(Lazy<IContentCache> contentCache, Lazy<ISlugHelper> slugHelper) {
        _contentCache = contentCache;
        _slugHelper = slugHelper;
    }

    public Task HandleAsync(SendingContentNotification notification, CancellationToken cancellationToken) {
        var isCampaign = notification.Content
                                     .ContentTypeAlias
                                     .IsAnyOf(AliasHelper<StandardCampaignContent>.ContentTypeAlias(),
                                              AliasHelper<QurbaniCampaignContent>.ContentTypeAlias(),
                                              AliasHelper<RegularGivingCampaignContent>.ContentTypeAlias(),
                                              AliasHelper<ScheduledGivingCampaignContent>.ContentTypeAlias(),
                                              AliasHelper<TelethonCampaignContent>.ContentTypeAlias());

        if (isCampaign) {
            foreach (var variant in notification.Content.Variants) {
                SetEmbedCode(variant, notification.Content.Key.GetValueOrDefault());
                SetUrl(notification, variant);
            }
        }
        
        return Task.CompletedTask;
    }

    private void SetUrl(SendingContentNotification notification, ContentVariantDisplay variant) {
        if (variant.State == ContentSavedState.Published) {
            var campaignPath = _contentCache.Value.GetCampaignPath(_slugHelper.Value, variant.Name);

            if (campaignPath.HasValue()) {
                notification.SetPlatformsUrls(_contentCache.Value, campaignPath);
            }
        }
    }

    private void SetEmbedCode(ContentVariantDisplay variant, Guid contentId) {
        var donationButtonTag = new TagBuilder(ElementTypes.DonationButton.TagName);
        var donationFormTag = new TagBuilder(ElementTypes.DonationForm.TagName);
        var donationPopupTag = new TagBuilder(ElementTypes.DonationPopup.TagName);

        donationButtonTag.Attributes.Add("element-id", $"{contentId.ToString()}");
        donationFormTag.Attributes.Add("element-id", $"{contentId.ToString()}");
        donationPopupTag.Attributes.Add("element-id", $"{contentId.ToString()}");

        donationButtonTag.Attributes.Add("element-kind", ElementKind.DonationButtonCampaign.ToEnumString());
        donationFormTag.Attributes.Add("element-kind", ElementKind.DonationFormCampaign.ToEnumString());
        donationPopupTag.Attributes.Add("element-kind", ElementKind.DonationPopupCampaign.ToEnumString());
        
        var embedTab = variant.Tabs
                              .SingleOrDefault(x => x.Properties.OrEmpty().Any(y => y.Alias.IsAnyOf(AliasHelper<CampaignContent>.PropertyAlias(z => z.DonationFormEmbedCode),
                                                                                                    AliasHelper<CampaignContent>.PropertyAlias(z => z.DonationButtonEmbedCode))));
        
        var donationButtonEmbedProperty = GetProperty(embedTab, AliasHelper<CampaignContent>.PropertyAlias(x => x.DonationButtonEmbedCode));
        var donationFormTagEmbedProperty = GetProperty(embedTab, AliasHelper<CampaignContent>.PropertyAlias(x => x.DonationFormEmbedCode));
        var donationPopupEmbedProperty = GetProperty(embedTab, AliasHelper<CampaignContent>.PropertyAlias(x => x.DonationPopupEmbedCode));
        
        donationButtonEmbedProperty.IfNotNull(x => x.Value = donationButtonTag.ToHtmlString());
        donationFormTagEmbedProperty.IfNotNull(x => x.Value = donationFormTag.ToHtmlString());
        donationPopupEmbedProperty.IfNotNull(x => x.Value = donationPopupTag.ToHtmlString());
    }
    
    private ContentPropertyDisplay GetProperty(Tab<ContentPropertyDisplay> tab, string alias) {
        return tab?.Properties?.SingleOrDefault(x => x.Alias.EqualsInvariant(alias));
    }
}