using N3O.Umbraco.Lookups;

namespace N3O.Umbraco.Cloud.Lookups;

public class SubscriptionFile : Lookup {
    public SubscriptionFile(string id) : base(id) { }

    public string Filename => $"{Id}.json";
}

public class SubscriptionFiles : StaticLookupsCollection<SubscriptionFile> {
    /*TODO Need to update these*/
    public static readonly SubscriptionFile Campaigns = new("campaigns");
    public static readonly SubscriptionFile Countries = new("countries");
    public static readonly SubscriptionFile Currencies = new("currencies");
    public static readonly SubscriptionFile DonationItems = new("donationItems");
    public static readonly SubscriptionFile Elements = new("elements");
    public static readonly SubscriptionFile FeedbackSchemes = new("feedbackSchemes");
    public static readonly SubscriptionFile FundStructure = new("fundStructure");
    public static readonly SubscriptionFile GivingSchedules = new("givingSchedules");
    public static readonly SubscriptionFile Infrastructure = new("infrastructure");
    public static readonly SubscriptionFile Localization = new("localization");
    public static readonly SubscriptionFile Lookups = new("lookups");
    public static readonly SubscriptionFile Nisab = new("nisab");
    public static readonly SubscriptionFile OrganizationInfo = new("organizationInfo");
    public static readonly SubscriptionFile QurbaniItems = new("qurbaniItems");
    public static readonly SubscriptionFile QurbaniSeason = new("qurbaniSeason");
    public static readonly SubscriptionFile SponsorshipSchemes = new("sponsorshipSchemes");
    public static readonly SubscriptionFile TagDefinitions = new("tagDefinitions");
}