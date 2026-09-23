using N3O.Umbraco.Extensions;
using N3O.Umbraco.ImageProcessing.Models;
using N3O.Umbraco.Plugins.Lookups;
using N3O.Umbraco.Utilities;
using SixLabors.ImageSharp;
using System;
using System.Collections.Concurrent;
using System.IO;
using Umbraco.Cms.Core.IO;

namespace N3O.Umbraco.ImageProcessing;

public class ImagePublisher : IImagePublisher {
    private static readonly ConcurrentDictionary<string, PublishedImage> Cache = new();
    
    private readonly MediaFileManager _mediaFileManager;
    private readonly IImageBuilder _imageBuilder;
    private readonly IUrlBuilder _urlBuilder;

    public ImagePublisher(MediaFileManager mediaFileManager, IImageBuilder imageBuilder, IUrlBuilder urlBuilder) {
        _mediaFileManager = mediaFileManager;
        _imageBuilder = imageBuilder;
        _urlBuilder = urlBuilder;
    }

    public PublishedImage Publish(Action<CacheKeyBuilder> cacheKeyBuilderAction,
                                  Func<IImageBuilder, IFluentImageBuilder> imageBuilderAction,
                                  ImageFormat format,
                                  bool forcePublish = false) {
        var cacheKeyBuilder = CacheKeyBuilder.Create();

        cacheKeyBuilderAction(cacheKeyBuilder);
        
        var cacheKey = cacheKeyBuilder.Build();

        return Publish(cacheKey, imageBuilderAction, format, forcePublish);
    }
    
    public PublishedImage Publish(string cacheKey,
                                  Func<IImageBuilder, IFluentImageBuilder> imageBuilderAction,
                                  ImageFormat format,
                                  bool forcePublish = false) {
        if (forcePublish) {
            return SaveAndPublish(cacheKey, imageBuilderAction, format);
        } else {
            return Cache.GetOrAddAtomic(GetMediaPath(cacheKey, format), () => {
                var url = TryFind(cacheKey, format) ?? SaveAndPublish(cacheKey, imageBuilderAction, format);

                return url;
            });
        }
    }
    
    private PublishedImage TryFind(string cacheKey, ImageFormat format) {
        var mediaPath = GetMediaPath(cacheKey, format);

        if (_mediaFileManager.FileSystem.FileExists(mediaPath)) {
            return GetPublishedImage(mediaPath);
        } else {
            return null;
        }
    }
    
    private PublishedImage SaveAndPublish(string cacheKey,
                                          Func<IImageBuilder, IFluentImageBuilder> imageBuilderAction,
                                          ImageFormat format) {
        var imageBuilder = imageBuilderAction(_imageBuilder);

        using (var stream = new MemoryStream()) {
            imageBuilder.Do(x => x.Save(stream, format.Encoder));
            
            stream.Rewind();

            return Publish(stream, cacheKey, format);
        };
    }
    
    private PublishedImage Publish(Stream stream, string cacheKey, ImageFormat format) {
        var mediaPath = GetMediaPath(cacheKey, format);

        _mediaFileManager.FileSystem.AddFile(mediaPath, stream);

        var publishedImage = GetPublishedImage(mediaPath);

        return publishedImage;
    }

    private string GetMediaPath(string cacheKey, ImageFormat format) {
        return $"/media/cdn/{cacheKey}{format.Extension}";
    }
    
    PublishedImage GetPublishedImage(string mediaPath) {
        using (var stream = _mediaFileManager.FileSystem.OpenFile(mediaPath)) {
            var url = _urlBuilder.Root().AppendPathSegments(mediaPath);
            
            var image = Image.Load(stream);

            return new PublishedImage(url, image.Size);
        }
    }
}