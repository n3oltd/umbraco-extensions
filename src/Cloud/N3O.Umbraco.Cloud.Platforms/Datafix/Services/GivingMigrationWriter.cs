using Microsoft.Extensions.Logging;
using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Cloud.Platforms.Lookups;
using N3O.Umbraco.Cloud.Platforms.Models;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Giving.Allocations.Lookups;
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
    private const string NestedContentTypeAlias = "ncContentTypeAlias";
    private const string NestedKey = "key";
    private const string NestedName = "name";

    // Copied straight across: the legacy and platforms properties use the same editor over the same lookup ids.
    private static readonly string[] VerbatimStateAliases = [
        GivingMigrationConstants.Properties.DonationItem,
        GivingMigrationConstants.Properties.Scheme,
        GivingMigrationConstants.Properties.Dimension1,
        GivingMigrationConstants.Properties.Dimension2,
        GivingMigrationConstants.Properties.Dimension3
    ];

    private static readonly string[] CampaignAliases = [
        GivingMigrationConstants.Properties.AnalyticsTags,
        GivingMigrationConstants.Properties.HeroImage
    ];

    private readonly IContentEditor _contentEditor;
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly IDataTypeService _dataTypeService;
    private readonly ILogger<GivingMigrationWriter> _logger;

    public GivingMigrationWriter(IContentEditor contentEditor,
                                 IContentService contentService,
                                 IContentTypeService contentTypeService,
                                 IDataTypeService dataTypeService,
                                 ILogger<GivingMigrationWriter> logger) {
        _contentEditor = contentEditor;
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _dataTypeService = dataTypeService;
        _logger = logger;
    }

    public Guid? GetCampaignsContainerId() {
        var containers = GetAllOfAlias(GivingMigrationConstants.Platforms.CampaignsAlias);

        return containers.Count == 1 ? containers[0].Key : null;
    }

    public Guid? EnsureCrossSellsContainerId(out string problem) {
        problem = null;

        var containers = GetAllOfAlias(PlatformsConstants.CrossSells.ContainerAlias);

        if (containers.Count == 1) {
            return containers[0].Key;
        }

        if (containers.Count > 1) {
            problem = "There is more than one cross sells container, so the migration cannot tell which to use";

            return null;
        }

        if (_contentTypeService.Get(PlatformsConstants.CrossSells.ContainerAlias) == null) {
            problem = "The " + PlatformsConstants.CrossSells.ContainerAlias + " content type does not exist on this " +
                      "site, so the container cannot be created";

            return null;
        }

        var roots = GetAllOfAlias(PlatformsConstants.Platforms.Alias);

        if (roots.Count != 1) {
            problem = roots.Count == 0
                          ? "There is no platforms root to create the cross sells container under"
                          : "There is more than one platforms root, so the migration cannot tell which to use";

            return null;
        }

        var publisher = _contentEditor.New("Cross Sells",
                                           roots[0].Key,
                                           PlatformsConstants.CrossSells.ContainerAlias);

        var result = publisher.SaveAndPublish();

        if (!result.Success) {
            problem = "The cross sells container could not be published: " + DescribeResult(result);

            _logger.LogError("Could not create the cross sells container: {Reason}", DescribeResult(result));

            return null;
        }

        var created = GetAllOfAlias(PlatformsConstants.CrossSells.ContainerAlias);

        if (created.Count != 1) {
            problem = "The cross sells container was published but could not be read back";

            return null;
        }

        return created[0].Key;
    }

    public GivingMigrationRunItemRes CreateCampaign(GivingMigrationCampaignRes plan,
                                                    Guid containerId,
                                                    GivingPlaceholders placeholders,
                                                    ICollection<GivingMigrationLedgerEntry> ledger) {
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

            SetContentPlaceholders(publisher, plan.CampaignName, placeholders, null);

            Set(publisher,
                GivingMigrationConstants.Properties.AnalyticsTags,
                BuildAnalyticsTagsJson(plan.CampaignName));
            Set(publisher,
                GivingMigrationConstants.Properties.HeroImage,
                BuildHeroImageJson(plan.CampaignContentTypeAlias, placeholders.HeroImage));

            var result = publisher.SaveAndPublish();

            if (!result.Success) {
                item.Outcome = GivingMigrationConstants.Outcomes.Failed;
                item.InvalidProperties = InvalidProperties(result);
                item.Message = "The campaign could not be published: " + DescribeResult(result);

                return item;
            }

            ledger.Add(Entry(plan.LegacyFormId,
                             campaignId,
                             GivingMigrationConstants.LedgerKinds.Campaign,
                             plan.CampaignName));

            item.CampaignId = campaignId;
            item.OfferingsCreated = CreateOfferings(plan, campaignId, plan.Offerings, placeholders, item, ledger);

            // The campaign is committed by this point and the planner will call it already migrated from now on, so
            // anything short of every offering is a failure.
            item.Outcome = item.OfferingsCreated == plan.Offerings.Count()
                               ? GivingMigrationConstants.Outcomes.Created
                               : GivingMigrationConstants.Outcomes.Failed;
        } catch (Exception ex) {
            _logger.LogError(ex,
                             "There was an error migrating legacy form with id {LegacyFormId}",
                             plan.LegacyFormId.ToString());

            item.Outcome = GivingMigrationConstants.Outcomes.Failed;
            item.Message = Describe(ex);
        }

        return item;
    }

    public GivingMigrationRunItemRes CreateCrossSell(GivingMigrationCrossSellRes plan,
                                                     Guid containerId,
                                                     GivingPlaceholders placeholders,
                                                     ICollection<GivingMigrationLedgerEntry> ledger) {
        var item = new GivingMigrationRunItemRes();
        item.LegacyFormId = plan.LegacyUpsellId;
        item.LegacyPath = plan.LegacyPath;
        item.CampaignName = plan.CrossSellName;

        var crossSellId = Guid.NewGuid();

        try {
            var upsell = _contentService.GetById(plan.LegacyUpsellId);

            if (upsell == null) {
                item.Outcome = GivingMigrationConstants.Outcomes.Failed;
                item.Message = "The legacy upsell offer could not be found";

                return item;
            }

            var publisher = _contentEditor.New(plan.CrossSellName,
                                               containerId,
                                               plan.CrossSellContentTypeAlias,
                                               crossSellId);

            // The legacy upsell carries its own description, so it is not overwritten with the placeholder.
            var description = upsell.GetValue<string>(GivingMigrationConstants.Properties.Description);

            SetContentPlaceholders(publisher, plan.CrossSellName, placeholders, description);

            var problems = new List<string>();

            CopyVerbatimState(upsell, publisher);
            CopyGiftType(upsell, publisher, GivingMigrationConstants.Properties.GivingType, problems);

            CopySuggestedAmounts(upsell,
                                 publisher,
                                 GivingMigrationConstants.Properties.PriceHandles,
                                 GivingMigrationConstants.Properties.OneTimeSuggestedAmounts,
                                 plan.CrossSellContentTypeAlias,
                                 problems);

            Set(publisher,
                GivingMigrationConstants.Properties.Stage,
                BuildDataListJson(GivingMigrationConstants.Stages.Cart));

            var fixedAmount = upsell.GetValue<decimal?>(GivingMigrationConstants.Properties.FixedAmount);

            if (fixedAmount.HasValue) {
                Set(publisher, GivingMigrationConstants.Properties.Amount, fixedAmount.Value);
            }

            var result = publisher.SaveAndPublish();

            if (!result.Success) {
                item.Outcome = GivingMigrationConstants.Outcomes.Failed;
                item.InvalidProperties = InvalidProperties(result);
                item.Message = "The cross sell could not be published: " + DescribeResult(result);

                return item;
            }

            ledger.Add(Entry(plan.LegacyUpsellId,
                             crossSellId,
                             GivingMigrationConstants.LedgerKinds.CrossSell,
                             plan.CrossSellName));

            item.CampaignId = crossSellId;
            item.Outcome = GivingMigrationConstants.Outcomes.Created;
        } catch (Exception ex) {
            _logger.LogError(ex,
                             "There was an error migrating legacy upsell offer with id {LegacyUpsellId}",
                             plan.LegacyUpsellId.ToString());

            item.Outcome = GivingMigrationConstants.Outcomes.Failed;
            item.Message = Describe(ex);
        }

        return item;
    }

    // A campaign whose offerings only partly succeeded is still in the ledger and is reported as already migrated,
    // so without this the offerings it is missing could not be created by any endpoint.
    public GivingMigrationRunItemRes CreateMissingOfferings(GivingMigrationCampaignRes plan,
                                                            IReadOnlyCollection<Guid> migratedOptionIds,
                                                            GivingPlaceholders placeholders,
                                                            ICollection<GivingMigrationLedgerEntry> ledger) {
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

        // The ledger can be behind the content if a run was interrupted between the save and the append, so what is
        // already under the campaign is checked too rather than creating a second copy of it.
        var existingNames = GivingMigrationContent.GetChildren(_contentService, campaign.Id)
                                                  .Select(x => x.Name)
                                                  .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = plan.Offerings
                          .Where(x => !migratedOptionIds.Contains(x.LegacyOptionId))
                          .Where(x => !existingNames.Contains(x.OfferingName))
                          .ToList();

        if (missing.Count == 0) {
            item.Outcome = GivingMigrationConstants.Outcomes.NotAttempted;

            return item;
        }

        try {
            item.OfferingsCreated = CreateOfferings(plan,
                                                    plan.TargetCampaignId.Value,
                                                    missing,
                                                    placeholders,
                                                    item,
                                                    ledger);

            item.Outcome = item.OfferingsCreated == missing.Count
                               ? GivingMigrationConstants.Outcomes.Created
                               : GivingMigrationConstants.Outcomes.Failed;
        } catch (Exception ex) {
            _logger.LogError(ex,
                             "There was an error creating missing offerings for legacy form with id {LegacyFormId}",
                             plan.LegacyFormId.ToString());

            item.Outcome = GivingMigrationConstants.Outcomes.Failed;
            item.Message = Describe(ex);
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
        var invalidProperties = new List<string>();

        // Only the offerings are published, the same set the reporter counts. A campaign can hold other children and
        // this step has no mandate to push those live.
        var offeringTypeIds = GivingMigrationContent.GetOfferingContentTypeIds(_contentTypeService);

        foreach (var child in GivingMigrationContent.GetChildren(_contentService, campaign.Id)
                                                    .Where(x => offeringTypeIds.Contains(x.ContentTypeId))) {
            try {
                var result = _contentEditor.ForExisting(child.Key).SaveAndPublish();

                if (result.Success) {
                    published++;
                } else {
                    invalidProperties.AddRange(InvalidProperties(result).Select(x => child.Name + "." + x));

                    failures.Add(child.Name + ": " + DescribeResult(result));
                }
            } catch (Exception ex) {
                _logger.LogError(ex,
                                 "There was an error publishing offering with id {OfferingId}",
                                 child.Key.ToString());

                failures.Add(child.Name + ": " + Describe(ex));
            }
        }

        item.OfferingsCreated = published;
        item.InvalidProperties = invalidProperties;
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
                                IEnumerable<GivingMigrationOfferingRes> offerings,
                                GivingPlaceholders placeholders,
                                GivingMigrationRunItemRes item,
                                ICollection<GivingMigrationLedgerEntry> ledger) {
        var created = 0;
        var failures = new List<string>();

        foreach (var offering in offerings) {
            if (!offering.OfferingContentTypeAlias.HasValue()) {
                failures.Add(offering.OfferingName + ": no platforms offering type is mapped for " +
                             offering.LegacyOptionAlias);

                continue;
            }

            var offeringId = Guid.NewGuid();

            var publisher = _contentEditor.New(offering.OfferingName,
                                               campaignId,
                                               offering.OfferingContentTypeAlias,
                                               offeringId);

            SetContentPlaceholders(publisher, offering.OfferingName, placeholders, null);
            CopyOfferingState(offering, publisher, failures);

            var result = publisher.SaveUnpublished();

            if (result.Success) {
                created++;

                ledger.Add(Entry(offering.LegacyOptionId,
                                 offeringId,
                                 GivingMigrationConstants.LedgerKinds.Offering,
                                 offering.OfferingName));
            } else {
                failures.Add(offering.OfferingName + ": " + DescribeResult(result));
            }
        }

        if (failures.Count > 0) {
            item.Message = string.Join("; ", failures);
        }

        return created;
    }

    // Each offering type carries its own mandatory state property, so an offering whose state is not copied saves
    // but can never be published.
    private void CopyOfferingState(GivingMigrationOfferingRes offering,
                                   IContentPublisher publisher,
                                   ICollection<string> problems) {
        var option = _contentService.GetById(offering.LegacyOptionId);

        if (option == null) {
            problems.Add(offering.OfferingName + ": the legacy donation option no longer exists so its state could " +
                         "not be copied");

            return;
        }

        CopyVerbatimState(option, publisher);
        CopyGiftType(option, publisher, GivingMigrationConstants.Properties.DefaultGivingType, problems);

        CopySuggestedAmounts(option,
                             publisher,
                             GivingMigrationConstants.Properties.DonationPriceHandles,
                             GivingMigrationConstants.Properties.OneTimeSuggestedAmounts,
                             offering.OfferingContentTypeAlias,
                             problems);

        CopySuggestedAmounts(option,
                             publisher,
                             GivingMigrationConstants.Properties.RegularGivingPriceHandles,
                             GivingMigrationConstants.Properties.RecurringSuggestedAmounts,
                             offering.OfferingContentTypeAlias,
                             problems);
    }

    private void CopyVerbatimState(IContent source, IContentPublisher publisher) {
        foreach (var alias in VerbatimStateAliases) {
            if (!publisher.HasProperty(alias)) {
                continue;
            }

            var value = source.GetValue<string>(alias);

            if (value.HasValue()) {
                Set(publisher, alias, value);
            }
        }
    }

    // Both sides are data lists but over different lookups, so the ids have to be translated rather than copied.
    private void CopyGiftType(IContent source,
                              IContentPublisher publisher,
                              string sourceAlias,
                              ICollection<string> problems) {
        var target = GivingMigrationConstants.Properties.SuggestedGiftType;

        if (!publisher.HasProperty(target)) {
            return;
        }

        var value = source.GetValue<string>(sourceAlias);

        if (!value.HasValue()) {
            return;
        }

        var parsed = ParseDataList(value);

        if (parsed.None()) {
            problems.Add(target + ": the legacy " + sourceAlias + " value could not be read so no gift type was set");

            return;
        }

        var giftTypes = parsed.Select(MapGiftType).Where(x => x != null).Distinct().ToList();

        if (giftTypes.Count == 0) {
            problems.Add(target + ": none of the legacy giving types in " + sourceAlias + " map to a platforms gift " +
                         "type, so the offering cannot be published");

            return;
        }

        Set(publisher, target, JsonConvert.SerializeObject(giftTypes));
    }

    // Publishing maps the allocation for the outbound webhook, and the framework dereferences the donation item
    // or scheme lookup without a null check. An unresolved lookup therefore surfaces as a bare
    // NullReferenceException, which says nothing about the actual cause.
    private static string Describe(Exception ex) {
        foreach (var inner in Flatten(ex)) {
            if (inner is NullReferenceException &&
                inner.StackTrace?.Contains(nameof(DonationFormStateContent.GetFundDimensionOptions),
                                           StringComparison.Ordinal) == true) {
                return "The allocation could not be mapped because the donation item or scheme lookup returned " +
                       "nothing. Those lookups are served by the cloud, so the usual cause is the site not reaching " +
                       "it with a valid subscription. Original error: " + inner.Message;
            }
        }

        return ex.Message;
    }

    private static IEnumerable<Exception> Flatten(Exception ex) {
        while (ex != null) {
            if (ex is AggregateException aggregate) {
                foreach (var inner in aggregate.Flatten().InnerExceptions.SelectMany(Flatten)) {
                    yield return inner;
                }

                yield break;
            }

            yield return ex;

            ex = ex.InnerException;
        }
    }

    private static string BuildAnalyticsTagsJson(string campaignName) {
        var tag = new {
            icon = "icon-stop",
            name = GivingMigrationConstants.Placeholders.AnalyticsTagName,
            value = campaignName,
            description = ""
        };

        return JsonConvert.SerializeObject(new[] { tag });
    }

    private static string MapGiftType(string givingTypeId) {
        if (givingTypeId.EqualsInvariant(GivingTypes.Donation.Id)) {
            return GiftTypes.OneTime.Id;
        }

        if (givingTypeId.EqualsInvariant(GivingTypes.RegularGiving.Id)) {
            return GiftTypes.Recurring.Id;
        }

        return null;
    }

    private void CopySuggestedAmounts(IContent source,
                                      IContentPublisher publisher,
                                      string sourceAlias,
                                      string targetAlias,
                                      string targetContentTypeAlias,
                                      ICollection<string> problems) {
        if (!publisher.HasProperty(targetAlias)) {
            return;
        }

        var value = source.GetValue<string>(sourceAlias);

        if (!value.HasValue()) {
            return;
        }

        var elementAlias = ResolveNestedElementAlias(targetContentTypeAlias,
                                                     targetAlias,
                                                     PlatformsConstants.DonationFormState.SuggestedAmount);

        if (!elementAlias.HasValue()) {
            _logger.LogWarning("Could not resolve the nested element alias for {ContentType}.{Property}",
                               targetContentTypeAlias,
                               targetAlias);

            problems.Add(targetAlias + ": the nested element type behind " + targetContentTypeAlias +
                         " could not be resolved so the suggested amounts were dropped");

            return;
        }

        var rewritten = RewriteNestedContent(value, elementAlias);

        if (!rewritten.HasValue()) {
            problems.Add(targetAlias + ": the legacy " + sourceAlias + " value is not valid JSON so the suggested " +
                         "amounts were dropped");

            return;
        }

        Set(publisher, targetAlias, rewritten);
    }

    private static string RewriteNestedContent(string json, string elementAlias) {
        JArray items;

        try {
            items = JArray.Parse(json);
        } catch (JsonException) {
            return null;
        }

        foreach (var item in items.OfType<JObject>()) {
            item[NestedContentTypeAlias] = elementAlias;
            item[NestedKey] = Guid.NewGuid().ToString();
            item.Remove(NestedName);
        }

        return items.ToString(Formatting.None);
    }

    // The element alias is read from the target data type rather than assumed, because a site seeded before the
    // element was renamed still uses the old alias and the seeder never updates an existing type.
    // A nested content data type may legitimately allow more than one element type, so the one being written is
    // asserted to be among them rather than whichever happens to be listed first.
    private string ResolveNestedElementAlias(string contentTypeAlias, string propertyAlias, string expectedAlias) {
        var configuration = GetDataTypeConfiguration(contentTypeAlias, propertyAlias);

        if (Find(configuration, "contentTypes") is not JArray contentTypes) {
            return null;
        }

        return contentTypes.OfType<JObject>()
                           .Select(x => Find(x, "ncAlias")?.ToString() ?? Find(x, "alias")?.ToString())
                           .FirstOrDefault(x => x.EqualsInvariant(expectedAlias));
    }

    // The data type configuration is a strongly typed object, so JObject.FromObject emits the CLR property names
    // rather than the camel cased ones stored in the database. Lookups therefore have to ignore case.
    private static JToken Find(JObject json, string name) {
        return json?.Property(name, StringComparison.OrdinalIgnoreCase)?.Value;
    }

    private JObject GetDataTypeConfiguration(string contentTypeAlias, string propertyAlias) {
        var contentType = _contentTypeService.Get(contentTypeAlias);

        var propertyType = contentType?.CompositionPropertyTypes
                                       .FirstOrDefault(x => x.Alias.EqualsInvariant(propertyAlias));

        if (propertyType == null) {
            return null;
        }

        var dataType = _dataTypeService.GetDataType(propertyType.DataTypeKey);

        return dataType?.Configuration == null ? null : JObject.FromObject(dataType.Configuration);
    }

    private void SetContentPlaceholders(IContentPublisher publisher,
                                        string name,
                                        GivingPlaceholders placeholders,
                                        string description) {
        Set(publisher,
            GivingMigrationConstants.Properties.Description,
            description.HasValue() ? description : name);

        Set(publisher, GivingMigrationConstants.Properties.Summary, name);
        Set(publisher, GivingMigrationConstants.Properties.Icon, BuildMediaPickerJson(placeholders.Icon.Id));
        Set(publisher, GivingMigrationConstants.Properties.Image, BuildMediaPickerJson(placeholders.Image.Id));
    }

    private static void Set(IContentPublisher publisher, string alias, object value) {
        if (!publisher.HasProperty(alias)) {
            return;
        }

        publisher.Content.Property<RawPropertyBuilder>(alias).Set(value);
    }

    private string BuildHeroImageJson(string contentTypeAlias, GivingPlaceholderMedia media) {
        var crops = GetCropDefinitions(contentTypeAlias, GivingMigrationConstants.Properties.HeroImage)
                    .Select(x => AutoCrop(media.Width, media.Height, x.Width, x.Height))
                    .ToList();

        var source = new {
            src = media.Src,
            mediaId = media.MediaFileId,
            filename = media.Filename,
            width = media.Width,
            height = media.Height,
            crops
        };

        return JsonConvert.SerializeObject(source);
    }

    // Each stored crop is matched to its definition by position when the value is read, so one crop is emitted per
    // definition in the configured order, including any whose dimensions are not set.
    private IReadOnlyList<(int Width, int Height)> GetCropDefinitions(string contentTypeAlias, string propertyAlias) {
        var configuration = GetDataTypeConfiguration(contentTypeAlias, propertyAlias);

        if (Find(configuration, "cropDefinitions") is not JArray definitions) {
            return [];
        }

        return definitions.Select(x => (Width: GetInt(x, "width"), Height: GetInt(x, "height"))).ToList();
    }

    private static IReadOnlyList<string> ParseDataList(string json) {
        try {
            return JArray.Parse(json).Select(x => x.ToString()).Where(x => x.HasValue()).ToList();
        } catch (JsonException) {
            return [];
        }
    }

    private static string BuildDataListJson(string value) {
        return JsonConvert.SerializeObject(new[] { value });
    }

    private static int GetInt(JToken token, string name) {
        var value = Find(token as JObject, name);

        return value == null ? 0 : value.Value<int>();
    }

    private static object AutoCrop(int imageWidth, int imageHeight, int cropWidth, int cropHeight) {
        var whole = new { x = 0, y = 0, width = imageWidth, height = imageHeight };

        // A definition with no dimensions still needs its position in the array, and the whole image is the only
        // crop that can be derived without them.
        if (cropWidth <= 0 || cropHeight <= 0) {
            return whole;
        }

        var aspectRatio = cropWidth / (decimal) cropHeight;

        var candidates = new List<(int X, int Y, int Width, int Height)>();
        candidates.Add((0, 0, imageWidth, (int) (imageWidth / aspectRatio)));
        candidates.Add((0, 0, (int) (imageHeight * aspectRatio), imageHeight));
        candidates.Add(((int) Math.Max(0, (imageWidth - cropWidth) / 2m),
                        (int) Math.Max(0, (imageHeight - cropHeight) / 2m),
                        Math.Min(cropWidth, imageWidth),
                        Math.Min(cropHeight, imageHeight)));

        var crops = candidates.Where(x => x.X + x.Width <= imageWidth && x.Y + x.Height <= imageHeight)
                              .OrderByDescending(x => (long) x.Width * x.Height)
                              .ToList();

        if (crops.Count == 0) {
            return whole;
        }

        return new { x = crops[0].X, y = crops[0].Y, width = crops[0].Width, height = crops[0].Height };
    }

    private static string BuildMediaPickerJson(Guid mediaId) {
        var value = new[] { new { key = Guid.NewGuid(), mediaKey = mediaId } };

        return JsonConvert.SerializeObject(value);
    }

    private IReadOnlyList<IContent> GetAllOfAlias(string contentTypeAlias) {
        return GivingMigrationContent.GetAllOfAlias(_contentService, _contentTypeService, contentTypeAlias)
                                     .Where(x => !x.Trashed)
                                     .ToList();
    }

    private static IReadOnlyList<string> InvalidProperties(PublishResult result) {
        return result.InvalidProperties.OrEmpty().Select(x => x.PropertyType.Alias).ToList();
    }

    private static string DescribeResult(PublishResult result) {
        var messages = result.EventMessages?.GetAll().Select(x => x.Message).ToList();

        return messages != null && messages.Count > 0 ? string.Join("; ", messages) : result.Result.ToString();
    }

    private static string DescribeResult(OperationResult result) {
        var messages = result.EventMessages?.GetAll().Select(x => x.Message).ToList();

        return messages != null && messages.Count > 0 ? string.Join("; ", messages) : result.Result.ToString();
    }

    private static GivingMigrationLedgerEntry Entry(Guid legacyId, Guid newId, string kind, string name) {
        var entry = new GivingMigrationLedgerEntry();
        entry.LegacyId = legacyId;
        entry.NewId = newId;
        entry.Kind = kind;
        entry.Name = name;

        return entry;
    }

    private static GivingMigrationRunItemRes NewItem(GivingMigrationCampaignRes plan) {
        var item = new GivingMigrationRunItemRes();
        item.LegacyFormId = plan.LegacyFormId;
        item.LegacyPath = plan.LegacyPath;
        item.CampaignName = plan.CampaignName;
        item.OfferingsExpected = plan.ExpectedOfferings;

        return item;
    }
}
