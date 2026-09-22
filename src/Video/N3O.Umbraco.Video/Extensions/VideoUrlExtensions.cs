using N3O.Umbraco.Extensions;
using System.Net;
using System.Text.RegularExpressions;
using Umbraco.Extensions;

namespace N3O.Umbraco.Video.Extensions;

public static class VideoUrlExtensions {
    private static readonly Regex YouTube = new(
        @"((?<=(v|V)/)|(?<=be/)|(?<=(\?|\&)v=)|(?<=embed/)|(?<=shorts/)|(?<=live/))([\w-]+)",
        RegexOptions.Compiled);

    private static readonly Regex Vimeo = new(
        @"vimeo\.com/(?:video/|channels/[\w-]+/|groups/[\w-]+/videos/)?(\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex Facebook = new(
        @"facebook\.com/(?:[^/]+/videos/|watch/?\?v=|share/v/|video\.php\?v=|reel/)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex Dailymotion = new(
        @"(?:dailymotion\.com/(?:embed/)?video/|dai\.ly/)([a-zA-Z0-9]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex TikTok = new(
        @"tiktok\.com/@[\w.-]+/video/(\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex Instagram = new(
        @"instagram\.com/(p|reel|tv)/([\w-]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex Loom = new(
        @"loom\.com/(?:share|embed)/([\w-]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex Streamable = new(
        @"streamable\.com/(?:e/)?([\w-]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex DirectFile = new(
        @"\.(mp4|webm|ogg|m4v|mov)(\?.*)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string GetYouTubeEmbedUrl(this string url) {
        if (!url.HasValue()) {
            return null;
        }

        var match = YouTube.Match(url);

        if (!match.Success) {
            return null;
        }

        var host = url.InvariantContains("youtube-nocookie.com")
                       ? "https://www.youtube-nocookie.com"
                       : "https://www.youtube.com";

        return $"{host}/embed/{match.Groups[0].Value}?enablejsapi=1";
    }

    public static string GetVimeoEmbedUrl(this string url) {
        if (!url.HasValue()) {
            return null;
        }

        var match = Vimeo.Match(url);

        return match.Success ? $"https://player.vimeo.com/video/{match.Groups[1].Value}" : null;
    }

    public static string GetFacebookEmbedUrl(this string url) {
        if (!url.HasValue() || !Facebook.IsMatch(url)) {
            return null;
        }

        return $"https://www.facebook.com/plugins/video.php?href={WebUtility.UrlEncode(url)}&show_text=false";
    }

    public static string GetDailymotionEmbedUrl(this string url) {
        if (!url.HasValue()) {
            return null;
        }

        var match = Dailymotion.Match(url);

        return match.Success ? $"https://www.dailymotion.com/embed/video/{match.Groups[1].Value}" : null;
    }

    public static string GetTikTokEmbedUrl(this string url) {
        if (!url.HasValue()) {
            return null;
        }

        var match = TikTok.Match(url);

        return match.Success ? $"https://www.tiktok.com/embed/v2/{match.Groups[1].Value}" : null;
    }

    public static string GetInstagramEmbedUrl(this string url) {
        if (!url.HasValue()) {
            return null;
        }

        var match = Instagram.Match(url);

        return match.Success
                   ? $"https://www.instagram.com/{match.Groups[1].Value}/{match.Groups[2].Value}/embed/"
                   : null;
    }

    public static string GetLoomEmbedUrl(this string url) {
        if (!url.HasValue()) {
            return null;
        }

        var match = Loom.Match(url);

        return match.Success ? $"https://www.loom.com/embed/{match.Groups[1].Value}" : null;
    }

    public static string GetStreamableEmbedUrl(this string url) {
        if (!url.HasValue()) {
            return null;
        }

        var match = Streamable.Match(url);

        return match.Success ? $"https://streamable.com/e/{match.Groups[1].Value}" : null;
    }

    public static bool IsDirectVideoFileUrl(this string url) {
        return url.HasValue() && DirectFile.IsMatch(url);
    }
}
