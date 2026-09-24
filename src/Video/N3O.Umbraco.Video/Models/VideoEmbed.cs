namespace N3O.Umbraco.Video.Models;

public class VideoEmbed : Value {
    public VideoEmbed(string url, int aspectRatioWidth, int aspectRatioHeight, int? maxWidth) {
        Url = url;
        AspectRatioWidth = aspectRatioWidth;
        AspectRatioHeight = aspectRatioHeight;
        MaxWidth = maxWidth;
    }

    public string Url { get; }
    public int AspectRatioWidth { get; }
    public int AspectRatioHeight { get; }
    public int? MaxWidth { get; }
}
