using N3O.Umbraco.Extensions;
using System.Text.RegularExpressions;

namespace N3O.Umbraco.Video;

public class VimeoVideoEmbedProvider : IVideoEmbedProvider {
    private static readonly Regex Pattern = new(
        @"vimeo\.com/(?:video/|channels/[\w-]+/|groups/[\w-]+/videos/)?(\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public bool CanEmbed(string url) => url.HasValue() && Pattern.IsMatch(url);

    public VideoEmbed Embed(string url) {
        var match = Pattern.Match(url);

        return VideoEmbed.Iframe($"https://player.vimeo.com/video/{match.Groups[1].Value}");
    }
}
