using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Cloud.Platforms.Lookups;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Lookups;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class CrowdfundingCampaignSaving : INotificationAsyncHandler<ContentSavingNotification> {
    private readonly ICrowdfundingCampaignContentCopier _contentCopier;
    private readonly IContentHelper _contentHelper;

    [Obsolete("Delete me once the can-enable query is called from the regenerated crowdfunding client")]
    private readonly IContentLocator _contentLocator;

    private readonly IContentService _contentService;

    [Obsolete("Delete me once the can-enable query is called from the regenerated crowdfunding client")]
    private readonly ILookups _lookups;

    public CrowdfundingCampaignSaving(ICrowdfundingCampaignContentCopier contentCopier,
                                      IContentHelper contentHelper,
                                      IContentLocator contentLocator,
                                      IContentService contentService,
                                      ILookups lookups) {
        _contentCopier = contentCopier;
        _contentHelper = contentHelper;
        _contentLocator = contentLocator;
        _contentService = contentService;
        _lookups = lookups;
    }

    public Task HandleAsync(ContentSavingNotification notification, CancellationToken cancellationToken) {
        foreach (var content in notification.SavedEntities) {
            if (!content.IsCrowdfundingCampaign()) {
                continue;
            }

            var creating = !content.HasIdentity;
            var campaignKey = content.GetCampaignKey();

            // Cancelling stops the whole notification, not the one entity
            if (campaignKey == null) {
                if (creating) {
                    notification.CancelWithError("A campaign must be selected");

                    return Task.CompletedTask;
                }

                continue;
            }

            if (!CampaignAllowsCrowdfunding(campaignKey.Value)) {
                notification.CancelWithError("No offering allows crowdfunding for the selected campaign");

                return Task.CompletedTask;
            }

            if (AnotherCrowdfundingCampaignExistsFor(content, campaignKey.Value)) {
                notification.CancelWithError("This campaign already has a crowdfunding campaign");

                return Task.CompletedTask;
            }

            var campaign = _contentService.GetById(campaignKey.Value);

            if (campaign == null) {
                notification.CancelWithError("The selected campaign no longer exists");

                return Task.CompletedTask;
            }

            content.Name = campaign.Name;

            if (creating) {
                _contentCopier.CopyFromCampaign(content, campaign);
            }
        }

        return Task.CompletedTask;
    }

    [Obsolete("Delete me once the can-enable query is called from the regenerated crowdfunding client")]
    private bool CampaignAllowsCrowdfunding(Guid campaignKey) {
        var campaignId = campaignKey.ToString();
        var offeringIds = _lookups.GetAll<Offering>()
                                  .Where(x => x.CampaignId == campaignId)
                                  .Select(x => x.Id)
                                  .ToList();

        return _contentLocator.All(x => x.IsOffering(AliasHelper<OfferingContent>.ContentTypeAlias()))
                              .As<OfferingContent>()
                              .Where(x => offeringIds.Contains(x.Key.ToString()))
                              .Any(x => x.AllowCrowdfunding);
    }

    private bool AnotherCrowdfundingCampaignExistsFor(IContent crowdfundingCampaign, Guid campaignKey) {
        return _contentHelper.GetCrowdfundingCampaigns()
                             .Any(x => x.Key != crowdfundingCampaign.Key && x.GetCampaignKey() == campaignKey);
    }
}