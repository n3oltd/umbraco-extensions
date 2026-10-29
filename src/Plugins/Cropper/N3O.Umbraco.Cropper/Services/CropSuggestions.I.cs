using N3O.Umbraco.Cropper.DataTypes;
using System.Collections.Generic;

namespace N3O.Umbraco.Cropper;

public interface ICropSuggestions {
    IReadOnlyList<CropperSource.Crop> Suggest(CropperConfiguration configuration,
                                              string mediaId,
                                              int width,
                                              int height);
}
