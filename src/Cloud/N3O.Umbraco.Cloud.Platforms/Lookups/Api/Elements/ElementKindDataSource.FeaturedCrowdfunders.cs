using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Lookups;

namespace N3O.Umbraco.Cloud.Platforms.Lookups;

public class FeaturedCrowdfundersElementKindDataSource : ElementKindDataSource {
    public FeaturedCrowdfundersElementKindDataSource(ILookups lookups) : base(lookups) { }

    public override string Name => "Featured Crowdfunders Element";
    public override string Description => "Data source for featured crowdfunders element";
    public override string Icon => "icon-thumbnails";

    protected override ElementKind Kind => ElementKind.FeaturedCrowdfunders;
}
