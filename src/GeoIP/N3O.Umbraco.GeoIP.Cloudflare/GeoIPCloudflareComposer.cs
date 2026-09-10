using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace N3O.Umbraco.GeoIP.Cloudflare;

public class GeoIPCloudflareComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        builder.Services.AddTransient<IIPGeoLocationProvider, CloudflareIPGeoLocationProvider>();
    }
}
