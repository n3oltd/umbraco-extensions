using FluentValidation;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Giving.Allocations.Lookups;
using N3O.Umbraco.Localization;
using N3O.Umbraco.Validation;

namespace N3O.Umbraco.Giving.Cart.Models;

public class AddToCartReqValidator : ModelValidator<AddToCartReq> {
    public AddToCartReqValidator(IFormatter formatter) : base(formatter) {
        RuleFor(x => x.GivingType)
            .NotNull()
            .WithMessage(Get<Strings>(s => s.SpecifyGivingType));
    
        RuleFor(x => x.Allocation)
            .NotNull()
            .WithMessage(Get<Strings>(s => s.SpecifyAllocation));

        RuleFor(x => x.Quantity)
            .NotNull()
            .WithMessage(Get<Strings>(s => s.SpecifyQuantity));
        
        RuleFor(x => x.Allocation.Sponsorship)
            .NotNull()
            .When(x => x.GivingType == GivingTypes.Donation && x.Allocation.Type == AllocationTypes.Sponsorship)
            .WithMessage(Get<Strings>(s => s.SpecifySponsorshipDuration));
        
        RuleFor(x => x.Allocation.Sponsorship.Duration)
            .Null()
            .When(x => x.GivingType != GivingTypes.Donation &&
                       x.Allocation.HasValue(allocation => allocation.Sponsorship) && 
                       x.Allocation.Type == AllocationTypes.Sponsorship)
            .WithMessage(Get<Strings>(s => s.CannotSpecifySponsorshipDuration));

        RuleFor(x => x.Allocation.Feedback.DonationPlan)
            .Null()
            .When(x => x.GivingType != GivingTypes.RegularGiving &&
                       x.Allocation.HasValue(allocation => allocation.Feedback) &&
                       x.Allocation.Type == AllocationTypes.Feedback)
            .WithMessage(Get<Strings>(s => s.CannotSpecifyDonationPlan));

        RuleFor(x => x.Allocation.Feedback.DonationPlan)
            .NotNull()
            .When(x => x.GivingType == GivingTypes.RegularGiving &&
                       x.Allocation.HasValue(allocation => allocation.Feedback) &&
                       x.Allocation.Feedback.HasValue(feedback => feedback.Scheme) &&
                       x.Allocation.Feedback.Scheme.AllowedDonationPlans.HasAny())
            .WithMessage(Get<Strings>(s => s.SpecifyDonationPlan));
    }

    public class Strings : ValidationStrings {
        public string CannotSpecifyDonationPlan => "A donation plan can only be specified for regular giving";
        public string CannotSpecifySponsorshipDuration => "Sponsorship duration can only be specified for donations";
        public string SpecifyAllocation => "Please specify the allocation";
        public string SpecifyDonationPlan => "Please specify the donation plan";
        public string SpecifyGivingType => "Please specify the giving type";
        public string SpecifyQuantity => "Please specify the quantity";
        public string SpecifySponsorshipDuration => "Please specify the sponsorship duration";
    }
}
