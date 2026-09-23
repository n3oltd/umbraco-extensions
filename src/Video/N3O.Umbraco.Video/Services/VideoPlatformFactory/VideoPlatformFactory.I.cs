namespace N3O.Umbraco.Video;

public interface IVideoPlatformFactory {
    IVideoPlatform GetPlatform(string videoUrl);
}
