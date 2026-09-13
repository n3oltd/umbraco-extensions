using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using System;
using System.Linq;

namespace N3O.Umbraco.Cloud.Platforms.Validators;

public class CrowdfundingCampaignValidator : ContentValidator {
    public CrowdfundingCampaignValidator(IContentHelper contentHelper) : base(contentHelper) { }

    public override bool IsValidator(ContentProperties content) {
        var alias = PlatformsConstants.CrowdfundingCampaigns.CrowdfundingCampaign.Alias;

        return content.ContentTypeAlias.EqualsInvariant(alias);
    }

    public override void Validate(ContentProperties content) {
        var campaignKey = content.GetCampaignKey();

        if (campaignKey != null && AnotherCrowdfundingCampaignExistsFor(content.Id, campaignKey.Value)) {
            ErrorResult("This campaign already has a crowdfunding campaign");
        }
    }

    private bool AnotherCrowdfundingCampaignExistsFor(Guid crowdfundingCampaignKey, Guid campaignKey) {
        return ContentHelper.GetCrowdfundingCampaigns()
                            .Any(x => x.Key != crowdfundingCampaignKey &&
                                      !x.Trashed &&
                                      x.GetCampaignKey() == campaignKey);
    }
}
