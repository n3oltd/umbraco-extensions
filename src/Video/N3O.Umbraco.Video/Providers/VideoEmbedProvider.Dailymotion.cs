using N3O.Umbraco.Extensions;
using System.Text.RegularExpressions;

namespace N3O.Umbraco.Video;

public class DailymotionVideoEmbedProvider : IVideoEmbedProvider {
    private static readonly Regex Pattern = new(
        @"(?:dailymotion\.com/(?:embed/)?video/|dai\.ly/)([a-zA-Z0-9]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public bool CanEmbed(string url) => url.HasValue() && Pattern.IsMatch(url);

    public VideoEmbed Embed(string url) {
        var match = Pattern.Match(url);

        return VideoEmbed.Iframe($"https://www.dailymotion.com/embed/video/{match.Groups[1].Value}");
    }
}
