using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Giving.Allocations;
using N3O.Umbraco.Giving.Allocations.Lookups;
using N3O.Umbraco.Giving.Allocations.Models;
using N3O.Umbraco.Lookups;

namespace N3O.Umbraco.Cloud.Platforms.Validators;

public class QurbaniCrossSellValidator : CrossSellValidator {
    private readonly ILookups _lookups;

    public QurbaniCrossSellValidator(IContentHelper contentHelper,
                                     ILookups lookups,
                                     IFundStructureAccessor fundStructureAccessor) 
        : base(contentHelper, lookups, fundStructureAccessor) {
        _lookups = lookups;
    }

    protected override IFundDimensionOptions GetFundDimensionOptions(ContentProperties content) {
        return GetQurbaniItem(content)?.FundDimensionOptions;
    }
    
    protected override bool HasAnyLockedPrice(ContentProperties content) {
        return GetQurbaniItem(content)?.Price?.Locked == true;
    }

    protected override bool HasLockedPrice(ContentProperties content) {
        return false;
    }

    private QurbaniItem GetQurbaniItem(ContentProperties content) {
        return content.GetPropertyByAlias(AliasHelper<QurbaniDonationFormStateContent>.PropertyAlias(x => x.QurbaniItem))
                      .IfNotNull(x => ContentHelper.GetLookupValue<QurbaniItem>(_lookups, x));
    }
    
    protected override string ContentTypeAlias => PlatformsConstants.CrossSells.Qurbani;
}
