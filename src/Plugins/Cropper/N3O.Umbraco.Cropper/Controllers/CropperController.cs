using Microsoft.AspNetCore.Mvc;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Cropper.DataTypes;
using N3O.Umbraco.Cropper.Models;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Plugins.Extensions;
using N3O.Umbraco.Plugins.Models;
using N3O.Umbraco.Validation;
using N3O.Umbraco.Validation.Hosting.Controllers;
using NodaTime;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace N3O.Umbraco.Cropper.Controllers;

[ApiDocument(CropperConstants.ApiName)]
public class CropperController : ValidatingPluginController {
    private const int JpegQuality = 95;

    private readonly IClock _clock;
    private readonly MediaFileManager _mediaFileManager;
    private readonly IDataTypeService _dataTypeService;
    private readonly ICropSuggestions _cropSuggestions;

    public CropperController(IClock clock,
                             MediaFileManager mediaFileManager,
                             IDataTypeService dataTypeService,
                             ICropSuggestions cropSuggestions,
                             IValidation validation,
                             Lazy<IValidationHandler> validationHandler) 
        : base(validation, validationHandler) {
        _clock = clock;
        _mediaFileManager = mediaFileManager;
        _dataTypeService = dataTypeService;
        _cropSuggestions = cropSuggestions;
    }

    [HttpGet("media/{mediaId}")]
    public ActionResult<ImageMedia> GetMediaById(string mediaId) {
        var file = _mediaFileManager.GetSourceFile(mediaId);

        if (file == null) {
            return NotFound();
        }

        using (var stream = _mediaFileManager.FileSystem.OpenFile(file)) {
            var metadata = stream.GetImageMetadata();

            return Ok(GetResponse(file, metadata.Height, metadata.Width));
        }
    }

    [HttpGet("media/{mediaId}/suggestedCrops/{dataTypeKey:guid}")]
    public ActionResult<IEnumerable<CropperSource.Crop>> GetSuggestedCrops(string mediaId, Guid dataTypeKey) {
        var file = _mediaFileManager.GetSourceFile(mediaId);
        var dataType = _dataTypeService.GetDataType(dataTypeKey);

        if (file == null || dataType?.EditorAlias.EqualsInvariant(CropperConstants.PropertyEditorAlias) != true) {
            return NotFound();
        }

        using (var stream = _mediaFileManager.FileSystem.OpenFile(file)) {
            var metadata = stream.GetImageMetadata();
            var configuration = dataType.ConfigurationAs<CropperConfiguration>();

            return Ok(_cropSuggestions.Suggest(configuration, mediaId, metadata.Width, metadata.Height));
        }
    }

    [HttpPost("upload")]
    public async Task<ActionResult<ImageMedia>> Upload([FromForm] ImageUploadReq req) {
        await ValidateAsync(req);
        
        var instant = _clock.GetCurrentInstant();
    
        using (var uploadedImage = await GetUploadedImageAsync(req)) {
            if (uploadedImage == null) {
                return BadRequest();
            }

            var storagePath = uploadedImage.Filename.GetStoragePath(instant);

            using (var image = await Image.LoadAsync(uploadedImage.Stream)) {
                if (IsNotUpright(image)) {
                    image.Mutate(x => x.AutoOrient());
                    image.Metadata.ExifProfile.RemoveValue(ExifTag.Orientation);

                    using (var uprightStream = new MemoryStream()) {
                        await image.SaveAsync(uprightStream, GetEncoder(image));

                        uprightStream.Rewind();

                        _mediaFileManager.FileSystem.AddFile(storagePath, uprightStream, false);
                    }
                } else {
                    uploadedImage.Stream.Rewind();

                    _mediaFileManager.FileSystem.AddFile(storagePath, uploadedImage.Stream, false);
                }

                return Ok(GetResponse(storagePath, image.Height, image.Width));
            }
        }
    }

    private IImageEncoder GetEncoder(Image image) {
        var format = image.Metadata.DecodedImageFormat;

        if (format == JpegFormat.Instance) {
            return new JpegEncoder { Quality = JpegQuality };
        } else {
            return image.Configuration.ImageFormatsManager.GetEncoder(format);
        }
    }

    private ImageMedia GetResponse(string storagePath, int height, int width) {
        var mediaId = Path.GetDirectoryName(storagePath);
        var filename = Path.GetFileName(storagePath);

        return new ImageMedia {
            UrlPath = "/media/" + mediaId + "/" + filename,
            MediaId = mediaId,
            Filename = filename,
            Height = height,
            Width = width
        };
    }

    private bool IsNotUpright(Image image) {
        if (image.Metadata.ExifProfile?.TryGetValue(ExifTag.Orientation, out var orientation) == true) {
            return orientation.Value > ExifOrientationMode.TopLeft;
        } else {
            return false;
        }
    }
}
