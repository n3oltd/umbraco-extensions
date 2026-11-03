using N3O.Umbraco.Extensions;
using N3O.Umbraco.Financial;
using N3O.Umbraco.Giving.Allocations.Lookups;
using System;

namespace N3O.Umbraco.Giving.Allocations.Extensions;

public static class DonationPlanExtensions {
    public static decimal GetDonationAmount(this DonationPlan donationPlan, decimal price, Currency currency) {
        var factor = currency.GetLowestDenominationFactor();

        return Math.Ceiling(price * factor / donationPlan.Donations) / factor;
    }
}
