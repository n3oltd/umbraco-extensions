using Newtonsoft.Json.Linq;
using System.Linq;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;
using Umbraco.Extensions;

namespace N3O.Umbraco.Extensions;

// Helpers for Umbraco.ImageCropper, which is what the retired N3O Cropper became. Unlike Umbraco.MediaPicker3
// (see IMediaUrl) an image cropper on a document or element type keeps the file and its crops ON THE PROPERTY,
// with no media library node behind it — so neither the media node's URL helpers nor its Name are available.
public static class ImageCropperExtensions {
    // Umbraco has no GetCropUrl overload that takes an ImageCropperValue as its receiver; the local-crop form
    // is string.GetCropUrl(ImageCropperValue, cropAlias: ...). It also requires a crop alias, which the retired
    // Cropper's single "Crop" never had to name, so the alias defaults to the value's own first crop.
    public static string CropUrl(this ImageCropperValue value) {
        return value.CropUrl(value?.Crops?.FirstOrDefault()?.Alias);
    }

    public static string CropUrl(this ImageCropperValue value, string cropAlias) {
        if (value?.Src == null) {
            return null;
        }

        if (!cropAlias.HasValue()) {
            return value.Src;
        }

        return value.Src.GetCropUrl(value, cropAlias: cropAlias, useCropDimensions: true);
    }

    // Alt text has no slot on Umbraco.ImageCropper (or Umbraco.UploadField), and with the file on the property
    // there is no media node whose name could carry it — Umbraco's own guidance for a media picker is
    // alt="@Model.Photo.Name", which does not apply here.
    //
    // The Cropper→ImageCropper data migration therefore preserves the original alt text as a non-standard
    // "altText" member of the stored JSON. ImageCropperValue ignores members it does not know, so nothing
    // breaks, but for the same reason the text cannot be read back off the typed value — it has to come from
    // the property's raw source value, which is what this does.
    //
    // IMPORTANT: this is migration continuity, not a permanent home. The value survives publishing, but the
    // moment an editor saves that property in the backoffice Umbraco re-serialises it from ImageCropperValue
    // and the preserved text is gone. Anything needing durable alt text wants a real property for it.
    public static string AltText(this IPublishedElement content, string propertyAlias) {
        var sourceValue = content?.GetProperty(propertyAlias)?.GetSourceValue() as string;

        if (!sourceValue.HasValue()) {
            return null;
        }

        var trimmed = sourceValue.TrimStart();

        if (trimmed.Length == 0 || trimmed[0] != '{') {
            return null;
        }

        try {
            return (string) JObject.Parse(sourceValue)["altText"];
        } catch {
            return null;
        }
    }
}
