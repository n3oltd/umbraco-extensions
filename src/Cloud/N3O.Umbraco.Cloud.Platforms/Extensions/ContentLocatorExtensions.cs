using Flurl;
using N3O.Umbraco.Cloud.Platforms.Lookups;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using Slugify;

namespace N3O.Umbraco.Cloud.Platforms.Extensions;

public static class ContentLocatorExtensions {
    public static string GetCampaignPath(this IContentCache contentCache, ISlugHelper slugHelper, string name) {
        var campaignPage = contentCache.Special(PlatformsSpecialPages.Campaign);

        if (campaignPage.HasValue()) {
            var campaignSlug = slugHelper.GenerateSlug(name);
            var campaignUrl = new Url(campaignPage.RelativeUrl());

            campaignUrl.RemovePathSegment();
            campaignUrl.AppendPathSegment(campaignSlug);

            return campaignUrl;
        } else {
            return null;
        }
    }

    public static string GetCrowdfundingCampaignPath(this IContentCache contentCache,
                                                     ISlugHelper slugHelper,
                                                     string name) {
        var crowdfundingCampaignPage = contentCache.Special(PlatformsSpecialPages.CrowdfundingCampaign);

        if (crowdfundingCampaignPage.HasValue()) {
            var crowdfundingCampaignSlug = slugHelper.GenerateSlug(name);
            var crowdfundingCampaignUrl = new Url(crowdfundingCampaignPage.RelativeUrl());

            crowdfundingCampaignUrl.RemovePathSegment();
            crowdfundingCampaignUrl.AppendPathSegment(crowdfundingCampaignSlug);

            return crowdfundingCampaignUrl;
        } else {
            return null;
        }
    }

    public static string GetOfferingPath(this IContentCache contentCache,
                                         ISlugHelper slugHelper,
                                         string campaignName,
                                         string offeringName) {
        var campaignUrl = GetCampaignPath(contentCache, slugHelper, campaignName);

        if (campaignUrl.HasValue()) {
            var offeringSlug = slugHelper.GenerateSlug(offeringName);
            var offeringUrl = new Url(campaignUrl);

            offeringUrl.AppendPathSegment(offeringSlug);

            return offeringUrl;
        } else {
            return null;
        }
    }
}
