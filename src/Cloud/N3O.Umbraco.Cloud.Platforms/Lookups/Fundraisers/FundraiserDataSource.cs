using N3O.Umbraco.Lookups;

namespace N3O.Umbraco.Cloud.Platforms.Lookups;

public class FundraiserDataSource : LookupsDataSource<Fundraiser> {
    public FundraiserDataSource(ILookups lookups) : base(lookups) { }

    public override string Name => "Fundraisers";
    public override string Description => "Data source for fundraisers";
    public override string Icon => "icon-user";

    protected override string GetIcon(Fundraiser fundraiser) => "icon-user";
}
