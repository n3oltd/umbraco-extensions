namespace N3O.Umbraco.Video;

public interface IVideoEmbedProvider {
    bool CanEmbed(string url);
    VideoEmbed Embed(string url);
}
