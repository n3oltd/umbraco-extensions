using N3O.Umbraco.Cropper.DataTypes;
using N3O.Umbraco.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.IO;

namespace N3O.Umbraco.Cropper;

public class ImageCropper : IImageCropper {
    private readonly MediaFileManager _mediaFileManager;

    public ImageCropper(MediaFileManager mediaFileManager) {
        _mediaFileManager = mediaFileManager;
    }

    public async Task CropAllAsync(CropperConfiguration configuration,
                                   CropperSource source,
                                   CancellationToken cancellationToken = default) {
        foreach (var (crop, index) in source.Crops.SelectWithIndex()) {
            var cropDefinition = configuration.CropDefinitions.ElementAtOrDefault(index);

            if (cropDefinition != null) {
                await CropAsync(cropDefinition, crop, source, cancellationToken);
            }
        }
    }

    public async Task CropAsync(CropDefinition definition,
                                CropperSource.Crop crop,
                                CropperSource source,
                                CancellationToken cancellationToken = default) {
        using (var srcStream = _mediaFileManager.FileSystem.OpenFile($"{source.MediaId}/{source.Filename}")) {
            using (var destStream = new MemoryStream()) {
                using (var image = await Image.LoadAsync(srcStream, cancellationToken)) {
                    var clone = image.Clone(i => {
                        if (IsMeasuredOnDisplayedImage(crop)) {
                            i.AutoOrient();
                        }

                        i.Crop(new Rectangle(crop.X, crop.Y, crop.Width, crop.Height));

                        i.Resize(definition.Width, definition.Height);
                    });

                    await clone.SaveAsync(destStream, image.Metadata.DecodedImageFormat, cancellationToken);
                }
                
                destStream.Rewind();
                
                var destFilename = CropFilename.Generate(source.MediaId, source.Filename, definition, crop);
                var destPath = Path.Combine(source.MediaId, destFilename);
                
                _mediaFileManager.FileSystem.AddFile(destPath, destStream, true);
            }
        }
    }

    private static bool IsMeasuredOnDisplayedImage(CropperSource.Crop crop) {
        return crop.Rotate % 180 == 0;
    }
}
