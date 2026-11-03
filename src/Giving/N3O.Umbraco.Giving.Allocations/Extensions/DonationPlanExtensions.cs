using N3O.Umbraco.Financial;
using N3O.Umbraco.Giving.Allocations.Lookups;
using System;

namespace N3O.Umbraco.Giving.Allocations.Extensions;

public static class DonationPlanExtensions {
    // Rounded up, so the plan's donations never add up to less than the price
    public static Money GetMinimumDonationAmount(this DonationPlan donationPlan, Money price) {
        var amount = Math.Round(price.Amount / donationPlan.Donations,
                                (int) price.Currency.DecimalDigits,
                                MidpointRounding.ToPositiveInfinity);

        return new Money(amount, price.Currency);
    }
}
