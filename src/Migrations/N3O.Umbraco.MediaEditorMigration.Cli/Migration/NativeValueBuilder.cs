using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace N3O.Umbraco.MediaEditorMigration.Cli;

// Native Umbraco JSON shapes for both --target values. Pure functions, no DB access.
public static class NativeValueBuilder {
    // Umbraco's ImageCropperConfigurationExtensions.ApplyConfiguration rebuilds the crop list from the DATA
    // TYPE config on read, matching by alias, so an alias missing from the config is discarded — which is why
    // the same cropDefinitions go into both the value and the config.
    public static (string Json, CropOutcome Crops) BuildImageCropperValue(
        SourceFile file, IReadOnlyList<CropDefinition> cropDefinitions) {

        var (crops, outcome) = BuildCrops(file, cropDefinitions);

        // focalPoint stays null: the retired Cropper had none, and null makes an uncoordinated crop fall back
        // to a centre crop rather than to an invented focal point.
        var value = new JObject {
            ["src"] = file.Src,
            ["crops"] = crops,
            ["focalPoint"] = null
        };

        // No alt-text slot on this editor and no media node to name, so the text rides along as a non-standard
        // member. Inert to Umbraco (ImageCropperValue ignores unknown members) and read back by
        // IPublishedElement.AltText(alias). Migration continuity only: a backoffice re-save loses it.
        if (!string.IsNullOrWhiteSpace(file.AltText)) {
            value["altText"] = file.AltText;
        }

        return (JsonConvert.SerializeObject(value), outcome);
    }

    // ImageCropperConfiguration is a single [ConfigurationField("crops")] of {alias, width, height}.
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

    // The old Uploader stored ".png, .jpg"; UploadField wants ["png","jpg"], and an empty array means "no
    // restriction" just as an unset allowedExtensions did. maxFileSizeMb, imagesOnly and altTextRequired have
    // no native equivalent and are dropped.
    public static string BuildUploadFieldConfig(string allowedExtensions) {
        var extensions = new JArray();

        foreach (var extension in ParseExtensions(allowedExtensions)) {
            extensions.Add(extension);
        }

        return JsonConvert.SerializeObject(new JObject { ["fileExtensions"] = extensions });
    }

    // ImageCropper crops and MediaPicker3 local crops are the same shape, so one builder serves both.
    //
    // The old Cropper stored rectangles POSITIONALLY — rectangle i belongs to cropDefinitions[i], with no alias
    // on the rectangle. Hence definitions drive the loop; surplus rectangles mean definitions were removed
    // after the value was saved, so they have no alias to be written under and count as dropped.
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
                // No coordinates: the crop falls back to the focal point, or to a default centre crop when
                // there is none. Flag for a manual check only when there WAS a stored rectangle we couldn't
                // convert (missing image dimensions).
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

    // ---------------------------------------------------------------------------------------------------
    // --target mediapicker
    // ---------------------------------------------------------------------------------------------------

    // The MediaPicker3 stored property value: a JSON array of one media item (these editors were single-value).
    // crops/focalPoint are only populated for Cropper. Returns the serialised JSON plus what the caller must
    // flag for review — see BuildCrops.
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

    // The data type config when flipping to Umbraco.MediaPicker3. Single item; crops carried over for Cropper.
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

    // The umbracoFile value stored on a new media node. Image media uses the ImageCropper editor (JSON with a
    // default centre focal point); File media uses the Upload editor (a plain path string).
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

    // ---------------------------------------------------------------------------------------------------

    // Absolute pixel rectangle → relative-fraction coordinates {x1,y1,x2,y2} in [0,1]. These are INSETS from
    // each edge, not corners: Umbraco passes them straight to ImageSharp's crop processor as
    // "left,top,right,bottom". Null when the rectangle or the source image dimensions are missing/zero (can't
    // convert without the original width/height).
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
