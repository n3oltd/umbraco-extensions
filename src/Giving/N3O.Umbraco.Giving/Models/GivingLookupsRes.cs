using N3O.Umbraco.Financial;
using N3O.Umbraco.Giving.Allocations.Lookups;
using N3O.Umbraco.Giving.Allocations.Models;
using N3O.Umbraco.Lookups;
using System.Collections.Generic;

namespace N3O.Umbraco.Giving.Models;

public class GivingLookupsRes : LookupsRes {
    [FromLookupType(GivingLookupTypes.AllocationTypes)]
    public IEnumerable<NamedLookupRes> AllocationTypes { get; set; }

    [FromLookupType(GivingLookupTypes.Currencies)]
    public IEnumerable<CurrencyRes> Currencies { get; set; }
    
    [FromLookupType(GivingLookupTypes.DonationItems)]
    public IEnumerable<DonationItemRes> DonationItems { get; set; }

    [FromLookupType(GivingLookupTypes.DonationPlans)]
    public IEnumerable<DonationPlanRes> DonationPlans { get; set; }

    [FromLookupType(GivingLookupTypes.GivingTypes)]
    public IEnumerable<NamedLookupRes> GivingTypes { get; set; }
    
    [FromLookupType(GivingLookupTypes.FundDimension1Values)]
    public IEnumerable<FundDimensionValueRes> FundDimension1Values { get; set; }
    
    [FromLookupType(GivingLookupTypes.FundDimension2Values)]
    public IEnumerable<FundDimensionValueRes> FundDimension2Values { get; set; }
    
    [FromLookupType(GivingLookupTypes.FundDimension3Values)]
    public IEnumerable<FundDimensionValueRes> FundDimension3Values { get; set; }
    
    [FromLookupType(GivingLookupTypes.FundDimension4Values)]
    public IEnumerable<FundDimensionValueRes> FundDimension4Values { get; set; }
    
    [FromLookupType(GivingLookupTypes.SponsorshipDurations)]
    public IEnumerable<SponsorshipDurationRes> SponsorshipDurations { get; set; }
    
    [FromLookupType(GivingLookupTypes.SponsorshipSchemes)]
    public IEnumerable<SponsorshipSchemeRes> SponsorshipSchemes { get; set; }
}
