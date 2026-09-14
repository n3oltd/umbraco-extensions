using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Bundling.Middleware;
using N3O.Umbraco.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace N3O.Umbraco.Bundling;

public class BundlingComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        var settings = builder.Config.GetSection(BundlingSettings.SectionName).Get<BundlingSettings>() ??
                       new BundlingSettings();

        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton<IAssetManifest, AssetManifest>();

        if (!settings.ServeSourceMaps) {
            builder.Services.AddTransient<IStartupFilter, SourceMapsStartupFilter>();
        }
    }
}
