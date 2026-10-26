using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Giving.Allocations.Lookups;
using N3O.Umbraco.Giving.Allocations.Models;
using N3O.Umbraco.Lookups;
using System.Linq;

namespace N3O.Umbraco.Cloud.Platforms.Validators;

public class FundDonationFormStateValidator : DonationFormStateValidator<FundDonationFormStateContent> {
    private static readonly string OneTimeSuggestedAmountsAlias = AliasHelper<FundDonationFormStateContent>.PropertyAlias(x => x.OneTimeSuggestedAmounts);
    private static readonly string RecurringSuggestedAmountsAlias = AliasHelper<FundDonationFormStateContent>.PropertyAlias(x => x.RecurringSuggestedAmounts);

    private readonly ILookups _lookups;

    public FundDonationFormStateValidator(IContentHelper contentHelper, ILookups lookups)
        : base(contentHelper, lookups) {
        _lookups = lookups;
    }

    public override void Validate(ContentProperties content) {
        base.Validate(content);

        var donationItem = GetDonationItem(content);

        if (donationItem != null) {
            ValidateSuggestedAmount(content, donationItem, GivingTypes.Donation, OneTimeSuggestedAmountsAlias);
            ValidateSuggestedAmount(content, donationItem, GivingTypes.RegularGiving, RecurringSuggestedAmountsAlias);
        }
    }

    protected override IFundDimensionOptions GetFundDimensionOptions(ContentProperties content) {
        return GetDonationItem(content)?.FundDimensionOptions;
    }

    private DonationItem GetDonationItem(ContentProperties content) {
        return content.GetPropertyByAlias(AliasHelper<FundDonationFormStateContent>.PropertyAlias(x => x.DonationItem))
                      .IfNotNull(x => ContentHelper.GetLookupValue<DonationItem>(_lookups, x));
    }

    private void ValidateSuggestedAmount(ContentProperties content,
                                         DonationItem donationItem,
                                         GivingType givingType,
                                         string propertyAlias) {
        var property = content.ElementsProperties.SingleOrDefault(x => x.Alias.EqualsInvariant(propertyAlias));
        var suggestedAmounts = property.IfNotNull(x => ContentHelper.GetBlockList(x))
                                       .OrEmpty()
                                       .Select(x => x.Content)
                                       .As<DonationFormStateSuggestedAmountElement>()
                                       .ToList();

        if (suggestedAmounts.HasAny()) {
            if (donationItem.Pricing?.Price?.Locked == true) {
                ErrorResult(property, $"{donationItem.Name} has pricing so does not allow price handles");
            }

            if (!donationItem.AllowedGivingTypes.OrEmpty().Contains(givingType)) {
                ErrorResult(property, $"{donationItem.Name} does not allow {givingType.Name.Quote()} so these price handles are not permitted");
            }
        }
    }
}
