using N3O.Umbraco.Giving.Allocations.Lookups;
using N3O.Umbraco.Lookups;
using System.Collections.Generic;

namespace N3O.Umbraco.Giving.Allocations.Models;

public class FeedbackSchemeRes : NamedLookupRes {
    public IEnumerable<GivingType> AllowedGivingTypes { get; set; }
    public IEnumerable<DonationPlan> AllowedDonationPlans { get; set; }
    public IEnumerable<FeedbackCustomFieldDefinitionRes> CustomFields { get; set; }
    public FundDimensionOptionsRes FundDimensionOptions { get; set; }
    public PricingRes Pricing { get; set; }
}
