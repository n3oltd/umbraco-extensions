using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace N3O.Umbraco.MediaEditorMigration.Cli;

public static class NativeValueBuilder {
    public static (string Json, CropOutcome Crops) BuildImageCropperValue(
        SourceFile file, IReadOnlyList<CropDefinition> cropDefinitions) {

        var (crops, outcome) = BuildCrops(file, cropDefinitions);

        var value = new JObject {
            ["src"] = file.Src,
            ["crops"] = crops,
            ["focalPoint"] = null
        };

        if (!string.IsNullOrWhiteSpace(file.AltText)) {
            value["altText"] = file.AltText;
        }

        return (JsonConvert.SerializeObject(value), outcome);
    }

    public static string BuildImageCropperConfig(IReadOnlyList<CropDefinition> cropDefinitions) {
        var crops = new JArray();

        foreach (var def in cropDefinitions) {
            crops.Add(new JObject {
                ["alias"] = def.Alias,
                ["width"] = def.Width,
                ["height"] = def.Height
            });
        }

        return JsonConvert.SerializeObject(new JObject { ["crops"] = crops });
    }

    public static string BuildUploadFieldConfig(string allowedExtensions) {
        var extensions = new JArray();

        foreach (var extension in ParseExtensions(allowedExtensions)) {
            extensions.Add(extension);
        }

        return JsonConvert.SerializeObject(new JObject { ["fileExtensions"] = extensions });
    }

    private static (JArray Crops, CropOutcome Outcome) BuildCrops(
        SourceFile file, IReadOnlyList<CropDefinition> cropDefinitions) {

        var outcome = new CropOutcome();
        var crops = new JArray();

        for (var i = 0; i < cropDefinitions.Count; i++) {
            var def = cropDefinitions[i];
            var crop = new JObject {
                ["alias"] = def.Alias,
                ["width"] = def.Width,
                ["height"] = def.Height
            };

            var rect = i < file.Crops.Count ? file.Crops[i] : null;
            var coordinates = ToCoordinates(rect, file.Width, file.Height);

            if (coordinates != null) {
                crop["coordinates"] = coordinates;
            } else {
                crop["coordinates"] = null;

                if (rect != null && (rect.Width > 0 || rect.Height > 0)) {
                    outcome.WithoutCoordinates.Add(def.Alias);
                }
            }

            crops.Add(crop);
        }

        for (var i = cropDefinitions.Count; i < file.Crops.Count; i++) {
            var rect = file.Crops[i];

            if (rect.Width > 0 || rect.Height > 0) {
                outcome.DroppedRectangles++;
            }
        }

        return (crops, outcome);
    }

    private static IReadOnlyList<string> ParseExtensions(string allowedExtensions) {
        if (string.IsNullOrWhiteSpace(allowedExtensions)) {
            return Array.Empty<string>();
        }

        return allowedExtensions.Split(',')
                                .Select(x => x.Trim().TrimStart('.').ToLowerInvariant())
                                .Where(x => x.Length > 0)
                                .Distinct(StringComparer.Ordinal)
                                .ToList();
    }

    public static (string Json, CropOutcome Crops) BuildPickerValue(
        Guid mediaKey, SourceFile file, IReadOnlyList<CropDefinition> cropDefinitions) {

        var (crops, outcome) = BuildCrops(file, cropDefinitions);

        var item = new JObject {
            ["key"] = Guid.NewGuid().ToString(),
            ["mediaKey"] = mediaKey.ToString(),
            ["crops"] = crops,
            ["focalPoint"] = null
        };

        return (JsonConvert.SerializeObject(new JArray(item)), outcome);
    }

    public static string BuildMediaPickerConfig(IReadOnlyList<CropDefinition> cropDefinitions,
                                                bool enableLocalFocalPoint) {
        var crops = new JArray();

        foreach (var def in cropDefinitions) {
            crops.Add(new JObject {
                ["alias"] = def.Alias,
                ["width"] = def.Width,
                ["height"] = def.Height
            });
        }

        var config = new JObject {
            ["filter"] = "",
            ["multiple"] = false,
            ["startNodeId"] = null,
            ["ignoreUserStartNodes"] = false,
            ["enableLocalFocalPoint"] = enableLocalFocalPoint,
            ["crops"] = crops,
            ["validationLimit"] = new JObject { ["min"] = 0, ["max"] = 1 }
        };

        return JsonConvert.SerializeObject(config);
    }

    public static string BuildUmbracoFileValue(SourceFile file) {
        if (!file.IsImage) {
            return file.Src;
        }

        var value = new JObject {
            ["src"] = file.Src,
            ["crops"] = new JArray(),
            ["focalPoint"] = new JObject { ["left"] = 0.5, ["top"] = 0.5 }
        };

        return JsonConvert.SerializeObject(value);
    }

    // x2 and y2 are insets from the right and bottom edges, not corners.
    private static JObject ToCoordinates(CropRect rect, int? imageWidth, int? imageHeight) {
        if (rect == null || imageWidth is not > 0 || imageHeight is not > 0 || rect.Width <= 0 || rect.Height <= 0) {
            return null;
        }

        double w = imageWidth.Value;
        double h = imageHeight.Value;

        return new JObject {
            ["x1"] = Clamp(rect.X / w),
            ["y1"] = Clamp(rect.Y / h),
            ["x2"] = Clamp((double) (imageWidth.Value - (rect.X + rect.Width)) / w),
            ["y2"] = Clamp((double) (imageHeight.Value - (rect.Y + rect.Height)) / h)
        };
    }

    private static double Clamp(double value) {
        if (value < 0) return 0;
        if (value > 1) return 1;

        return Math.Round(value, 10);
    }
}
