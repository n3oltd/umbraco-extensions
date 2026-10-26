using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Content;
using Umbraco.Cms.Core.Mapping;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class SuggestedAmountReqMapping : IMapDefinition {
    public void DefineMaps(IUmbracoMapper mapper) {
        mapper.Define<DonationFormStateSuggestedAmountElement, DonationFormSuggestedAmountReq>((_, _) => new DonationFormSuggestedAmountReq(), Map);
    }

    // Umbraco.Code.MapAll
    private void Map(DonationFormStateSuggestedAmountElement src, DonationFormSuggestedAmountReq dest, MapperContext ctx) {
        dest.Amount = (double) src.Amount;
        dest.Description = src.Description;
        dest.Title = src.Title;
    }
}
