using N3O.Umbraco.Exceptions;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Financial;
using N3O.Umbraco.Giving.Allocations.Extensions;
using N3O.Umbraco.Giving.Allocations.Lookups;
using N3O.Umbraco.Giving.Allocations.Models;
using N3O.Umbraco.Giving.Cart.Models;
using N3O.Umbraco.Lookups;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.Giving.Cart;

public class CartValidator : ICartValidator {
    private readonly ILookups _lookups;

    public CartValidator(ILookups lookups) {
        _lookups = lookups;
    }
    
    public bool IsValid(Currency currentCurrency, Entities.Cart cart) {
        try {
            var isValid = currentCurrency == cart.Currency &&
                          ContentsAreValid(cart.Donation) &&
                          ContentsAreValid(cart.RegularGiving);

            return isValid;
        } catch {
            return false;
        }
    }

    private bool ContentsAreValid(CartContents cartContents) {
        foreach (var allocation in cartContents.OrEmpty(x => x.Allocations)) {
            if (!AllocationIsValid(allocation)) {
                return false;
            }
        }

        return true;
    }

    private bool AllocationIsValid(Allocation allocation) {
        if (!FundDimensionsAreValid(allocation)) {
            return false;
        }
        
        if (allocation.Type == AllocationTypes.Fund) {
            var fund = allocation.Fund;

            if (!fund.HasValue(x => x.DonationItem)) {
                return false;
            }

            if (allocation.Value.IsZero() && fund.DonationItem.HasPricing()) {
                return false;
            }
        } else if (allocation.Type == AllocationTypes.Sponsorship) {
            var sponsorship = allocation.Sponsorship;
        
            if (!sponsorship.HasValue(x => x.Scheme)) {
                return false;
            }

            foreach (var componentAllocation in sponsorship.Components.OrEmpty()) {
                if (!componentAllocation.HasValue(x => x.Component)) {
                    return false;
                }
                
                if (componentAllocation.Component.GetSponsorshipScheme(_lookups) != allocation.Sponsorship.Scheme) {
                    return false;
                }
            }

            if (sponsorship.Scheme.Components.Any(c => c.Mandatory &&
                                                       sponsorship.Components.None(x => x.Component == c))) {
                return false;
            }
        } else if (allocation.Type == AllocationTypes.Feedback) {
            var feedback = allocation.Feedback;
        
            if (!feedback.HasValue(x => x.Scheme)) {
                return false;
            }
        }
        else {
            throw UnrecognisedValueException.For(allocation.Type);
        }

        return true;
    }

    private bool FundDimensionsAreValid(Allocation allocation) {
        var fundDimensions = allocation.FundDimensions;
        var fundDimensionOptions = allocation.GetFundDimensionsOptions();
        
        if (FundDimensionIsValid(fundDimensionOptions.Dimension1, fundDimensions.Dimension1) &&
            FundDimensionIsValid(fundDimensionOptions.Dimension2, fundDimensions.Dimension2) &&
            FundDimensionIsValid(fundDimensionOptions.Dimension3, fundDimensions.Dimension3) &&
            FundDimensionIsValid(fundDimensionOptions.Dimension4, fundDimensions.Dimension4)) {
            return true;
        }

        return false;
    }

    private bool FundDimensionIsValid<T>(IEnumerable<T> allowed, T value) {
        return allowed.None() || allowed.Contains(value);
    }
}
