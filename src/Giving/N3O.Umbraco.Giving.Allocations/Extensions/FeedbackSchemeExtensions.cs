using N3O.Umbraco.Extensions;
using N3O.Umbraco.Giving.Allocations.Lookups;
using N3O.Umbraco.Giving.Allocations.Models;
using System.Linq;

namespace N3O.Umbraco.Giving.Allocations.Extensions; 

public static class FeedbackSchemeExtensions {
    public static bool AllowsDonationPlan(this FeedbackScheme scheme, DonationPlan donationPlan) {
        return scheme.AllowedDonationPlans.Contains(donationPlan);
    }

    public static FeedbackCustomFieldDefinition GetFeedbackCustomFieldDefinition(this FeedbackScheme scheme,
                                                                                        string alias) {
        return scheme.CustomFields.SingleOrDefault(x => x.Alias.EqualsInvariant(alias));
    }

    public static bool RequiresDonationPlan(this FeedbackScheme scheme, GivingType givingType) {
        return givingType == GivingTypes.RegularGiving && scheme.AllowedDonationPlans.HasAny();
    }
}