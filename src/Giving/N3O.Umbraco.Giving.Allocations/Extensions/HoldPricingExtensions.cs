using N3O.Umbraco.Extensions;
using N3O.Umbraco.Giving.Allocations.Models;
using System.Linq;

namespace N3O.Umbraco.Giving.Allocations.Extensions;

public static class HoldPricingExtensions {
    public static bool HasPricing(this IHoldPricing holdPricing) {
        return holdPricing.HasValue(x => x.Pricing);
    }
    
    public static bool HasPrice(this IHoldPricing holdPricing) {
        return holdPricing.HasValue(x => x.Pricing?.Price);
    }
    
    public static bool HasAnyLockedPrice(this IHoldPricing holdPricing) {
        return holdPricing.HasLockedPrice() ||
               holdPricing?.Pricing?.Rules.OrEmpty().Any(x => x.Price?.Locked == true) == true;
    }

    public static bool HasLockedPrice(this IHoldPricing holdPricing) {
        return holdPricing?.Pricing?.Price?.Locked == true;
    }
}
