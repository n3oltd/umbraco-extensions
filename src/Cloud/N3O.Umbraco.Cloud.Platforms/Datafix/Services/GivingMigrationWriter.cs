using Microsoft.Extensions.Logging;
using N3O.Umbraco.Cloud.Platforms.Models;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public class GivingMigrationWriter : IGivingMigrationWriter {
    private const int PageSize = 200;

    private static readonly string[] ContentAliases = [
        GivingMigrationConstants.Properties.Description,
        GivingMigrationConstants.Properties.Icon,
        GivingMigrationConstants.Properties.Image,
        GivingMigrationConstants.Properties.Summary
    ];

    private static readonly string[] CampaignAliases = [
        GivingMigrationConstants.Properties.AnalyticsTags,
        GivingMigrationConstants.Properties.HeroImage,
        GivingMigrationConstants.Properties.Description,
        GivingMigrationConstants.Properties.Icon,
        GivingMigrationConstants.Properties.Image,
        GivingMigrationConstants.Properties.Summary
    ];

    private readonly IContentEditor _contentEditor;
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly IMediaService _mediaService;
    private readonly IDataTypeService _dataTypeService;
    private readonly ILogger<GivingMigrationWriter> _logger;

    public GivingMigrationWriter(IContentEditor contentEditor,
                                 IContentService contentService,
                                 IContentTypeService contentTypeService,
                                 IMediaService mediaService,
                                 IDataTypeService dataTypeService,
                                 ILogger<GivingMigrationWriter> logger) {
        _contentEditor = contentEditor;
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _mediaService = mediaService;
        _dataTypeService = dataTypeService;
        _logger = logger;
    }

    public Guid? GetCampaignsContainerId() {
        var contentType = _contentTypeService.Get(GivingMigrationConstants.Platforms.CampaignsAlias);

        if (contentType == null) {
            return null;
        }

        var containers = new List<IContent>();
        long page = 0;
        long total;

        do {
            var items = _contentService.GetPagedOfType(contentType.Id, page, PageSize, out total, null);

            containers.AddRange(items.Where(x => !x.Trashed));

            page++;
        } while (page * PageSize < total);

        return containers.Count == 1 ? containers[0].Key : null;
    }

    public GivingPlaceholders BuildPlaceholders(Guid mediaId, string analyticsTag) {
        var media = _mediaService.GetById(mediaId);

        if (media == null) {
            return null;
        }

        var src = GetMediaSrc(media);

        if (!src.HasValue()) {
            return null;
        }

        var segments = src.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length < 2) {
            return null;
        }

        var placeholders = new GivingPlaceholders();
        placeholders.MediaId = mediaId;
        placeholders.Src = src;
        placeholders.Filename = segments[segments.Length - 1];
        placeholders.MediaFileId = segments[segments.Length - 2];
        placeholders.Width = media.GetValue<int?>("umbracoWidth") ?? 0;
        placeholders.Height = media.GetValue<int?>("umbracoHeight") ?? 0;
        placeholders.AnalyticsTagsJson = BuildAnalyticsTagsJson(analyticsTag);

        return placeholders;
    }

    public GivingMigrationRunItemRes CreateCampaign(GivingMigrationCampaignRes plan,
                                                    Guid containerId,
                                                    GivingPlaceholders placeholders) {
        var item = NewItem(plan);
        var campaignId = Guid.NewGuid();

        try {
            var publisher = _contentEditor.New(plan.CampaignName,
                                               containerId,
                                               plan.CampaignContentTypeAlias,
                                               campaignId);

            var missing = CampaignAliases.Where(x => !publisher.HasProperty(x)).ToList();

            if (missing.Count > 0) {
                item.Outcome = GivingMigrationConstants.Outcomes.Failed;
                item.InvalidProperties = missing;
                item.Message = "The campaign content type is missing required properties: " +
                               string.Join(", ", missing);

                return item;
            }

            SetContentPlaceholders(publisher, plan.CampaignName, placeholders);

            publisher.Content
                     .Property<RawPropertyBuilder>(GivingMigrationConstants.Properties.AnalyticsTags)
                     .Set(placeholders.AnalyticsTagsJson);

            publisher.Content
                     .Property<RawPropertyBuilder>(GivingMigrationConstants.Properties.HeroImage)
                     .Set(BuildHeroImageJson(plan.CampaignContentTypeAlias, placeholders));

            var result = publisher.SaveAndPublish();

            if (!result.Success) {
                item.Outcome = GivingMigrationConstants.Outcomes.Failed;
                item.InvalidProperties = result.InvalidProperties
                                               .OrEmpty()
                                               .Select(x => x.PropertyType.Alias)
                                               .ToList();
                item.Message = "The campaign could not be published: " + DescribeResult(result);

                return item;
            }

            item.CampaignId = campaignId;
            item.OfferingsCreated = CreateOfferings(plan, campaignId, placeholders);
            item.Outcome = GivingMigrationConstants.Outcomes.Created;
        } catch (Exception ex) {
            _logger.LogError(ex,
                             "There was an error migrating legacy form with id {LegacyFormId}",
                             plan.LegacyFormId.ToString());

            item.Outcome = GivingMigrationConstants.Outcomes.Failed;
            item.Message = ex.Message;
        }

        return item;
    }

    public GivingMigrationRunItemRes PublishOfferings(GivingMigrationCampaignRes plan) {
        var item = NewItem(plan);
        item.CampaignId = plan.TargetCampaignId;

        if (plan.TargetCampaignId == null) {
            item.Outcome = GivingMigrationConstants.Outcomes.NotAttempted;
            item.Message = "The campaign has not been migrated";

            return item;
        }

        var campaign = _contentService.GetById(plan.TargetCampaignId.Value);

        if (campaign == null) {
            item.Outcome = GivingMigrationConstants.Outcomes.Failed;
            item.Message = "The migrated campaign could not be found";

            return item;
        }

        var published = 0;
        var failures = new List<string>();

        foreach (var child in GetChildren(campaign.Id)) {
            try {
                var result = _contentEditor.ForExisting(child.Key).SaveAndPublish();

                if (result.Success) {
                    published++;
                } else {
                    failures.Add(child.Name + ": " + DescribeResult(result));
                }
            } catch (Exception ex) {
                _logger.LogError(ex,
                                 "There was an error publishing offering with id {OfferingId}",
                                 child.Key.ToString());

                failures.Add(child.Name + ": " + ex.Message);
            }
        }

        item.OfferingsCreated = published;
        item.Outcome = failures.Count == 0
                           ? GivingMigrationConstants.Outcomes.Published
                           : GivingMigrationConstants.Outcomes.Failed;

        if (failures.Count > 0) {
            item.Message = string.Join("; ", failures);
        }

        return item;
    }

    private int CreateOfferings(GivingMigrationCampaignRes plan,
                                Guid campaignId,
                                GivingPlaceholders placeholders) {
        var created = 0;

        foreach (var offering in plan.Offerings) {
            if (!offering.OfferingContentTypeAlias.HasValue()) {
                continue;
            }

            var publisher = _contentEditor.New(offering.OfferingName,
                                               campaignId,
                                               offering.OfferingContentTypeAlias);

            SetContentPlaceholders(publisher, offering.OfferingName, placeholders);
            CopyDonationItem(offering.LegacyOptionId, publisher);

            publisher.SaveUnpublished();

            created++;
        }

        return created;
    }

    private void CopyDonationItem(Guid legacyOptionId, IContentPublisher publisher) {
        if (!publisher.HasProperty(GivingMigrationConstants.Properties.DonationItem)) {
            return;
        }

        var option = _contentService.GetById(legacyOptionId);
        var value = option?.GetValue<string>(GivingMigrationConstants.Properties.DonationItem);

        if (value.HasValue()) {
            publisher.Content
                     .Property<RawPropertyBuilder>(GivingMigrationConstants.Properties.DonationItem)
                     .Set(value);
        }
    }

    private void SetContentPlaceholders(IContentPublisher publisher,
                                        string name,
                                        GivingPlaceholders placeholders) {
        foreach (var alias in ContentAliases) {
            if (!publisher.HasProperty(alias)) {
                continue;
            }

            var isMedia = alias.EqualsInvariant(GivingMigrationConstants.Properties.Icon) ||
                          alias.EqualsInvariant(GivingMigrationConstants.Properties.Image);

            var value = isMedia ? BuildMediaPickerJson(placeholders.MediaId) : name;

            publisher.Content.Property<RawPropertyBuilder>(alias).Set(value);
        }
    }

    private string BuildHeroImageJson(string contentTypeAlias, GivingPlaceholders placeholders) {
        var crops = GetCropDefinitions(contentTypeAlias, GivingMigrationConstants.Properties.HeroImage)
                    .Select(x => AutoCrop(placeholders.Width, placeholders.Height, x.Item1, x.Item2))
                    .ToList();

        var source = new {
            src = placeholders.Src,
            mediaId = placeholders.MediaFileId,
            filename = placeholders.Filename,
            width = placeholders.Width,
            height = placeholders.Height,
            crops
        };

        return JsonConvert.SerializeObject(source);
    }

    private IReadOnlyList<Tuple<int, int>> GetCropDefinitions(string contentTypeAlias, string propertyAlias) {
        var contentType = _contentTypeService.Get(contentTypeAlias);

        var propertyType = contentType?.CompositionPropertyTypes
                                       .FirstOrDefault(x => x.Alias.EqualsInvariant(propertyAlias));

        if (propertyType == null) {
            return [];
        }

        var dataType = _dataTypeService.GetDataType(propertyType.DataTypeKey);

        if (dataType?.Configuration == null) {
            return [];
        }

        var configuration = JObject.FromObject(dataType.Configuration);

        if (configuration["cropDefinitions"] is not JArray definitions) {
            return [];
        }

        return definitions.Select(x => Tuple.Create(GetInt(x, "width"), GetInt(x, "height")))
                          .Where(x => x.Item1 > 0 && x.Item2 > 0)
                          .ToList();
    }

    private static int GetInt(JToken token, string name) {
        var value = token?[name];

        return value == null ? 0 : value.Value<int>();
    }

    private static object AutoCrop(int imageWidth, int imageHeight, int cropWidth, int cropHeight) {
        var aspectRatio = cropWidth / (decimal) cropHeight;

        var candidates = new List<Tuple<int, int, int, int>>();
        candidates.Add(Tuple.Create(0, 0, imageWidth, (int) (imageWidth / aspectRatio)));
        candidates.Add(Tuple.Create(0, 0, (int) (imageHeight * aspectRatio), imageHeight));
        candidates.Add(Tuple.Create((int) Math.Max(0, (imageWidth - cropWidth) / 2m),
                                    (int) Math.Max(0, (imageHeight - cropHeight) / 2m),
                                    Math.Min(cropWidth, imageWidth),
                                    Math.Min(cropHeight, imageHeight)));

        var crop = candidates.Where(x => x.Item1 + x.Item3 <= imageWidth && x.Item2 + x.Item4 <= imageHeight)
                             .OrderByDescending(x => (long) x.Item3 * x.Item4)
                             .FirstOrDefault();

        if (crop == null) {
            return new { x = 0, y = 0, width = imageWidth, height = imageHeight };
        }

        return new { x = crop.Item1, y = crop.Item2, width = crop.Item3, height = crop.Item4 };
    }

    private static string BuildMediaPickerJson(Guid mediaId) {
        var value = new[] { new { key = Guid.NewGuid(), mediaKey = mediaId } };

        return JsonConvert.SerializeObject(value);
    }

    private static string BuildAnalyticsTagsJson(string tag) {
        var value = new[] { new { icon = "icon-stop", name = tag, value = tag, description = "" } };

        return JsonConvert.SerializeObject(value);
    }

    private static string GetMediaSrc(IMedia media) {
        var value = media.GetValue<string>("umbracoFile");

        if (!value.HasValue()) {
            return null;
        }

        if (!value.TrimStart().StartsWith("{")) {
            return value;
        }

        var json = JObject.Parse(value);

        return json["src"]?.ToString();
    }

    private IReadOnlyList<IContent> GetChildren(int parentId) {
        var children = new List<IContent>();
        long page = 0;
        long total;

        do {
            children.AddRange(_contentService.GetPagedChildren(parentId, page, PageSize, out total)
                                             .Where(x => !x.Trashed));

            page++;
        } while (page * PageSize < total);

        return children;
    }

    private static string DescribeResult(PublishResult result) {
        var messages = result.EventMessages
                             ?.GetAll()
                             .Select(x => x.Message)
                             .ToList();

        return messages != null && messages.Count > 0 ? string.Join("; ", messages) : result.Result.ToString();
    }

    private static GivingMigrationRunItemRes NewItem(GivingMigrationCampaignRes plan) {
        var item = new GivingMigrationRunItemRes();
        item.LegacyFormId = plan.LegacyFormId;
        item.LegacyPath = plan.LegacyPath;
        item.CampaignName = plan.CampaignName;
        item.CampaignSlug = plan.CampaignSlug;
        item.OfferingsExpected = plan.ExpectedOfferings;

        return item;
    }
}
