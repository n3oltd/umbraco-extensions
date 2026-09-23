using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Composing;
using N3O.Umbraco.Extensions;
using Umbraco.Cms.Core.DependencyInjection;

namespace N3O.Umbraco.Cloud.Platforms.Search;

public class PlatformsSearchComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        RegisterAll(t => t.ImplementsInterface<ICampaignOfferingVisibilityFilter>(),
                    t => builder.Services.AddScoped(typeof(ICampaignOfferingVisibilityFilter), t));

        builder.Services.AddTransient<ICampaignOfferingVisibility, CampaignOfferingVisibility>();
        builder.Services.AddSingleton<IPlatformsCollectionNameResolver, PlatformsCollectionNameResolver>();
    }
}
