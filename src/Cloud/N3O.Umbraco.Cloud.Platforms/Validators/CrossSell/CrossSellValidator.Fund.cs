using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Giving.Allocations;
using N3O.Umbraco.Giving.Allocations.Extensions;
using N3O.Umbraco.Giving.Allocations.Lookups;
using N3O.Umbraco.Giving.Allocations.Models;
using N3O.Umbraco.Lookups;

namespace N3O.Umbraco.Cloud.Platforms.Validators;

public class FundCrossSellValidator : CrossSellValidator {
    private readonly ILookups _lookups;

    public FundCrossSellValidator(IContentHelper contentHelper,
                                  ILookups lookups,
                                  IFundStructureAccessor fundStructureAccessor) 
        : base(contentHelper, lookups, fundStructureAccessor) {
        _lookups = lookups;
    }

    protected override IFundDimensionOptions GetFundDimensionOptions(ContentProperties content) {
        return GetDonationItem(content)?.FundDimensionOptions;
    }
    
    protected override bool HasAnyLockedPrice(ContentProperties content) {
        return GetDonationItem(content).HasAnyLockedPrice();
    }

    protected override bool HasLockedPrice(ContentProperties content) {
        return GetDonationItem(content).HasLockedPrice();
    }

    private DonationItem GetDonationItem(ContentProperties content) {
        return content.GetPropertyByAlias(AliasHelper<FundDonationFormStateContent>.PropertyAlias(x => x.DonationItem))
                      .IfNotNull(x => ContentHelper.GetLookupValue<DonationItem>(_lookups, x));
    }
    
    protected override string ContentTypeAlias => PlatformsConstants.CrossSells.Fund;
}
