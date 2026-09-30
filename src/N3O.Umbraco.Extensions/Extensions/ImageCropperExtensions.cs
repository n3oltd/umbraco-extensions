using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;
using Umbraco.Extensions;

namespace N3O.Umbraco.Extensions;

public static class ImageCropperExtensions {
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

    // altText is a member the image cropper data migration added to the stored JSON. ImageCropperValue does not
    // read it, and saving the property in the backoffice drops it.
    // TODO Remove once the sites calling AltText keep alt text in a property of its own.
    public static string AltText(this IPublishedElement content, string propertyAlias) {
        var sourceValue = content?.GetProperty(propertyAlias)?.GetSourceValue().ToSourceValueJson();

        // Umbraco also accepts a bare image URL as an image cropper value.
        if (!sourceValue.HasValue() || !sourceValue.TrimStart().StartsWith('{')) {
            return null;
        }

        var settings = new JsonSerializerSettings();
        settings.DateParseHandling = DateParseHandling.None;

        var cropperValue = JsonConvert.DeserializeObject<JObject>(sourceValue, settings);

        return (string) cropperValue["altText"];
    }
}
