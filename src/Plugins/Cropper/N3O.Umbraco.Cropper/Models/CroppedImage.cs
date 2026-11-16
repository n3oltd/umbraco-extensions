using N3O.Umbraco.Cropper.DataTypes;
using N3O.Umbraco.Cropper.Extensions;
using N3O.Umbraco.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace N3O.Umbraco.Cropper.Models;

public class CroppedImage : DynamicObject, IEnumerable<ImageCrop> {
    private readonly Dictionary<string, ImageCrop> _crops = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly CropperSource _cropperSource;

    public CroppedImage(IUrlBuilder urlBuilder,
                        CropperConfiguration configuration,
                        CropperSource cropperSource,
                        string propertyAlias) {
        _cropperSource = cropperSource;
        AltText = cropperSource.AltText;

        foreach (var (definition, crop) in cropperSource.GetCropsWithDefinitions(configuration, propertyAlias)) {
            AddImageCrop(urlBuilder, definition, cropperSource.MediaId, cropperSource.Filename, crop);
        }
    }

    public Uri GetUncroppedUrl(IUrlBuilder urlBuilder) {
        return urlBuilder.Root().AppendPathSegment(_cropperSource.Src).ToUri();
    }
    
    public CropperSource GetUncroppedImage() {
        return _cropperSource;
    }

    public bool HasCrop(string alias) {
        return _crops.ContainsKey(alias);
    }

    public string AltText { get; }
    public ImageCrop Crop => this.Single();

    public override bool TryGetMember(GetMemberBinder binder, out object result) {
        var alias = binder.Name;
        var exists = _crops.ContainsKey(alias);
        var crop = exists ? _crops[alias] : null;

        result = crop;

        return exists;
    }

    public IEnumerator<ImageCrop> GetEnumerator() {
        return _crops.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return _crops.Values.GetEnumerator();
    }

    public ImageCrop this[string alias] => _crops[alias];

    private void AddImageCrop(IUrlBuilder urlBuilder,
                              CropDefinition definition,
                              string mediaId,
                              string sourceFile,
                              CropperSource.Crop crop) {
        var src = ImagePath.Get(mediaId, sourceFile, definition, crop);
        var url = urlBuilder.Root().AppendPathSegment(src);
        
        var imageCrop = new ImageCrop(definition.Alias,
                                      src,
                                      url,
                                      definition.Height,
                                      definition.Width);

        _crops[definition.Alias] = imageCrop;
    }
}
