using N3O.Umbraco.Giving.Allocations.Lookups;
using N3O.Umbraco.Giving.Allocations.Models;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Models;

public class PublishedFeedbackScheme :
    PublishedNamedLookup, IHoldAllowedGivingTypes, IHoldFundDimensionOptions, IHoldPricing {
    public IEnumerable<PublishedDonationPlan> AllowedDonationPlans { get; set; }
    public PublishedFundDimensionOptions FundDimensionOptions { get; set; }
    public PublishedPricing Pricing { get; set; }
    public IEnumerable<PublishedFeedbackCustomFieldDefinition> CustomFieldDefinitions { get; set; }

    [JsonIgnore]
    public IEnumerable<GivingType> AllowedGivingTypes {
        get {
            yield return GivingTypes.Donation;
            yield return GivingTypes.RegularGiving;
        }
    }

    [JsonIgnore]
    IFundDimensionOptions IHoldFundDimensionOptions.FundDimensionOptions => FundDimensionOptions;

    [JsonIgnore]
    IPricing IHoldPricing.Pricing => Pricing;
}