namespace N3O.Umbraco.Video;

public interface IVideoEmbedResolver {
    VideoEmbed Resolve(string url);
}
