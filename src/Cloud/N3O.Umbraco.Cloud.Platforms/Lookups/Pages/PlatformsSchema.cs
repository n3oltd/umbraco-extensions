using N3O.Umbraco.Cloud.Extensions;
using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Lookups;

namespace N3O.Umbraco.Cloud.Platforms.Lookups;

public class PlatformsSchema : NamedLookup {
    public PlatformsSchema(string id, string name) : base(id, name) { }
}

public class PlatformsSchemas : StaticLookupsCollection<PlatformsSchema> {
    public static readonly PlatformsSchema CampaignPage = new(PlatformsSystemSchema.Sys__campaignPage.ToEnumString(), "Campaign Page");
    public static readonly PlatformsSchema CrowdfunderPage = new(CrowdfundingSystemSchema.Sys__crowdfunderPage.ToEnumString(), "Crowdfunder Page");
    public static readonly PlatformsSchema CrowdfundingCampaignPage =
        new(CrowdfundingSystemSchema.Sys__crowdfundingCampaignPage.ToEnumString(), "Crowdfunding Campaign Page");
    public static readonly PlatformsSchema OfferingPage = new(PlatformsSystemSchema.Sys__offeringPage.ToEnumString(), "Offering Page");
}