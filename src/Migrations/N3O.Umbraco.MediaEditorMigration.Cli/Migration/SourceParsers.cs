using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace N3O.Umbraco.MediaEditorMigration.Cli;

// Parses a raw stored property value (textValue) into a SourceFile. Returns null when the value is empty or not
// the expected N3O shape (already migrated, or unrecognised) so the caller leaves it untouched.
public static class SourceParsers {
    // Extensions Umbraco treats as Image media (everything else becomes a File media item).
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase) {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif", ".svg"
    };

    // CropperSource: { src, mediaId, filename, width, height, altText, crops:[{x,y,width,height}] }
    public static SourceFile ParseCropper(string textValue) {
        var obj = TryParseObject(textValue);

        if (obj == null) {
            return null;
        }

        var src = (string) obj["src"];
        var filename = (string) obj["filename"];

        if (string.IsNullOrWhiteSpace(src) && string.IsNullOrWhiteSpace(filename)) {
            return null; // not a Cropper value
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

    // UploaderSource: { altText, extension, filename, sizeMb, urlPath }
    public static SourceFile ParseUploader(string textValue) {
        var obj = TryParseObject(textValue);

        if (obj == null) {
            return null;
        }

        var urlPath = (string) obj["urlPath"];
        var filename = (string) obj["filename"];

        if (string.IsNullOrWhiteSpace(urlPath) && string.IsNullOrWhiteSpace(filename)) {
            return null; // not an Uploader value
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

        // Derive filename from src if absent, and vice-versa.
        if (string.IsNullOrWhiteSpace(filename) && !string.IsNullOrWhiteSpace(src)) {
            filename = src.Split('/').LastOrDefault();
        }

        // mediaId is the folder segment of "/media/{mediaId}/{filename}".
        if (string.IsNullOrWhiteSpace(mediaId) && !string.IsNullOrWhiteSpace(src)) {
            var parts = src.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            // parts: ["media", "{mediaId}", "{filename}"]
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

        // A migrated MediaPicker3 value is a JSON array; never re-parse one as a source object.
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
