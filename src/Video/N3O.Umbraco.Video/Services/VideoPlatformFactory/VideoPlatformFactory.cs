using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.Video;

public class VideoPlatformFactory : IVideoPlatformFactory {
    private readonly IEnumerable<IVideoPlatform> _videoPlatforms;

    public VideoPlatformFactory(IEnumerable<IVideoPlatform> videoPlatforms) {
        _videoPlatforms = videoPlatforms;
    }

    public IVideoPlatform GetPlatform(string videoUrl) {
        var videoPlatform = _videoPlatforms.FirstOrDefault(x => x.CanEmbed(videoUrl));

        return videoPlatform;
    }
}
