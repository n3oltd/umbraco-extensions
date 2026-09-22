using N3O.Umbraco.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.Video;

public class VideoEmbedResolver : IVideoEmbedResolver {
    private readonly IEnumerable<IVideoEmbedProvider> _providers;

    public VideoEmbedResolver(IEnumerable<IVideoEmbedProvider> providers) {
        _providers = providers;
    }

    public VideoEmbed Resolve(string url) {
        if (!url.HasValue()) {
            return null;
        }

        var provider = _providers.OrEmpty().FirstOrDefault(x => x.CanEmbed(url));

        return provider?.Embed(url);
    }
}
