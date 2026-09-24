using N3O.Umbraco.Extensions;
using System.Text.RegularExpressions;

namespace N3O.Umbraco.Video.YouTube.Extensions;

public static class StringExtensions {
    private static readonly Regex VideoIdRegex = new(@"(youtu\.be/|youtube(-nocookie)?\.com/(watch/?\?([^#]*&)?v=|(embed|live|shorts|v)/))(?!videoseries)(?<videoId>[A-Za-z0-9_-]{11})(?![A-Za-z0-9_-])",
                                                     RegexOptions.Compiled |
                                                     RegexOptions.IgnoreCase |
                                                     RegexOptions.CultureInvariant);

    public static string GetYouTubeVideoId(this string videoUrl) {
        if (!videoUrl.IsOnDomain("youtube.com", "youtu.be", "youtube-nocookie.com")) {
            return null;
        }

        var match = VideoIdRegex.Match(videoUrl);

        if (match.Success) {
            return match.Groups["videoId"].Value;
        } else {
            return null;
        }
    }
}
