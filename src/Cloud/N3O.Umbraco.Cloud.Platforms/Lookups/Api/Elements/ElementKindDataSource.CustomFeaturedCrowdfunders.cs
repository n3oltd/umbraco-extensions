using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Lookups;

namespace N3O.Umbraco.Cloud.Platforms.Lookups;

public class CustomFeaturedCrowdfundersElementKindDataSource : ElementKindDataSource {
    public CustomFeaturedCrowdfundersElementKindDataSource(ILookups lookups) : base(lookups) { }

    public override string Name => "Featured Crowdfunders (Custom) Elements";
    public override string Description => "Data source for custom featured crowdfunders elements";
    public override string Icon => "icon-categories";

    protected override ElementKind Kind => ElementKind.CustomFeaturedCrowdfunders;
}