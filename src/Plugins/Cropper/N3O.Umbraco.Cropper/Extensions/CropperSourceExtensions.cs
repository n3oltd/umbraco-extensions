using N3O.Umbraco.Cropper.DataTypes;
using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.Cropper.Extensions;

public static class CropperSourceExtensions {
    public static IReadOnlyList<(CropDefinition Definition, CropperSource.Crop Crop)> GetCropsWithDefinitions(this CropperSource cropperSource,
                                                                                                          CropperConfiguration configuration,
                                                                                                          string propertyAlias) {
        var cropsWithDefinitions = new List<(CropDefinition Definition, CropperSource.Crop Crop)>();

        foreach (var (crop, index) in cropperSource.Crops.SelectWithIndex()) {
            var definition = configuration.CropDefinitions.ElementAtOrDefault(index);

            if (definition != null) {
                if (crop == null) {
                    throw new Exception($"Property {propertyAlias} has no crop for crop definition {definition.Alias}");
                } else {
                    cropsWithDefinitions.Add((definition, crop));
                }
            }
        }

        return cropsWithDefinitions;
    }

    public static CropperSource.Crop GetLargestCrop(this CropperSource cropperSource) {
        return cropperSource.Crops.OrderByDescending(c => c.Width * c.Height).First();
    }
}