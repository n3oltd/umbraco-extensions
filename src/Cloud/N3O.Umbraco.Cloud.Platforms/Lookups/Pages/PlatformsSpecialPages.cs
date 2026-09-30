using N3O.Umbraco.Content;

namespace N3O.Umbraco.Cloud.Platforms.Lookups;

public class PlatformsSpecialPages : ISpecialContents {
    public static readonly SpecialContent Campaign = new("campaignPage", "Campaign Page", "campaignPage");
    public static readonly SpecialContent Crowdfunder = new("crowdfunderPage", "Crowdfunder Page", "crowdfunderPage");
    public static readonly SpecialContent CrowdfundingCampaign = new("crowdfundingCampaignPage", "Crowdfunding Campaign Page", "crowdfundingCampaignPage");
    public static readonly SpecialContent Offering = new("offeringPage", "Offering Page", "offeringPage");

    public static readonly SpecialContent[] All = [Campaign, Crowdfunder, CrowdfundingCampaign, Offering];
}
