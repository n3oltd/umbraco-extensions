using Humanizer;
using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Models;
using N3O.Umbraco.Exceptions;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Utilities;
using PlatformsPage = N3O.Umbraco.Cloud.Platforms.Models.PlatformsPage;

namespace N3O.Umbraco.Cloud.Platforms.Extensions;

public static class PlatformsPageExtensions {
    public static string AbsoluteUrl(this PlatformsPage page, IUrlBuilder urlBuilder) {
        if (!page.HasValue(x => x.Url)) {
            return null;
        }

        return page.Url.RebaseOnSiteRoot(urlBuilder);
    }

    public static string GetCampaignId(this PlatformsPage page) {
        if (page.Kind == PublishedFileKinds.CampaignPage) {
            return page.Content[nameof(PublishedCampaignPage.Campaign).Camelize()]?[nameof(PublishedCampaignPage.Campaign.Id).Camelize()]?.ToString();
        } else if (page.Kind == PublishedFileKinds.CrowdfunderPage) {
            return page.Content[nameof(PublishedCrowdfunderPage.Crowdfunder).Camelize()]?[nameof(PublishedCrowdfunderPage.Crowdfunder.CampaignId).Camelize()]?.ToString();
        } else if (page.Kind == PublishedFileKinds.CrowdfundingCampaignPage) {
            var campaign = nameof(PublishedCrowdfundingCampaignPage.CrowdfundingCampaign).Camelize();
            var campaignId = nameof(PublishedCrowdfundingCampaignPage.CrowdfundingCampaign.CampaignId).Camelize();

            return page.Content[campaign]?[campaignId]?.ToString();
        } else if (page.Kind == PublishedFileKinds.OfferingPage) {
            return page.Content[nameof(PublishedOfferingPage.Offering).Camelize()]?[nameof(PublishedOfferingPage.Offering.Campaign).Camelize()]?[nameof(PublishedOfferingPage.Offering.Campaign.Id).Camelize()]?.ToString();
        } else {
            throw UnrecognisedValueException.For(page.Kind);
        }
    }

    public static string GetOfferingId(this PlatformsPage page) {
        if (page.Kind == PublishedFileKinds.OfferingPage) {
            return page.Content[nameof(PublishedOfferingPage.Offering).Camelize()]?[nameof(PublishedOfferingPage.Offering.Id).Camelize()]?.ToString();
        } else {
            throw UnrecognisedValueException.For(page.Kind);
        }
    }
}