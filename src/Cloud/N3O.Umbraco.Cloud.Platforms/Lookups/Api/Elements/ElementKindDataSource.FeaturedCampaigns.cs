using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Lookups;

namespace N3O.Umbraco.Cloud.Platforms.Lookups;

public class FeaturedCampaignsElementKindDataSource : ElementKindDataSource {
    public FeaturedCampaignsElementKindDataSource(ILookups lookups) : base(lookups) { }

    public override string Name => "Featured Campaigns Element";
    public override string Description => "Data source for featured campaigns element";
    public override string Icon => "icon-thumbnails";

    protected override ElementKind Kind => ElementKind.FeaturedCampaigns;
}
