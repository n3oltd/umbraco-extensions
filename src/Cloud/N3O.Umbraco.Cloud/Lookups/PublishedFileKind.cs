using N3O.Umbraco.Lookups;

namespace N3O.Umbraco.Cloud.Lookups;

public class PublishedFileKind : Lookup {
    public PublishedFileKind(string id) : base(id) { }

    public string PathSegment => Id;
}

public class PublishedFileKinds : StaticLookupsCollection<PublishedFileKind> {
    public static readonly PublishedFileKind Campaign = new("campaign");
    public static readonly PublishedFileKind CampaignPage = new("campaignPage");
    public static readonly PublishedFileKind CrowdfunderPage = new("crowdfunderPage");
    public static readonly PublishedFileKind CrowdfundingCampaignPage = new("crowdfundingCampaignPage");
    public static readonly PublishedFileKind Offering = new("offering");
    public static readonly PublishedFileKind OfferingPage = new("offeringPage");
    public static readonly PublishedFileKind PageRedirect = new("pageRedirect");
    public static readonly PublishedFileKind Subscription = new("subscription");
}