using N3O.Umbraco.Extensions;
using N3O.Umbraco.Giving.Allocations.Lookups;
using N3O.Umbraco.Lookups;
using System.Linq;
using Umbraco.Cms.Core.Mapping;

namespace N3O.Umbraco.Giving.Allocations.Models;

public class FeedbackSchemeMapping : IMapDefinition {
    public void DefineMaps(IUmbracoMapper mapper) {
        mapper.Define<FeedbackScheme, FeedbackSchemeRes>((_, _) => new FeedbackSchemeRes(), Map);
    }

    // Umbraco.Code.MapAll -Id -Name
    private void Map(FeedbackScheme src, FeedbackSchemeRes dest, MapperContext ctx) {
        ctx.Map<INamedLookup, NamedLookupRes>(src, dest);

        dest.AllowedGivingTypes = src.AllowedGivingTypes;
        dest.AllowedDonationPlans = src.AllowedDonationPlans;
        dest.CustomFields = src.CustomFields
                               .OrEmpty()
                               .Select(ctx.Map<FeedbackCustomFieldDefinition, FeedbackCustomFieldDefinitionRes>)
                               .ToList();
        dest.FundDimensionOptions = ctx.Map<IFundDimensionOptions, FundDimensionOptionsRes>(src.FundDimensionOptions);
        
        dest.Pricing = src.Pricing.IfNotNull(ctx.Map<IPricing, PricingRes>);
    }
}
