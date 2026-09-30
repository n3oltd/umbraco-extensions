using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Lookups;

namespace N3O.Umbraco.Cloud.Platforms.Lookups;

public class RecentlyStartedFeaturedCrowdfundersElementKindDataSource : ElementKindDataSource {
    public RecentlyStartedFeaturedCrowdfundersElementKindDataSource(ILookups lookups) : base(lookups) { }

    public override string Name => "Featured Crowdfunders (Recently Started) Elements";
    public override string Description => "Data source for recently started featured crowdfunders elements";
    public override string Icon => "icon-thumbnails";

    protected override ElementKind Kind => ElementKind.RecentlyStartedFeaturedCrowdfunders;
}