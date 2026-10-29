using N3O.Umbraco.Cropper.DataTypes;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Plugins.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.IO;

namespace N3O.Umbraco.Cropper;

public class CropSuggestions : ICropSuggestions {
    private readonly IEnumerable<ICropSuggester> _cropSuggesters;
    private readonly MediaFileManager _mediaFileManager;

    public CropSuggestions(IEnumerable<ICropSuggester> cropSuggesters, MediaFileManager mediaFileManager) {
        _cropSuggesters = cropSuggesters;
        _mediaFileManager = mediaFileManager;
    }

    public IReadOnlyList<CropperSource.Crop> Suggest(CropperConfiguration configuration,
                                                     string mediaId,
                                                     int width,
                                                     int height) {
        var cropDefinitions = configuration.CropDefinitions.OrEmpty().ToList();

        if (!configuration.CropSuggester.HasValue()) {
            return cropDefinitions.Select(_ => (CropperSource.Crop) null).ToList();
        }

        var cropSuggester = _cropSuggesters.SingleOrDefault(x => x.Alias == configuration.CropSuggester);

        if (cropSuggester == null) {
            throw new Exception($"No crop suggester is registered with alias {configuration.CropSuggester}");
        }

        var file = _mediaFileManager.GetSourceFile(mediaId);

        if (file == null) {
            throw new Exception($"No media found with ID {mediaId}");
        }

        var crops = new List<CropperSource.Crop>();

        foreach (var cropDefinition in cropDefinitions) {
            using (var stream = _mediaFileManager.FileSystem.OpenFile(file)) {
                crops.Add(cropSuggester.Suggest(stream, width, height, cropDefinition));
            }
        }

        return crops;
    }
}
