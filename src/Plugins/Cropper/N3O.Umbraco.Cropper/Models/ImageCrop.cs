using N3O.Umbraco.Attributes;

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

    public string Alias { get; }
    public string Src { get; }
    public string Url { get; }
    public int Height { get; }
    public int Width { get; }
}
