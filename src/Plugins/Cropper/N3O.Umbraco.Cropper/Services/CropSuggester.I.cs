using N3O.Umbraco.Cropper.DataTypes;
using System.IO;

namespace N3O.Umbraco.Cropper;

public interface ICropSuggester {
    string Alias { get; }

    CropperSource.Crop Suggest(Stream image, int width, int height, CropDefinition cropDefinition);
}
