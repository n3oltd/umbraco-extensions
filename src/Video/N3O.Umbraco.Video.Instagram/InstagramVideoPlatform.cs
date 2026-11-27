using N3O.Umbraco.Extensions;
using N3O.Umbraco.Video.Models;
using System.Text.RegularExpressions;

namespace N3O.Umbraco.Video.Instagram;

public class InstagramVideoPlatform : IVideoPlatform {
    private static readonly Regex PostRegex = new(@"^https?://([A-Za-z0-9-]+\.)*instagram\.com/([A-Za-z0-9_.]+/)?((?<reel>reels?)|p|tv)/(?!audio/)(?<shortcode>[A-Za-z0-9_-]+)",
                                                  RegexOptions.Compiled |
                                                  RegexOptions.IgnoreCase |
                                                  RegexOptions.CultureInvariant);

    private const string ResizeScript = """
        (function () {
          var script = document.currentScript;
          var wrapper = script && script.parentNode;
          var iframe = wrapper && wrapper.querySelector('iframe');
          var cap = wrapper && wrapper.style.maxWidth;

          if (!iframe || !cap) {
            return;
          }

          var box = iframe.parentNode;
          var maxLandscapeHeightRatio = 1.2;

          window.addEventListener('message', function (e) {
            if (e.source !== iframe.contentWindow) {
              return;
            }

            var data = e.data;

            if (typeof data === 'string') {
              try {
                data = JSON.parse(data);
              } catch (error) {
                return;
              }
            }

            if (!data || data.type !== 'MEASURE' || !data.details || !(data.details.height > 0)) {
              return;
            }

            var height = data.details.height;
            var width = wrapper.offsetWidth;
            var capped = wrapper.style.maxWidth === cap;

            if (width > 0) {
              var landscape = height < width * maxLandscapeHeightRatio;

              if (capped && landscape && width >= parseFloat(cap)) {
                wrapper.style.maxWidth = 'none';
              } else if (!capped && !landscape) {
                wrapper.style.maxWidth = cap;
              }
            }

            box.style.paddingBottom = '0';
            box.style.height = height + 'px';
          });
        })();
        """;

    public bool CanEmbed(string videoUrl) {
        var absoluteUrl = videoUrl.ToAbsoluteUrl();

        return absoluteUrl != null && PostRegex.IsMatch(absoluteUrl);
    }

    public VideoEmbed GetEmbed(string videoUrl) {
        var match = PostRegex.Match(videoUrl.ToAbsoluteUrl());
        var shortcode = match.Groups["shortcode"].Value;

        if (match.Groups["reel"].Success) {
            return new VideoEmbed($"https://www.instagram.com/reel/{shortcode}/embed/", 9, 16, 540, ResizeScript);
        } else {
            return new VideoEmbed($"https://www.instagram.com/p/{shortcode}/embed/", 4, 5, 540, ResizeScript);
        }
    }
}
