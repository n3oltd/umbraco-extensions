using N3O.Umbraco.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;

namespace N3O.Umbraco.Cloud.Platforms.Extensions;

public static class ContentHelperExtensions {
    public static bool AnotherCrowdfundingCampaignExistsFor(this IContentHelper contentHelper,
                                                            Guid crowdfundingCampaignKey,
                                                            Guid campaignKey) {
        return contentHelper.GetCrowdfundingCampaigns()
                            .Any(x => x.Key != crowdfundingCampaignKey && x.GetCampaignKey() == campaignKey);
    }

    public static IReadOnlyList<IContent> GetCrowdfundingCampaigns(this IContentHelper contentHelper) {
        return contentHelper.GetAllOfType(PlatformsConstants.CrowdfundingCampaigns.CrowdfundingCampaign.Alias)
                            .Where(x => !x.Trashed)
                            .ToList();
    }
}
