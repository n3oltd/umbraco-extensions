using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace N3O.Umbraco.MediaEditorMigration.Cli;

public static class SourceParsers {
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase) {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif", ".svg"
    };

    public static SourceFile ParseCropper(string textValue) {
        var obj = TryParseObject(textValue);

        if (obj == null) {
            return null;
        }

        var src = (string) obj["src"];
        var filename = (string) obj["filename"];

        if (string.IsNullOrWhiteSpace(src) && string.IsNullOrWhiteSpace(filename)) {
            return null;
        }

        var file = BuildBase(src, filename, (string) obj["mediaId"], (string) obj["altText"]);
        file.Width = (int?) obj["width"];
        file.Height = (int?) obj["height"];

        if (obj["crops"] is JArray crops) {
            foreach (var crop in crops.OfType<JObject>()) {
                file.Crops.Add(new CropRect {
                    X = (int?) crop["x"] ?? 0,
                    Y = (int?) crop["y"] ?? 0,
                    Width = (int?) crop["width"] ?? 0,
                    Height = (int?) crop["height"] ?? 0
                });
            }
        }

        return file;
    }

    public static SourceFile ParseUploader(string textValue) {
        var obj = TryParseObject(textValue);

        if (obj == null) {
            return null;
        }

        var urlPath = (string) obj["urlPath"];
        var filename = (string) obj["filename"];

        if (string.IsNullOrWhiteSpace(urlPath) && string.IsNullOrWhiteSpace(filename)) {
            return null;
        }

        var file = BuildBase(urlPath, filename, mediaId: null, altText: (string) obj["altText"]);

        var sizeMb = (double?) obj["sizeMb"];
        if (sizeMb.HasValue && sizeMb.Value > 0) {
            file.Bytes = (long) Math.Round(sizeMb.Value * 1024 * 1024);
        }

        return file;
    }

    private static SourceFile BuildBase(string src, string filename, string mediaId, string altText) {
        src = src?.Trim();
        filename = filename?.Trim();

        if (string.IsNullOrWhiteSpace(filename) && !string.IsNullOrWhiteSpace(src)) {
            filename = src.Split('/').LastOrDefault();
        }

        if (string.IsNullOrWhiteSpace(mediaId) && !string.IsNullOrWhiteSpace(src)) {
            var parts = src.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3 && string.Equals(parts[0], "media", StringComparison.OrdinalIgnoreCase)) {
                mediaId = parts[parts.Length - 2];
            }
        }

        var extension = string.IsNullOrWhiteSpace(filename) ? null : Path.GetExtension(filename).ToLowerInvariant();

        return new SourceFile {
            Src = src,
            Filename = filename,
            MediaId = mediaId,
            AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim(),
            Extension = extension,
            IsImage = extension != null && ImageExtensions.Contains(extension)
        };
    }

    private static JObject TryParseObject(string textValue) {
        if (string.IsNullOrWhiteSpace(textValue)) {
            return null;
        }

        var trimmed = textValue.TrimStart();

        if (!trimmed.StartsWith("{")) {
            return null;
        }

        try {
            return JObject.Parse(textValue);
        } catch {
            return null;
        }
    }
}
