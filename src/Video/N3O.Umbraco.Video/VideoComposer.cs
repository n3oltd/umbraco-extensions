using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Composing;
using N3O.Umbraco.Extensions;
using Umbraco.Cms.Core.DependencyInjection;

namespace N3O.Umbraco.Video;

public class VideoComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        builder.Services.AddTransient<IVideoPlatformFactory, VideoPlatformFactory>();

        RegisterAll(t => t.ImplementsInterface<IVideoPlatform>(),
                    t => builder.Services.AddTransient(typeof(IVideoPlatform), t));
    }
}
