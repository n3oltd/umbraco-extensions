using N3O.Umbraco.Video.Models;
using N3O.Umbraco.Video.YouTube.Extensions;
using Umbraco.Extensions;

namespace N3O.Umbraco.Video.YouTube;

public class YouTubeVideoPlatform : IVideoPlatform {
    public bool CanEmbed(string videoUrl) {
        return videoUrl.GetYouTubeVideoId() != null;
    }

    public VideoEmbed GetEmbed(string videoUrl) {
        var videoId = videoUrl.GetYouTubeVideoId();

        var host = videoUrl.InvariantContains("youtube-nocookie.com")
                       ? "https://www.youtube-nocookie.com"
                       : "https://www.youtube.com";

        return new VideoEmbed($"{host}/embed/{videoId}?enablejsapi=1", 16, 9, null);
    }
}
