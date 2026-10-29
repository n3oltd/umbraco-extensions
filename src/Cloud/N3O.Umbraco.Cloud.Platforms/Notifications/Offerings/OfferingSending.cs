using Microsoft.AspNetCore.Mvc.Rendering;
using N3O.Umbraco.Cloud.Content.Clients;
using N3O.Umbraco.Cloud.Extensions;
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
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class OfferingSending : INotificationAsyncHandler<SendingContentNotification> {
    private readonly IContentTypeService _contentTypeService;
    private readonly Lazy<IContentCache> _contentCache;
    private readonly Lazy<IContentLocator> _contentLocator;
    private readonly Lazy<ISlugHelper> _slugHelper;
    
    public OfferingSending(IContentTypeService contentTypeService,
                           Lazy<IContentCache> contentCache,
                           Lazy<IContentLocator> contentLocator,
                           Lazy<ISlugHelper> slugHelper) {
        _contentTypeService = contentTypeService;
        _contentCache = contentCache;
        _contentLocator = contentLocator;
        _slugHelper = slugHelper;
    }

    public Task HandleAsync(SendingContentNotification notification, CancellationToken cancellationToken) {
        var isOffering = _contentTypeService.Get(notification.Content.ContentTypeKey)
                                            .CompositionAliases()
                                            .Contains(PlatformsConstants.Offerings.CompositionAlias, true);

        if (isOffering) {
            foreach (var variant in notification.Content.Variants) {
                SetEmbedCode(variant, notification.Content.Key.GetValueOrDefault());
                SetUrl( notification, variant);
            }
        }
        
        return Task.CompletedTask;
    }
    
    private void SetUrl(SendingContentNotification notification, ContentVariantDisplay variant) {
        if (variant.State == ContentSavedState.Published) {
            var campaign = _contentLocator.Value.ById(notification.Content.ParentId.GetValueOrThrow());

            if (campaign == null) {
                return;
            }

            var offeringPath = _contentCache.Value.GetOfferingPath(_slugHelper.Value, campaign.Name, variant.Name);

            if (offeringPath.HasValue()) {
                notification.SetPlatformsUrls(_contentCache.Value, offeringPath);
            }
        }
    }
    
    private void SetEmbedCode(ContentVariantDisplay variant, Guid contentId) {
        var donationButtonTag = new TagBuilder(Clients.PlatformsElementTagName.DonationButton.ToEnumString());
        var donationFormTag = new TagBuilder(Clients.PlatformsElementTagName.DonationForm.ToEnumString());
        var donationPopupTag = new TagBuilder(Clients.PlatformsElementTagName.DonationPopup.ToEnumString());

        donationButtonTag.Attributes.Add("element-id", $"{contentId.ToString()}");
        donationFormTag.Attributes.Add("element-id", $"{contentId.ToString()}");
        donationPopupTag.Attributes.Add("element-id", $"{contentId.ToString()}");

        donationButtonTag.Attributes.Add("element-kind", ElementKind.DonationButtonOffering.ToEnumString());
        donationFormTag.Attributes.Add("element-kind", ElementKind.DonationFormOffering.ToEnumString());
        donationPopupTag.Attributes.Add("element-kind", ElementKind.DonationPopupOffering.ToEnumString());
        
        var embedTab = variant.Tabs
                              .SingleOrDefault(x => x.Properties.OrEmpty().Any(y => y.Alias.IsAnyOf(AliasHelper<OfferingContent>.PropertyAlias(z => z.DonationFormEmbedCode),
                                                                                                    AliasHelper<OfferingContent>.PropertyAlias(z => z.DonationButtonEmbedCode))));
        
        var donationButtonEmbedProperty = GetProperty(embedTab, AliasHelper<OfferingContent>.PropertyAlias(x => x.DonationButtonEmbedCode));
        var donationFormTagEmbedProperty = GetProperty(embedTab, AliasHelper<OfferingContent>.PropertyAlias(x => x.DonationFormEmbedCode));
        var donationPopupEmbedProperty = GetProperty(embedTab, AliasHelper<OfferingContent>.PropertyAlias(x => x.DonationPopupEmbedCode));
        
        donationButtonEmbedProperty.IfNotNull(x => x.Value = donationButtonTag.ToHtmlString());
        donationFormTagEmbedProperty.IfNotNull(x => x.Value = donationFormTag.ToHtmlString());
        donationPopupEmbedProperty.IfNotNull(x => x.Value = donationPopupTag.ToHtmlString());
    }
    
    private ContentPropertyDisplay GetProperty(Tab<ContentPropertyDisplay> tab, string alias) {
        return tab?.Properties?.SingleOrDefault(x => x.Alias.EqualsInvariant(alias));
    }
}
