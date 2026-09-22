using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Composing;
using N3O.Umbraco.Extensions;
using Umbraco.Cms.Core.DependencyInjection;

namespace N3O.Umbraco.Video;

public class VideoEmbedComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        builder.Services.AddTransient<IVideoEmbedResolver, VideoEmbedResolver>();

        RegisterAll(t => t.ImplementsInterface<IVideoEmbedProvider>(),
                    t => builder.Services.AddTransient(typeof(IVideoEmbedProvider), t));
    }
}
