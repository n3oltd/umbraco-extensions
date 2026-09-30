using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Lookups;

namespace N3O.Umbraco.Cloud.Platforms.Lookups;

public class AlmostCompleteFeaturedCrowdfundersElementKindDataSource : ElementKindDataSource {
    public AlmostCompleteFeaturedCrowdfundersElementKindDataSource(ILookups lookups) : base(lookups) { }

    public override string Name => "Featured Crowdfunders (Almost Complete) Elements";
    public override string Description => "Data source for almost complete featured crowdfunders elements";
    public override string Icon => "icon-thumbnails";

    protected override ElementKind Kind => ElementKind.AlmostCompleteFeaturedCrowdfunders;
}