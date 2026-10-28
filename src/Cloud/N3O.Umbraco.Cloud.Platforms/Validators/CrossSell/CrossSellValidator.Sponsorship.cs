using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Giving.Allocations;
using N3O.Umbraco.Giving.Allocations.Lookups;
using N3O.Umbraco.Giving.Allocations.Models;
using N3O.Umbraco.Lookups;

namespace N3O.Umbraco.Cloud.Platforms.Validators;

public class SponsorshipCrossSellValidator : CrossSellValidator {
    private readonly ILookups _lookups;

    public SponsorshipCrossSellValidator(IContentHelper contentHelper,
                                         ILookups lookups,
                                         IFundStructureAccessor fundStructureAccessor) 
        : base(contentHelper, lookups, fundStructureAccessor) {
        _lookups = lookups;
    }

    protected override IFundDimensionOptions GetFundDimensionOptions(ContentProperties content) {
        return GetSponsorshipScheme(content)?.FundDimensionOptions;
    }
    
    protected override bool HasAnyLockedPrice(ContentProperties content) {
        return false;
    }

    protected override bool HasLockedPrice(ContentProperties content) {
        return false;
    }

    private SponsorshipScheme GetSponsorshipScheme(ContentProperties content) {
        return content.GetPropertyByAlias(AliasHelper<SponsorshipDonationFormStateContent>.PropertyAlias(x => x.Scheme))
                      .IfNotNull(x => ContentHelper.GetLookupValue<SponsorshipScheme>(_lookups, x));
    }
    
    protected override string ContentTypeAlias => PlatformsConstants.CrossSells.Sponsorship;
}
