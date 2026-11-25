using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
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

    public static bool AnotherSiblingHasSameValue(this IContentHelper contentHelper,
                                                  ContentProperties content,
                                                  IContentProperty property) {
        var value = (property.Value as string).TrimOrNull();

        if (!value.HasValue()) {
            return false;
        }

        return contentHelper.GetAllOfType(content.ContentTypeAlias)
                            .Where(x => !x.Trashed && x.ParentId == content.ParentId && x.Key != content.Id)
                            .Any(x => x.GetValue<string>(property.Alias).TrimOrNull().EqualsInvariant(value));
    }

    public static IReadOnlyList<IContent> GetCrowdfundingCampaigns(this IContentHelper contentHelper) {
        return contentHelper.GetAllOfType(PlatformsConstants.CrowdfundingCampaigns.CrowdfundingCampaign.Alias)
                            .Where(x => !x.Trashed)
                            .ToList();
    }
}
