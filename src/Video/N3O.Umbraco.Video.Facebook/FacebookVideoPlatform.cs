using N3O.Umbraco.Extensions;
using N3O.Umbraco.Video.Models;
using System.Net;
using System.Text.RegularExpressions;

namespace N3O.Umbraco.Video.Facebook;

public class FacebookVideoPlatform : IVideoPlatform {
    private static readonly Regex VideoRegex = new(@"^https?://([A-Za-z0-9-]+\.)*facebook\.com/([^/?#]+/videos/[0-9]+|watch/?\?([^#]*&)?v=[0-9]+|video\.php\?([^#]*&)?v=[0-9]+|share/v/[A-Za-z0-9]+|(?<reel>reel/[0-9]+|share/r/[A-Za-z0-9]+))",
                                                   RegexOptions.Compiled |
                                                   RegexOptions.IgnoreCase |
                                                   RegexOptions.CultureInvariant);

    public bool CanEmbed(string videoUrl) {
        var absoluteUrl = videoUrl.ToAbsoluteUrl();

        return absoluteUrl != null && VideoRegex.IsMatch(absoluteUrl);
    }

    public VideoEmbed GetEmbed(string videoUrl) {
        var absoluteUrl = videoUrl.ToAbsoluteUrl();
        var href = WebUtility.UrlEncode(absoluteUrl);
        var embedUrl = $"https://www.facebook.com/plugins/video.php?href={href}&show_text=false";

        if (VideoRegex.Match(absoluteUrl).Groups["reel"].Success) {
            return new VideoEmbed(embedUrl, 9, 16, 360);
        } else {
            return new VideoEmbed(embedUrl, 16, 9, null);
        }
    }
}
