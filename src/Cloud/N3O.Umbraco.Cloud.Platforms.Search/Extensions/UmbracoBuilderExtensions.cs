using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;

namespace N3O.Umbraco.Cloud.Platforms.Search.Extensions;

public static class UmbracoBuilderExtensions {
    public static IUmbracoBuilder AddPlatformsSearchCollections(this IUmbracoBuilder builder) {
        builder.Services.AddSingleton<IPlatformsCollectionNameResolver, PlatformsCollectionNameResolver>();

        return builder;
    }
}
