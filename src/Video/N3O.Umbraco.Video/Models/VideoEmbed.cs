namespace N3O.Umbraco.Video.Models;

public class VideoEmbed : Value {
    public VideoEmbed(string url, int aspectRatioWidth, int aspectRatioHeight, int? maxWidth)
        : this(url, aspectRatioWidth, aspectRatioHeight, maxWidth, null) { }

    public VideoEmbed(string url, int aspectRatioWidth, int aspectRatioHeight, int? maxWidth, string script) {
        Url = url;
        AspectRatioWidth = aspectRatioWidth;
        AspectRatioHeight = aspectRatioHeight;
        MaxWidth = maxWidth;
        Script = script;
    }

    public string Url { get; }
    public int AspectRatioWidth { get; }
    public int AspectRatioHeight { get; }
    public int? MaxWidth { get; }
    public string Script { get; }
}
