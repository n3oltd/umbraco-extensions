using N3O.Umbraco.Content;
using N3O.Umbraco.Cropper.DataTypes;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Localization;
using N3O.Umbraco.Plugins.Extensions;
using N3O.Umbraco.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cropper.Content;

public class CropperPropertyBuilder : PropertyBuilder {
    private readonly MediaFileManager _mediaFileManager;
    private readonly ILocalClock _clock;
    private readonly Lazy<IVolume> _volume;
    private readonly ICropSuggestions _cropSuggestions;
    private readonly List<ICropBuilder> _cropBuilders = [];
    private string _src;
    private string _mediaId;
    private string _filename;
    private int? _width;
    private int? _height;
    private string _altText;

    public CropperPropertyBuilder(IContentTypeService contentTypeService,
                                  MediaFileManager mediaFileManager,
                                  ILocalClock clock,
                                  Lazy<IVolume> volume,
                                  ICropSuggestions cropSuggestions)
        : base(contentTypeService) {
        _mediaFileManager = mediaFileManager;
        _clock = clock;
        _volume = volume;
        _cropSuggestions = cropSuggestions;
    }

    public ICropBuilder AddCrop() {
        if (_width == null || _height == null) {
            throw new Exception("Must set image before adding crops");
        }
        
        var cropBuilder = new CropBuilder(_width.Value, _height.Value);

        _cropBuilders.Add(cropBuilder);

        return cropBuilder;
    }

    public CropperPropertyBuilder AutoCrop(CropperConfiguration configuration) {
        var suggestedCrops = _cropSuggestions.Suggest(configuration,
                                                      _mediaId,
                                                      _width.GetValueOrThrow(),
                                                      _height.GetValueOrThrow());

        foreach (var (cropDefinition, index) in configuration.CropDefinitions.SelectWithIndex()) {
            var suggestedCrop = suggestedCrops[index];

            if (suggestedCrop != null) {
                AddCrop().CropTo(suggestedCrop.X, suggestedCrop.Y, suggestedCrop.Width, suggestedCrop.Height);
            } else {
                AddCrop().AutoCrop(cropDefinition);
            }
        }

        return this;
    }
    
    public CropperPropertyBuilder SetAltText(string altText) {
        _altText = altText;

        return this;
    }
    
    public CropperPropertyBuilder SetImage(CropperSource source) {
        _src = source.Src;
        _mediaId = source.MediaId;
        _height = source.Height;
        _width = source.Width;
        _filename = source.Filename;

        return this;
    }

    public CropperPropertyBuilder SetImage(string mediaId) {
        _src = _mediaFileManager.GetSourceFile(mediaId);

        if (_src == null) {
            throw new Exception($"No media found with ID {mediaId}");
        }

        _mediaId = mediaId;

        using (var stream = _mediaFileManager.FileSystem.OpenFile(_src)) {
            var metadata = stream.GetImageMetadata();

            _height = metadata.Height;
            _width = metadata.Width;
        }

        return this;
    }

    public async Task<CropperPropertyBuilder> SetImageAsync(StorageToken storageToken) {
        var storageFolder = await _volume.Value.GetStorageFolderAsync(storageToken.StorageFolderName);
        var imageFile = await storageFolder.GetFileAsync(storageToken.Filename);

        using (imageFile.Stream) {
            SetImage(imageFile);
        }

        return this;
    }

    public CropperPropertyBuilder SetImage(Blob blob) {
        return SetImage(blob.Stream, blob.Filename);
    }
    
    public CropperPropertyBuilder SetImage(byte[] bytes, string filename) {
        using (var stream = new MemoryStream(bytes)) {
            SetImage(stream, filename);

            return this;
        }
    }

    public CropperPropertyBuilder SetImage(Stream stream, string filename) {
        var instant = _clock.GetCurrentInstant();

        stream.Rewind();

        if (!_mediaFileManager.FileSystem.FileExists(filename.GetStoragePath(instant))) {
            _mediaFileManager.FileSystem.AddFile(filename.GetStoragePath(instant), stream, false);
        }

        stream.Rewind();

        var metadata = stream.GetImageMetadata();
        _height = metadata.Height;
        _width = metadata.Width;
        _filename = filename;
        _mediaId = instant.GetMediaId();
        _src = filename.GetMediaUrlPath(instant);

        return this;
    }

    public override (object, IPropertyType) Build(string propertyAlias, string contentTypeAlias) {
        Validate();
        
        var cropperSource = new CropperSource();
        cropperSource.Filename = _filename;
        cropperSource.Height = _height.GetValueOrThrow();
        cropperSource.Width = _width.GetValueOrThrow();
        cropperSource.Src = _src;
        cropperSource.AltText = _altText;
        cropperSource.MediaId = _mediaId;
        cropperSource.Crops = _cropBuilders.Select(x => x.Build()).ToList();

        Value = JsonConvert.SerializeObject(cropperSource);

        return (Value, GetPropertyType(propertyAlias, contentTypeAlias));
    }

    private void Validate() {
        if (!_src.HasValue()) {
            throw new Exception("Image must be specified");
        }
        
        if (_cropBuilders.None()) {
            throw new Exception("At least one crop must be defined");
        }
    }
}
