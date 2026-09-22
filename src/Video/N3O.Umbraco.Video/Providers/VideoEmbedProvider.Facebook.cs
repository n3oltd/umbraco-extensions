using N3O.Umbraco.Extensions;
using System.Net;
using System.Text.RegularExpressions;

namespace N3O.Umbraco.Video;

public class FacebookVideoEmbedProvider : IVideoEmbedProvider {
    private static readonly Regex Pattern = new(
        @"facebook\.com/(?:[^/]+/videos/|watch/?\?v=|share/v/|video\.php\?v=|reel/)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public bool CanEmbed(string url) => url.HasValue() && Pattern.IsMatch(url);

    public VideoEmbed Embed(string url) {
        var encoded = WebUtility.UrlEncode(url);

        return VideoEmbed.Iframe($"https://www.facebook.com/plugins/video.php?href={encoded}&show_text=false");
    }
}
