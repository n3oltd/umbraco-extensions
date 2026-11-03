using N3O.Umbraco.Extensions;
using N3O.Umbraco.Financial;
using N3O.Umbraco.Giving.Allocations.Lookups;
using System;
using System.Linq;

namespace N3O.Umbraco.Giving.Allocations.Extensions;

public static class DonationPlanExtensions {
    // Rounded up, so the plan's donations never add up to less than the price
    public static Money GetMinimumDonationAmount(this DonationPlan donationPlan, Money price) {
        return price.SafeDivide(donationPlan.Donations, MidpointRounding.ToPositiveInfinity).First();
    }
}
