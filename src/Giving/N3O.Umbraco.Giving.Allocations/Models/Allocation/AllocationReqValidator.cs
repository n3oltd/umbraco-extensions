using FluentValidation;
using N3O.Umbraco.Context;
using N3O.Umbraco.Exceptions;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Financial;
using N3O.Umbraco.Giving.Allocations.Extensions;
using N3O.Umbraco.Giving.Allocations.Lookups;
using N3O.Umbraco.Localization;
using N3O.Umbraco.Validation;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.Giving.Allocations.Models;

public class AllocationReqValidator : ModelValidator<AllocationReq> {
    private const int PledgeUrlMaxLength = 200;
    private const int NotesMaxLength = 200;
    
    public AllocationReqValidator(IFormatter formatter,
                                  ICurrencyAccessor currencyAccessor,
                                  IPriceCalculator priceCalculator,
                                  IPricedAmountValidator pricedAmountValidator,
                                  IEnumerable<IAllocationExtensionRequestValidator> extensionValidators)
        : base(formatter) {
        var currentCurrency = currencyAccessor.GetCurrency();

        RuleFor(x => x.Type)
            .NotNull()
            .WithMessage(Get<Strings>(s => s.SpecifyType));

        RuleFor(x => x.FundDimensions)
            .NotNull()
            .WithMessage(Get<Strings>(s => s.SpecifyFundDimensions));

        RuleFor(x => x.Fund)
            .NotNull()
            .When(x => x.Type == AllocationTypes.Fund)
            .WithMessage(Get<Strings>(s => s.SpecifyFundAllocation));

        RuleFor(x => x.Fund)
            .Null()
            .When(x => x.Type != AllocationTypes.Fund)
            .WithMessage(Get<Strings>(s => s.FundAllocationNotAllowed));

        RuleFor(x => x.Sponsorship)
            .NotNull()
            .When(x => x.Type == AllocationTypes.Sponsorship)
            .WithMessage(Get<Strings>(s => s.SpecifySponsorshipAllocation));

        RuleFor(x => x.Sponsorship)
            .Null()
            .When(x => x.Type != AllocationTypes.Sponsorship)
            .WithMessage(Get<Strings>(s => s.SponsorshipAllocationNotAllowed));
        
        RuleFor(x => x.Feedback)
            .Null()
            .When(x => x.Type != AllocationTypes.Feedback)
            .WithMessage(Get<Strings>(s => s.FeedbackAllocationNotAllowed));
        
        RuleFor(x => x.Value)
            .Must((req, x) => pricedAmountValidator.IsValid(x, req.Fund.DonationItem.Pricing, req.FundDimensions))
            .When(x => x.Value.HasValue() && x.Fund?.DonationItem?.HasPricing() == true)
            .WithMessage(Get<Strings>(s => s.InvalidValue));

        RuleForEach(x => x.Sponsorship.Components)
            .Must((req, x) => x.Component?.HasPricing() != true ||
                              pricedAmountValidator.IsValid(x.Value,
                                                            x.Component.Pricing,
                                                            req.FundDimensions,
                                                            req.Sponsorship.Duration?.Months ?? 1))
            .When(x => x.Sponsorship.HasValue())
            .WithMessage(Get<Strings>(s => s.InvalidValue));

        RuleFor(x => x.Feedback.CustomFields)
           .Must((req, x) => AllRequiredFieldsAreIncluded(req.Feedback.Scheme, x))
           .When(x => x.Feedback.HasValue(f => f.CustomFields))
           .WithMessage(Get<Strings>(s => s.FeedbackRequiredFieldsMissing));

        RuleForEach(x => x.Feedback.CustomFields.Entries)
           .Must((req, x) => CustomFieldValueIsValid(req.Feedback.Scheme, x))
           .When(x => x.Feedback.HasAny(f => f.CustomFields.Entries))
           .WithMessage(Get<Strings>(s => s.FeedbackInvalidFieldValues));

        RuleForEach(x => x.Feedback.CustomFields.Entries)
           .Must((req, x) => CustomFieldAliasIsValid(req.Feedback.Scheme, x))
           .When(x => x.Feedback.HasAny(f => f.CustomFields.Entries))
           .WithMessage(Get<Strings>(s => s.FeedbackInvalidFields));

        RuleFor(x => x.Feedback.DonationPlan)
           .Must((req, x) => req.Feedback.Scheme.AllowsDonationPlan(x))
           .When(x => x.Feedback.HasValue(f => f.Scheme) && x.Feedback.HasValue(f => f.DonationPlan))
           .WithMessage(Get<Strings>(s => s.FeedbackDonationPlanNotAllowed));

        RuleFor(x => x.Value)
           .Must((req, x) => DonationPlanValueIsValid(priceCalculator, req.Feedback, req.FundDimensions, x))
           .When(x => x.Value.HasValue(v => v.Amount) &&
                      x.Value.HasValue(v => v.Currency) &&
                      x.Feedback.HasValue(f => f.Scheme) &&
                      x.Feedback.HasValue(f => f.DonationPlan))
           .WithMessage(Get<Strings>(s => s.FeedbackInvalidDonationPlanValue));

        RuleFor(x => x.Value)
            .Must((req, x) => x.Amount.GetValueOrThrow() == req.Sponsorship.Components.Sum(c => c.Value.Amount.GetValueOrThrow()))
            .When(x => x.Value.HasValue(v => v.Amount) && x.Sponsorship.HasValue() && x.Sponsorship.Components.All(c => c.Value.HasValue(v => v.Amount)))
            .WithMessage(Get<Strings>(s => s.InvalidValue));

        RuleFor(x => x.FundDimensions)
            .Must((req, x) => FundDimensionsAreValid(x, req.GetFundDimensionsOptions()))
            .When(x => x.FundDimensions.HasValue())
            .WithMessage(Get<Strings>(s => s.InvalidFundDimensions));

        RuleFor(x => x.Notes)
            .MaximumLength(NotesMaxLength)
            .When(x => x.Notes.HasAny())
            .WithMessage(Get<Strings>(s => s.NotesTooLong));
        
        RuleFor(x => x.PledgeUrl)
            .MaximumLength(PledgeUrlMaxLength)
            .When(x => x.PledgeUrl.HasAny())
            .WithMessage(Get<Strings>(s => s.PledgeUrlTooLong));
        
        RuleFor(x => x)
            .Custom((req, context) => ValidateAllocationExtensionData(extensionValidators, context, req));
        
        ValidateCurrencies(currentCurrency);
    }

    private void ValidateCurrencies(Currency currency) {
        RuleFor(x => x.Value.Currency)
            .Must(x => x == currency)
            .When(x => x.HasValue(y => y.Value?.Currency))
            .WithMessage(Get<Strings>(s => s.CurrencyMismatch));

        RuleForEach(x => x.Sponsorship.Components)
            .Must(x => x.Value.Currency == currency)
            .When(x => x.Sponsorship.OrEmpty(y => y.Components).HasAny(c => c.Value.HasValue(v => v.Currency)))
            .WithMessage(Get<Strings>(s => s.CurrencyMismatch));
    }
    
    private void ValidateAllocationExtensionData(IEnumerable<IAllocationExtensionRequestValidator> extensionValidators,
                                                 ValidationContext<AllocationReq> context,
                                                 AllocationReq req) {
        foreach (var validator in extensionValidators.Where(x => req.Extensions.CanValidate(x))) {
            var result = validator.Validate(req.Extensions);

            result.Errors.OrEmpty().Do(context.AddFailure);
        }
    }

    private bool AllRequiredFieldsAreIncluded(FeedbackScheme feedbackScheme,
                                              FeedbackNewCustomFieldsReq newCustomFieldReq) {
        foreach (var customField in feedbackScheme.CustomFields.Where(x => x.Required)) {
            if (newCustomFieldReq.Entries.None(x => x.Alias == customField.Alias)) {
                return false;
            }
        }

        return true;
    }

    private bool CustomFieldValueIsValid(FeedbackScheme feedbackScheme, IFeedbackNewCustomField newCustomFieldReq) {
        var definition = feedbackScheme.GetFeedbackCustomFieldDefinition(newCustomFieldReq.Alias);

        if (definition.HasValue()) {
            return HasOnlyValueOfType(newCustomFieldReq, definition.Type) &&
                   newCustomFieldReq.PassesValidation(definition);
        }

        return true;
    }

    private bool CustomFieldAliasIsValid(FeedbackScheme feedbackScheme, IFeedbackNewCustomField newCustomFieldReq) {
        var definition = feedbackScheme.GetFeedbackCustomFieldDefinition(newCustomFieldReq.Alias);

        return definition != null;
    }

    private bool DonationPlanValueIsValid(IPriceCalculator priceCalculator,
                                          FeedbackAllocationReq feedback,
                                          IFundDimensionValues fundDimensions,
                                          Money value) {
        var price = priceCalculator.InCurrency(feedback.Scheme.Pricing, fundDimensions, value.Currency);

        if (!price.HasValue()) {
            return false;
        }

        var priceMoney = new Money(price.Amount, value.Currency);
        var minimumDonationAmount = feedback.DonationPlan.GetMinimumDonationAmount(priceMoney);

        // Applies to unlocked prices too: the plan stops after its donations, so together they must cover the price
        return value.Amount >= minimumDonationAmount.Amount && value.Amount <= price.Amount;
    }

    private bool FundDimensionsAreValid(FundDimensionValuesReq req, IFundDimensionOptions options) {
        if (FundDimensionIsValid(req.Dimension1, options?.Dimension1) &&
            FundDimensionIsValid(req.Dimension2, options?.Dimension2) &&
            FundDimensionIsValid(req.Dimension3, options?.Dimension3) &&
            FundDimensionIsValid(req.Dimension4, options?.Dimension4)) {
            return true;
        }

        return false;
    }

    private bool FundDimensionIsValid<T>(T value, IEnumerable<T> options) where T : FundDimensionValue<T> {
        return value == null || options?.Contains(value) != false;
    }

    private bool HasOnlyValueOfType(IFeedbackNewCustomField req, FeedbackCustomFieldType type) {
        if (type == FeedbackCustomFieldTypes.Bool) {
            return req.Date == null && req.Text == null;
        } else if (type == FeedbackCustomFieldTypes.Date) {
            return req.Text == null && req.Bool == null;
        } else if (type == FeedbackCustomFieldTypes.Text) {
            return req.Bool == null && req.Date == null;
        } else {
            throw UnrecognisedValueException.For(type);
        }
    }

    public class Strings : ValidationStrings {
        public string CurrencyMismatch => "All currencies must be the same and must match the currently active currency";
        public string FeedbackAllocationNotAllowed => "Feedback cannot be specified for this type of allocation";
        public string FeedbackDonationPlanNotAllowed => "The feedback scheme does not allow this donation plan";
        public string FeedbackInvalidDonationPlanValue => "The value is outside the range the donation plan allows";
        public string FeedbackInvalidFields => "The feedback allocation contains invalid custom fields";
        public string FeedbackInvalidFieldValues => "One or more of the feedback allocation custom fields have invalid values";
        public string FeedbackRequiredFieldsMissing => "Feedback allocation is missing a required field";
        public string FundAllocationNotAllowed => "Fund cannot be specified for this type of allocation";
        public string InvalidFundDimensions => "One or more fund dimension values are invalid";
        public string InvalidValue => "Invalid value specified";
        public string NotesTooLong => $"Notes cannot exceed {NotesMaxLength} characters";
        public string PledgeUrlTooLong => $"Pledge URL cannot exceed {PledgeUrlMaxLength} characters";
        public string SpecifyFundAllocation => "Please specify the fund allocation";
        public string SpecifyFundDimensions => "Please specify the fund dimensions";
        public string SpecifySponsorshipAllocation => "Please specify the sponsorship allocation";
        public string SponsorshipAllocationNotAllowed => "Sponsorship cannot be specified for this type of allocation";
        public string SpecifyType => "Please specify the allocation type";
    }
}