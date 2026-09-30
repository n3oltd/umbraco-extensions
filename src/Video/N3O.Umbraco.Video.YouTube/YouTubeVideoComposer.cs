using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace N3O.Umbraco.Video.YouTube;

public class YouTubeVideoComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        builder.Services.AddSingleton<IYouTube, YouTube>();
        builder.Services.AddTransient<YouTubeVideoPlatform>();
    }
}
