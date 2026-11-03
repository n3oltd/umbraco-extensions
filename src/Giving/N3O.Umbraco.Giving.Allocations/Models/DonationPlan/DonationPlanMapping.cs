using N3O.Umbraco.Giving.Allocations.Lookups;
using N3O.Umbraco.Lookups;
using Umbraco.Cms.Core.Mapping;

namespace N3O.Umbraco.Giving.Allocations.Models;

public class DonationPlanMapping : IMapDefinition {
    public void DefineMaps(IUmbracoMapper mapper) {
        mapper.Define<DonationPlan, DonationPlanRes>((_, _) => new DonationPlanRes(), Map);
    }

    // Umbraco.Code.MapAll -Id -Name
    private void Map(DonationPlan src, DonationPlanRes dest, MapperContext ctx) {
        ctx.Map<INamedLookup, NamedLookupRes>(src, dest);

        dest.Donations = src.Donations;
    }
}
