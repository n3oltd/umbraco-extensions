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

    public static string AltText(this IPublishedElement content, string propertyAlias) {
        return content?.Value<string>(propertyAlias + "AltText").TrimOrNull();
    }
}
