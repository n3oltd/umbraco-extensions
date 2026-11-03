using N3O.Umbraco.Giving.Allocations.Lookups;
using System;

namespace N3O.Umbraco.Giving.Allocations.Extensions;

public static class DonationPlanExtensions {
    public static decimal GetDonationAmount(this DonationPlan donationPlan, decimal price) {
        return Math.Ceiling(price * 100m / donationPlan.Donations) / 100m;
    }
}
