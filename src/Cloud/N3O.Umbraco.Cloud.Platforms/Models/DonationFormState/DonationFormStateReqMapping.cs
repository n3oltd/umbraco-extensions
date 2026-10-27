using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Lookups;
using Newtonsoft.Json;
using Umbraco.Cms.Core.Mapping;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public partial class DonationFormStateReqMapping : IMapDefinition {
    private readonly ILookups _lookups;
    private readonly ICdnClient _cdnClient;

    public DonationFormStateReqMapping(ILookups lookups, ICdnClient cdnClient) {
        _lookups = lookups;
        _cdnClient = cdnClient;
    }

    public void DefineMaps(IUmbracoMapper mapper) {
        mapper.Define<IHoldDonationFormStateContent, DonationFormStateReq>((src, _) => GetOrCreateDonationFormStateReq(src), Map);
    }

    private DonationFormStateReq GetOrCreateDonationFormStateReq(IHoldDonationFormStateContent src) {
        if (src.FormState.CustomFormState.HasValue()) {
            return JsonConvert.DeserializeObject<DonationFormStateReq>(src.FormState.CustomFormState);
        } else {
            return new DonationFormStateReq();
        }
    }

    private void Map(IHoldDonationFormStateContent src, DonationFormStateReq dest, MapperContext ctx) {
        if (!src.FormState.CustomFormState.HasValue()) {
            dest.CartItem = GetCartItemReqAsync(src, null).GetAwaiter().GetResult();
            dest.Options = GetDonationFormOptionsReq(ctx, src);
            dest.Extensions = null;
        }
    }
}