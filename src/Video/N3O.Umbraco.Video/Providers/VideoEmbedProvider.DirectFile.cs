using N3O.Umbraco.Extensions;
using System.Text.RegularExpressions;

namespace N3O.Umbraco.Video;

public class DirectFileVideoEmbedProvider : IVideoEmbedProvider {
    private static readonly Regex Pattern = new(
        @"\.(mp4|webm|ogg|m4v|mov)(\?.*)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public bool CanEmbed(string url) => url.HasValue() && Pattern.IsMatch(url);

    public VideoEmbed Embed(string url) => VideoEmbed.Video(url);
}
