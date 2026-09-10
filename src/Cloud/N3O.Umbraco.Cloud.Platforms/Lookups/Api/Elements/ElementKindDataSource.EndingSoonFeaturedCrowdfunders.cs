using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Lookups;

namespace N3O.Umbraco.Cloud.Platforms.Lookups;

public class EndingSoonFeaturedCrowdfundersElementKindDataSource : ElementKindDataSource {
    public EndingSoonFeaturedCrowdfundersElementKindDataSource(ILookups lookups) : base(lookups) { }

    public override string Name => "Featured Crowdfunders (Ending Soon) Elements";
    public override string Description => "Data source for ending soon featured crowdfunders elements";
    public override string Icon => "icon-thumbnails";

    protected override ElementKind Kind => ElementKind.EndingSoonFeaturedCrowdfunders;
}