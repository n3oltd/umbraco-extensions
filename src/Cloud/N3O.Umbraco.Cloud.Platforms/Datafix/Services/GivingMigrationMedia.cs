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
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
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
                                                       bool allowPlaceholder,
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
                                                  allowPlaceholder,
                                                  cancellationToken);

        placeholders.Image = await ResolveOneAsync(imageMediaId,
                                                  GivingMigrationConstants.Properties.Image,
                                                  GivingMigrationConstants.Placeholders.ImageUrl,
                                                  GivingMigrationConstants.Placeholders.ImageFilename,
                                                  UmbracoConventions.MediaTypes.Image,
                                                  PlatformsSchemaConstants.SharedDataTypes.ImageMediaPicker,
                                                  cache,
                                                  allowPlaceholder,
                                                  cancellationToken);

        // The hero image is the same kind of asset as the image, so it reuses it unless the caller supplies its own.
        if (heroImageMediaId.HasValue) {
            var heroImage = Describe(heroImageMediaId.Value, out var heroImageProblem);

            if (heroImage == null) {
                throw new InvalidOperationException(heroImageProblem);
            }

            placeholders.HeroImage = heroImage;
        } else {
            placeholders.HeroImage = placeholders.Image;
        }

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
                                                               bool allowPlaceholder,
                                                               CancellationToken cancellationToken) {
        if (suppliedId.HasValue) {
            var supplied = Describe(suppliedId.Value, out var problem);

            if (supplied == null) {
                throw new InvalidOperationException(problem);
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

        if (!allowPlaceholder) {
            throw new InvalidOperationException("No " +
                                                kind +
                                                " media was supplied. Pass one from the site's own library, or set " +
                                                "allowPlaceholderMedia to download a placeholder from a third party");
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

        byte[] bytes;

        using (var response = await client.GetAsync(url, cancellationToken)) {
            response.EnsureSuccessStatusCode();

            bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }

        using (var stream = new MemoryStream(bytes)) {
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
    }

    private GivingPlaceholderMedia Describe(Guid mediaId) {
        return Describe(mediaId, out _);
    }

    private GivingPlaceholderMedia Describe(Guid mediaId, out string problem) {
        problem = null;

        var media = _mediaService.GetById(mediaId);

        if (media == null) {
            problem = "No media item exists with id " + mediaId;

            return null;
        }

        var src = GetSrc(media);

        if (!src.HasValue()) {
            problem = "Media item " + mediaId + " has no " + UmbracoFile + " value";

            return null;
        }

        var segments = src.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // The file id is the folder the upload was placed in, so a src that is not in that shape would yield a
        // plausible but wrong id rather than an obvious failure.
        if (segments.Length < 2) {
            problem = "Media item " + mediaId + " has an unexpected file path (" + src + "), so its file id cannot " +
                      "be determined";

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

        if (!startNodeId.HasValue()) {
            return UmbracoSystem.Root;
        }

        // Falling back to the root here would place the media outside the start node the picker restricts browsing
        // to, which only shows up later as a validation failure on an editor's screen.
        if (!UdiParser.TryParse(startNodeId, out GuidUdi udi)) {
            throw new InvalidOperationException("The start node of " + pickerDataTypeName + " is not a valid udi (" +
                                                startNodeId + ")");
        }

        var startNode = _mediaService.GetById(udi.Guid);

        if (startNode == null) {
            throw new InvalidOperationException("The start node of " + pickerDataTypeName + " does not exist");
        }

        return startNode.Id;
    }
}
