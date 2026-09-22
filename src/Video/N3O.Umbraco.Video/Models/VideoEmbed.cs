using System.Collections.Generic;

namespace N3O.Umbraco.Video;

public class VideoEmbed {
    private VideoEmbed(string tagName, string src, IReadOnlyDictionary<string, string> defaultAttributes) {
        TagName = tagName;
        Src = src;
        DefaultAttributes = defaultAttributes;
    }

    public string TagName { get; }
    public string Src { get; }
    public IReadOnlyDictionary<string, string> DefaultAttributes { get; }

    public static VideoEmbed Iframe(string src) {
        return new VideoEmbed("iframe", src, new Dictionary<string, string> {
            ["frameborder"] = "0",
            ["allowfullscreen"] = "true"
        });
    }

    public static VideoEmbed Video(string src) {
        return new VideoEmbed("video", src, new Dictionary<string, string> {
            ["controls"] = "true"
        });
    }
}
