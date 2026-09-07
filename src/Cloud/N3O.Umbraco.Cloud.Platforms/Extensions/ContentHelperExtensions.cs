using N3O.Umbraco.Content;
using System.Collections.Generic;
using Umbraco.Cms.Core.Models;

namespace N3O.Umbraco.Cloud.Platforms.Extensions;

public static class ContentHelperExtensions {
    public static IReadOnlyList<IContent> GetCrowdfundingCampaigns(this IContentHelper contentHelper) {
        return contentHelper.GetAllOfType(PlatformsConstants.CrowdfundingCampaigns.CrowdfundingCampaign.Alias);
    }
}
