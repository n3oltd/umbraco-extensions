using N3O.Umbraco.Financial;
using N3O.Umbraco.Giving.Allocations.Models;
using N3O.Umbraco.Lookups;

namespace N3O.Umbraco.Giving.Allocations.Lookups;

public class GivingLookupTypes : ILookupTypesSet {
    [LookupInfo(typeof(AllocationType))]
    public const string AllocationTypes = "allocationTypes";

    [LookupInfo(typeof(Currency))]
    public const string Currencies = "currencies";
    
    [LookupInfo(typeof(DonationItem))]
    public const string DonationItems = "donationItems";

    [LookupInfo(typeof(DonationPlan))]
    public const string DonationPlans = "donationPlans";

    [LookupInfo(typeof(FeedbackScheme))]
    public const string FeedbackSchemes = "feedbackSchemes";

    [LookupInfo(typeof(FundDimension1Value))]
    public const string FundDimension1Values = "fundDimension1Values";
    
    [LookupInfo(typeof(FundDimension2Value))]
    public const string FundDimension2Values = "fundDimension2Values";
    
    [LookupInfo(typeof(FundDimension1Value))]
    public const string FundDimension3Values = "fundDimension3Values";
    
    [LookupInfo(typeof(FundDimension4Value))]
    public const string FundDimension4Values = "fundDimension4Values";
    
    [LookupInfo(typeof(GivingType))]
    public const string GivingTypes = "givingTypes";
    
    [LookupInfo(typeof(SponsorshipDuration))]
    public const string SponsorshipDurations = "sponsorshipDurations";
        
    [LookupInfo(typeof(SponsorshipScheme))]
    public const string SponsorshipSchemes = "sponsorshipSchemes";
}
