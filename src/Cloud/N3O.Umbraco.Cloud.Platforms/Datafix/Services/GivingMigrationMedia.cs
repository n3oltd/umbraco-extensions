using Microsoft.Extensions.Logging;
using N3O.Umbraco.DataTypes;
using N3O.Umbraco.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Core;
using Umbraco.Extensions;
using UmbracoConventions = Umbraco.Cms.Core.Constants.Conventions;
using UmbracoSystem = Umbraco.Cms.Core.Constants.System;

namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public class GivingMigrationMedia : IGivingMigrationMedia {
    private const string UmbracoFile = "umbracoFile";
    private const string UmbracoWidth = "umbracoWidth";
    private const string UmbracoHeight = "umbracoHeight";

    private readonly IMediaService _mediaService;
    private readonly MediaFileManager _mediaFileManager;
    private readonly MediaUrlGeneratorCollection _mediaUrlGenerators;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly IContentTypeBaseServiceProvider _contentTypeBaseServiceProvider;
    private readonly IDataTypeEditor _dataTypeEditor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IGivingMigrationStore _store;
    private readonly ILogger<GivingMigrationMedia> _logger;

    public GivingMigrationMedia(IMediaService mediaService,
                                MediaFileManager mediaFileManager,
                                MediaUrlGeneratorCollection mediaUrlGenerators,
                                IShortStringHelper shortStringHelper,
                                IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
                                IDataTypeEditor dataTypeEditor,
                                IHttpClientFactory httpClientFactory,
                                IGivingMigrationStore store,
                                ILogger<GivingMigrationMedia> logger) {
        _mediaService = mediaService;
        _mediaFileManager = mediaFileManager;
        _mediaUrlGenerators = mediaUrlGenerators;
        _shortStringHelper = shortStringHelper;
        _contentTypeBaseServiceProvider = contentTypeBaseServiceProvider;
        _dataTypeEditor = dataTypeEditor;
        _httpClientFactory = httpClientFactory;
        _store = store;
        _logger = logger;
    }

    public async Task<GivingPlaceholders> ResolveAsync(Guid? iconMediaId,
                                                       Guid? imageMediaId,
                                                       Guid? heroImageMediaId,
                                                       CancellationToken cancellationToken) {
        var cache = new Dictionary<string, Guid>(_store.GetPlaceholderMedia(), StringComparer.OrdinalIgnoreCase);

        var placeholders = new GivingPlaceholders();

        placeholders.Icon = await ResolveOneAsync(iconMediaId,
                                                  GivingMigrationConstants.Properties.Icon,
                                                  GivingMigrationConstants.Placeholders.IconUrl,
                                                  GivingMigrationConstants.Placeholders.IconFilename,
                                                  UmbracoConventions.MediaTypes.VectorGraphicsAlias,
                                                  PlatformsSchemaConstants.SharedDataTypes.IconMediaPicker,
                                                  cache,
                                                  cancellationToken);

        placeholders.Image = await ResolveOneAsync(imageMediaId,
                                                  GivingMigrationConstants.Properties.Image,
                                                  GivingMigrationConstants.Placeholders.ImageUrl,
                                                  GivingMigrationConstants.Placeholders.ImageFilename,
                                                  UmbracoConventions.MediaTypes.Image,
                                                  PlatformsSchemaConstants.SharedDataTypes.ImageMediaPicker,
                                                  cache,
                                                  cancellationToken);

        // The hero image is the same kind of asset as the image, so it reuses it unless the caller supplies its own.
        placeholders.HeroImage = heroImageMediaId.HasValue
                                     ? Describe(heroImageMediaId.Value)
                                     : placeholders.Image;

        _store.SavePlaceholderMedia(cache);

        return placeholders;
    }

    private async Task<GivingPlaceholderMedia> ResolveOneAsync(Guid? suppliedId,
                                                               string kind,
                                                               string url,
                                                               string filename,
                                                               string mediaTypeAlias,
                                                               string pickerDataTypeName,
                                                               IDictionary<string, Guid> cache,
                                                               CancellationToken cancellationToken) {
        if (suppliedId.HasValue) {
            var supplied = Describe(suppliedId.Value);

            if (supplied == null) {
                throw new InvalidOperationException("No media item exists with id " + suppliedId.Value);
            }

            return supplied;
        }

        if (cache.TryGetValue(kind, out var cachedId)) {
            var cached = Describe(cachedId);

            if (cached != null) {
                return cached;
            }

            cache.Remove(kind);
        }

        var created = await DownloadAsync(kind, url, filename, mediaTypeAlias, pickerDataTypeName, cancellationToken);

        cache[kind] = created.Id;

        return created;
    }

    private async Task<GivingPlaceholderMedia> DownloadAsync(string kind,
                                                             string url,
                                                             string filename,
                                                             string mediaTypeAlias,
                                                             string pickerDataTypeName,
                                                             CancellationToken cancellationToken) {
        _logger.LogInformation("Downloading {Kind} placeholder from {Url}", kind, url);

        // Both placeholder services redirect to a CDN, so the handler must follow redirects (the default).
        var client = _httpClientFactory.CreateClient();

        using var response = await client.GetAsync(url, cancellationToken);

        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        using var stream = new MemoryStream(bytes);

        var media = _mediaService.CreateMedia(GivingMigrationConstants.Placeholders.MediaFolderName + " " + kind,
                                              GetPickerStartNodeId(pickerDataTypeName),
                                              mediaTypeAlias);

        media.SetValue(_mediaFileManager,
                       _mediaUrlGenerators,
                       _shortStringHelper,
                       _contentTypeBaseServiceProvider,
                       UmbracoFile,
                       filename,
                       stream);

        _mediaService.Save(media);

        var descriptor = Describe(media.Key);

        if (descriptor == null) {
            throw new InvalidOperationException("The " + kind + " placeholder media could not be read back");
        }

        descriptor.Created = true;

        return descriptor;
    }

    private GivingPlaceholderMedia Describe(Guid mediaId) {
        var media = _mediaService.GetById(mediaId);

        if (media == null) {
            return null;
        }

        var src = GetSrc(media);

        if (!src.HasValue()) {
            return null;
        }

        var segments = src.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length < 2) {
            return null;
        }

        var descriptor = new GivingPlaceholderMedia();
        descriptor.Id = mediaId;
        descriptor.Src = src;
        descriptor.Filename = segments[segments.Length - 1];
        descriptor.MediaFileId = segments[segments.Length - 2];
        descriptor.Width = media.GetValue<int?>(UmbracoWidth) ?? 0;
        descriptor.Height = media.GetValue<int?>(UmbracoHeight) ?? 0;

        return descriptor;
    }

    private static string GetSrc(IMedia media) {
        var value = media.GetValue<string>(UmbracoFile);

        if (!value.HasValue()) {
            return null;
        }

        if (!value.TrimStart().StartsWith("{")) {
            return value;
        }

        return JObject.Parse(value)["src"]?.ToString();
    }

    // Both pickers restrict browsing to a start node, so placeholder media created outside it fails validation.
    private int GetPickerStartNodeId(string pickerDataTypeName) {
        var dataType = _dataTypeEditor.Find(pickerDataTypeName);

        if (dataType?.Configuration == null) {
            return UmbracoSystem.Root;
        }

        // The configuration is a strongly typed object, so the serialized names are the CLR ones
        var json = JObject.FromObject(dataType.Configuration);
        var startNodeId = json.Property("startNodeId", StringComparison.OrdinalIgnoreCase)?.Value?.ToString();

        if (!startNodeId.HasValue() || !UdiParser.TryParse(startNodeId, out GuidUdi udi)) {
            return UmbracoSystem.Root;
        }

        return _mediaService.GetById(udi.Guid)?.Id ?? UmbracoSystem.Root;
    }
}
