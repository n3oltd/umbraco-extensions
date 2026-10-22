using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Giving.Allocations;
using N3O.Umbraco.Giving.Allocations.Models;
using N3O.Umbraco.Lookups;
using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Validators;

public abstract class CrossSellValidator : ContentValidator {
    private static readonly string AmountAlias = AliasHelper<CrossSellContent>.PropertyAlias(x => x.Amount);
    private static readonly string MaximumAlias = AliasHelper<CrossSellContent>.PropertyAlias(x => x.Maximum);
    private static readonly string MinimumAlias = AliasHelper<CrossSellContent>.PropertyAlias(x => x.Minimum);
    private static readonly string PercentageAlias = AliasHelper<CrossSellContent>.PropertyAlias(x => x.Percentage);
    private static readonly string ThresholdAlias = AliasHelper<CrossSellContent>.PropertyAlias(x => x.Threshold);

    private readonly ILookups _lookups;
    private readonly IFundStructureAccessor _fundStructureAccessor;

    protected CrossSellValidator(IContentHelper contentHelper,
                                 ILookups lookups,
                                 IFundStructureAccessor fundStructureAccessor)
        : base(contentHelper) {
        _lookups = lookups;
        _fundStructureAccessor = fundStructureAccessor;
    }

    public override bool IsValidator(ContentProperties content) {
        return content.ContentTypeAlias == ContentTypeAlias;
    }

    public override void Validate(ContentProperties content) {
       ValidateFundDimensions(content);
       ValidateAmount(content);
    }

    protected abstract IFundDimensionOptions GetFundDimensionOptions(ContentProperties content);
    protected abstract bool HasLockedPrice(ContentProperties content);
    protected abstract string ContentTypeAlias { get; }

    private void ValidateFundDimensions(ContentProperties content) {
        var fundDimensionOptions = GetFundDimensionOptions(content);

        if (fundDimensionOptions != null) {
            ValidateFixedDimension(content, fundDimensionOptions);
        }
    }
    
    private void ValidateFixedDimension(ContentProperties content, IFundDimensionOptions fundDimensionOptions) {
        var fundStructure = _fundStructureAccessor.GetFundStructure();
            
        HasFixedDimension(content, fundDimensionOptions.Dimension1, AliasHelper<DonationFormStateContent>.PropertyAlias(x => x.Dimension1), fundStructure.Dimension1.IsActive);
        HasFixedDimension(content, fundDimensionOptions.Dimension2, AliasHelper<DonationFormStateContent>.PropertyAlias(x => x.Dimension2), fundStructure.Dimension2.IsActive);
        HasFixedDimension(content, fundDimensionOptions.Dimension3, AliasHelper<DonationFormStateContent>.PropertyAlias(x => x.Dimension3), fundStructure.Dimension3.IsActive);
        HasFixedDimension(content, fundDimensionOptions.Dimension4, AliasHelper<DonationFormStateContent>.PropertyAlias(x => x.Dimension4), fundStructure.Dimension4.IsActive);
    }
    
    private void ValidateAmount(ContentProperties content) {
        var amount = content.GetPropertyValueByAlias<decimal?>(AmountAlias).GetValueOrDefault();
        var maximum = content.GetPropertyValueByAlias<decimal?>(MaximumAlias).GetValueOrDefault();
        var minimum = content.GetPropertyValueByAlias<decimal?>(MinimumAlias).GetValueOrDefault();
        var percentage = content.GetPropertyValueByAlias<decimal?>(PercentageAlias).GetValueOrDefault();
        var threshold = content.GetPropertyValueByAlias<decimal?>(ThresholdAlias).GetValueOrDefault();
        var hasAmount = amount.IsSpecified();
        var hasRelativeValue = maximum.IsSpecified() ||
                               minimum.IsSpecified() ||
                               percentage.IsSpecified() ||
                               threshold.IsSpecified();

        if (ContentTypeAlias == PlatformsConstants.CrossSells.Sponsorship && (hasAmount || hasRelativeValue)) {
            ErrorResult("Sponsorship item cannot have an amount or relative value");
        }

        if (HasLockedPrice(content) && (hasAmount || hasRelativeValue)) {
            ErrorResult("Amount or relative value cannot be specified");
        }

        if (hasAmount && hasRelativeValue) {
            ErrorResult(content.GetPropertyByAlias(AmountAlias), "cannot be specified with a relative value");
        }

        if (hasRelativeValue && !percentage.IsSpecified()) {
            ErrorResult(content.GetPropertyByAlias(PercentageAlias), "must be specified for a relative value");
        }

        if (percentage < 0 || percentage > 100) {
            ErrorResult(content.GetPropertyByAlias(PercentageAlias), "must be greater than 0 and no more than 100");
        }

        NotNegative(content, MinimumAlias, minimum);
        NotNegative(content, MaximumAlias, maximum);
        NotNegative(content, ThresholdAlias, threshold);

        if (minimum.IsSpecified() && maximum.IsSpecified() && minimum > maximum) {
            ErrorResult(content.GetPropertyByAlias(MinimumAlias), "cannot be greater than the maximum");
        }

        if (minimum.IsSpecified() && threshold.IsSpecified() && threshold * percentage.ToFraction() < minimum) {
            ErrorResult(content.GetPropertyByAlias(ThresholdAlias),
                        "multiplied by the percentage cannot be below the minimum");
        }
    }

    private void HasFixedDimension<T>(ContentProperties content,
                                      IEnumerable<T> allowedValues,
                                      string propertyAlias,
                                      bool isActive)
        where T : FundDimensionValue<T> {
        var property = content.GetPropertyByAlias(propertyAlias);
        var value = property.IfNotNull(x => ContentHelper.GetLookupValue<T>(_lookups, x));

        if (isActive && value == null && !allowedValues.IsSingle()) {
            ErrorResult(property, $"{typeof(T).Name} should have a fixed value");
        }
    }

    private void NotNegative(ContentProperties content, string alias, decimal value) {
        if (value < 0) {
            ErrorResult(content.GetPropertyByAlias(alias), "cannot be negative");
        }
    }
}
