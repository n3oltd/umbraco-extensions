using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Lookups;

namespace N3O.Umbraco.Cloud.Platforms.Lookups;

public class PopularFeaturedCrowdfundersElementKindDataSource : ElementKindDataSource {
    public PopularFeaturedCrowdfundersElementKindDataSource(ILookups lookups) : base(lookups) { }

    public override string Name => "Featured Crowdfunders (Popular) Elements";
    public override string Description => "Data source for popular featured crowdfunders elements";
    public override string Icon => "icon-thumbnails";

    protected override ElementKind Kind => ElementKind.PopularFeaturedCrowdfunders;
}