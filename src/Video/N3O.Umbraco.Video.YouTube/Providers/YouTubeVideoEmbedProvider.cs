using N3O.Umbraco.Extensions;
using N3O.Umbraco.Video.YouTube.Extensions;
using Umbraco.Extensions;

namespace N3O.Umbraco.Video.YouTube;

public class YouTubeVideoEmbedProvider : IVideoEmbedProvider {
    public bool CanEmbed(string url) => url.HasValue() && url.GetYouTubeVideoId().HasValue();

    public VideoEmbed Embed(string url) {
        var videoId = url.GetYouTubeVideoId();

        var host = url.InvariantContains("youtube-nocookie.com")
                       ? "https://www.youtube-nocookie.com"
                       : "https://www.youtube.com";

        return VideoEmbed.Iframe($"{host}/embed/{videoId}?enablejsapi=1");
    }
}
