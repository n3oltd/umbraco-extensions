using N3O.Umbraco.Video.Models;

namespace N3O.Umbraco.Video;

public interface IVideoPlatform {
    bool CanEmbed(string videoUrl);
    VideoEmbed GetEmbed(string videoUrl);
}
