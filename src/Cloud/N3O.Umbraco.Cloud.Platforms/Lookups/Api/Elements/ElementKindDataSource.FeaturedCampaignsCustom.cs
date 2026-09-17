using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Lookups;

namespace N3O.Umbraco.Cloud.Platforms.Lookups;

public class FeaturedCampaignsCustomElementKindDataSource : ElementKindDataSource {
    public FeaturedCampaignsCustomElementKindDataSource(ILookups lookups) : base(lookups) { }

    public override string Name => "Featured Campaigns (Custom) Elements";
    public override string Description => "Data source for custom featured campaigns elements";
    public override string Icon => "icon-categories";

    protected override ElementKind Kind => ElementKind.FeaturedCampaignsCustom;
}
