using N3O.Umbraco.Attributes;
using N3O.Umbraco.Utilities;

namespace N3O.Umbraco.Cropper.Models;

[SerializeToUrl(nameof(Src))]
public class ImageCrop : Value {
    public ImageCrop(string alias, string src, string url, int height, int width) {
        Alias = alias;
        Src = src;
        Url = url;
        Height = height;
        Width = width;
    }

    public string GetMediaUrl(IUrlBuilder urlBuilder) {
        return urlBuilder.MediaUrl(Src);
    }

    public string Alias { get; }
    public string Src { get; }
    public string Url { get; }
    public int Height { get; }
    public int Width { get; }
}
