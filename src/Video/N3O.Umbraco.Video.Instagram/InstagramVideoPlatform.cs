using N3O.Umbraco.Extensions;
using N3O.Umbraco.Video.Models;
using System.Text.RegularExpressions;

namespace N3O.Umbraco.Video.Instagram;

public class InstagramVideoPlatform : IVideoPlatform {
    private static readonly Regex PostRegex = new(@"^(https?:)?(//)?([A-Za-z0-9-]+\.)*instagram\.com/([A-Za-z0-9_.]+/)?((?<reel>reels?)|p|tv)/(?!audio/)(?<shortcode>[A-Za-z0-9_-]+)",
                                                  RegexOptions.Compiled |
                                                  RegexOptions.IgnoreCase |
                                                  RegexOptions.CultureInvariant);

    public bool CanEmbed(string videoUrl) {
        return videoUrl.IsOnDomain("instagram.com") && PostRegex.IsMatch(videoUrl);
    }

    public VideoEmbed GetEmbed(string videoUrl) {
        var match = PostRegex.Match(videoUrl);
        var shortcode = match.Groups["shortcode"].Value;

        if (match.Groups["reel"].Success) {
            return new VideoEmbed($"https://www.instagram.com/reel/{shortcode}/embed/", 9, 16, 540);
        } else {
            return new VideoEmbed($"https://www.instagram.com/p/{shortcode}/embed/", 4, 5, 540);
        }
    }
}
