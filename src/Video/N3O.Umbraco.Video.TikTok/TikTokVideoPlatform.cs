using N3O.Umbraco.Extensions;
using N3O.Umbraco.Video.Models;
using System.Text.RegularExpressions;

namespace N3O.Umbraco.Video.TikTok;

public class TikTokVideoPlatform : IVideoPlatform {
    private static readonly Regex VideoRegex = new(@"^(https?:)?(//)?([A-Za-z0-9-]+\.)*tiktok\.com/@[A-Za-z0-9_.-]+/video/(?<videoId>[0-9]+)",
                                                   RegexOptions.Compiled |
                                                   RegexOptions.IgnoreCase |
                                                   RegexOptions.CultureInvariant);

    public bool CanEmbed(string videoUrl) {
        return videoUrl.IsOnDomain("tiktok.com") && VideoRegex.IsMatch(videoUrl);
    }

    public VideoEmbed GetEmbed(string videoUrl) {
        var videoId = VideoRegex.Match(videoUrl).Groups["videoId"].Value;

        return new VideoEmbed($"https://www.tiktok.com/player/v1/{videoId}", 9, 16, 360);
    }
}
