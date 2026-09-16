using N3O.Umbraco.Cloud.Platforms.Models;
using Slugify;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public class GivingMigrationPlanner : IGivingMigrationPlanner {
    private const int PageSize = 200;

    private readonly ILegacyGivingTreeReader _reader;
    private readonly ILegacyFormReferenceCounter _referenceCounter;
    private readonly ISlugHelper _slugHelper;
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;

    public GivingMigrationPlanner(ILegacyGivingTreeReader reader,
                                  ILegacyFormReferenceCounter referenceCounter,
                                  ISlugHelper slugHelper,
                                  IContentService contentService,
                                  IContentTypeService contentTypeService) {
        _reader = reader;
        _referenceCounter = referenceCounter;
        _slugHelper = slugHelper;
        _contentService = contentService;
        _contentTypeService = contentTypeService;
    }

    public GivingMigrationPlanRes BuildPlan() {
        var blockers = new List<GivingMigrationIssueRes>();
        var warnings = new List<GivingMigrationIssueRes>();
        var dataLoss = new List<GivingMigrationIssueRes>();

        var campaignTypeAlias = ResolveCampaignContentTypeAlias(blockers);
        var offeringTypeAliases = ResolveOfferingContentTypeAliases(blockers);

        ValidatePlatformsTree(blockers);

        var forms = _reader.GetForms();

        if (forms.Count == 0 && _reader.GetFormContentTypes().Count == 0) {
            blockers.Add(Issue(GivingMigrationConstants.IssueKinds.LegacyTreeNotRecognised,
                               GivingMigrationConstants.Severities.Blocker,
                               detail: "No legacy content type allows any donation option type as a child"));
        }

        var included = forms.Where(x => x.Options.Count > 0).ToList();
        var skipped = forms.Where(x => x.Options.Count == 0).ToList();

        foreach (var form in skipped) {
            warnings.Add(Issue(GivingMigrationConstants.IssueKinds.EmptyForm,
                               GivingMigrationConstants.Severities.Warning,
                               form.Content.Key,
                               form.Content.Name,
                               form.Path,
                               detail: "The form has no donation options so no campaign will be created"));
        }

        var duplicateNames = included.GroupBy(x => x.Content.Name, StringComparer.OrdinalIgnoreCase)
                                     .Where(x => x.Count() > 1)
                                     .Select(x => x.Key)
                                     .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var campaigns = included.Select(x => BuildCampaign(x, duplicateNames, campaignTypeAlias, offeringTypeAliases))
                                .ToList();

        FlagUnderivableNames(campaigns, duplicateNames, blockers);
        FlagUnderivableSlugs(campaigns, blockers);
        FlagSlugCollisions(campaigns, blockers);
        FlagAlreadyMigrated(campaigns, warnings);
        FlagFolderNames(campaigns, warnings);
        ApplyReferenceCounts(campaigns);

        var upsellOffers = _reader.CountOfType(GivingMigrationConstants.Legacy.UpsellOfferAlias);

        if (upsellOffers > 0) {
            dataLoss.Add(Issue(GivingMigrationConstants.IssueKinds.UpsellOfferNotMigrated,
                               GivingMigrationConstants.Severities.DataLoss,
                               detail: upsellOffers +
                                       " upsell offers have no platforms equivalent and will not be migrated"));
        }

        var res = new GivingMigrationPlanRes();
        res.Summary = BuildSummary(forms, skipped, campaigns, blockers, upsellOffers);
        res.Campaigns = campaigns.OrderBy(x => x.CampaignName, StringComparer.OrdinalIgnoreCase).ToList();
        res.Blockers = blockers;
        res.Warnings = warnings;
        res.DataLoss = dataLoss;

        return res;
    }

    private GivingMigrationCampaignRes BuildCampaign(LegacyForm form,
                                                     ICollection<string> duplicateNames,
                                                     string campaignTypeAlias,
                                                     IReadOnlyDictionary<string, string> offeringTypeAliases) {
        var nameFromFolder = duplicateNames.Contains(form.Content.Name) &&
                             !string.IsNullOrWhiteSpace(form.FolderName);

        var campaignName = nameFromFolder ? form.FolderName : form.Content.Name;

        var res = new GivingMigrationCampaignRes();
        res.LegacyFormId = form.Content.Key;
        res.LegacyUdi = ToLegacyUdi(form.Content.Key);
        res.LegacyFormName = form.Content.Name;
        res.LegacyFolderName = form.FolderName;
        res.LegacyPath = form.Path;
        res.LegacyPublished = form.Content.Published;
        res.CampaignName = campaignName;
        res.CampaignSlug = GenerateSlug(campaignName);
        res.CampaignContentTypeAlias = campaignTypeAlias;
        res.NameFromFolder = nameFromFolder;
        res.Status = GivingMigrationConstants.EntryStatuses.Planned;
        res.ExpectedOfferings = form.Options.Count;
        res.Ready = true;
        res.Offerings = form.Options.Select(x => BuildOffering(x, offeringTypeAliases)).ToList();

        return res;
    }

    private GivingMigrationOfferingRes BuildOffering(IContent option,
                                                     IReadOnlyDictionary<string, string> offeringTypeAliases) {
        var contentType = _contentTypeService.Get(option.ContentTypeId);
        var legacyAlias = contentType?.Alias;

        string targetAlias = null;

        if (legacyAlias != null) {
            offeringTypeAliases.TryGetValue(legacyAlias, out targetAlias);
        }

        var res = new GivingMigrationOfferingRes();
        res.LegacyOptionId = option.Key;
        res.LegacyUdi = ToLegacyUdi(option.Key);
        res.LegacyOptionName = option.Name;
        res.LegacyOptionAlias = legacyAlias;
        res.LegacyPublished = option.Published;
        res.OfferingName = option.Name;
        res.OfferingContentTypeAlias = targetAlias;
        res.Status = targetAlias == null
                         ? GivingMigrationConstants.EntryStatuses.Blocked
                         : GivingMigrationConstants.EntryStatuses.Planned;

        return res;
    }

    private void FlagSlugCollisions(IReadOnlyList<GivingMigrationCampaignRes> campaigns,
                                    List<GivingMigrationIssueRes> blockers) {
        var collisions = campaigns.Where(x => IsUsableSlug(x.CampaignSlug))
                                  .GroupBy(x => x.CampaignSlug, StringComparer.OrdinalIgnoreCase)
                                  .Where(x => x.Count() > 1);

        foreach (var collision in collisions) {
            var claimants = collision.ToList();
            var message = "Slug " + collision.Key + " is claimed by " + claimants.Count + " campaigns";

            foreach (var campaign in claimants) {
                Block(campaign, message);
            }

            blockers.Add(Issue(GivingMigrationConstants.IssueKinds.SlugCollision,
                               GivingMigrationConstants.Severities.Blocker,
                               detail: message + ": " +
                                       string.Join("; ", claimants.Select(x => x.LegacyPath))));
        }
    }

    private void FlagAlreadyMigrated(IReadOnlyList<GivingMigrationCampaignRes> campaigns,
                                     List<GivingMigrationIssueRes> warnings) {
        var existing = GetExistingCampaigns();

        if (existing.Count == 0) {
            return;
        }

        foreach (var campaign in campaigns.Where(x => x.Status == GivingMigrationConstants.EntryStatuses.Planned &&
                                                      IsUsableSlug(x.CampaignSlug))) {
            if (!existing.TryGetValue(campaign.CampaignSlug, out var target)) {
                continue;
            }

            campaign.TargetExists = true;
            campaign.TargetCampaignId = target.Key;
            campaign.CreatedOfferings = target.Offerings;
            campaign.Status = GivingMigrationConstants.EntryStatuses.AlreadyMigrated;

            if (target.Offerings >= campaign.ExpectedOfferings) {
                campaign.Message = "A platforms campaign already exists with all " + campaign.ExpectedOfferings +
                                   " offerings";
            } else {
                campaign.Ready = false;
                campaign.Message = "A platforms campaign already exists but has " + target.Offerings + " of " +
                                   campaign.ExpectedOfferings + " offerings";

                warnings.Add(Issue(GivingMigrationConstants.IssueKinds.PartiallyMigrated,
                                   GivingMigrationConstants.Severities.Warning,
                                   campaign.LegacyFormId,
                                   campaign.LegacyFormName,
                                   campaign.LegacyPath,
                                   detail: campaign.Message));
            }
        }
    }

    private void FlagUnderivableNames(IReadOnlyList<GivingMigrationCampaignRes> campaigns,
                                      ICollection<string> duplicateNames,
                                      List<GivingMigrationIssueRes> blockers) {
        foreach (var campaign in campaigns.Where(x => !x.NameFromFolder &&
                                                      duplicateNames.Contains(x.LegacyFormName))) {
            var message = "The form name is not unique and it has no folder to take a name from";

            Block(campaign, message);

            blockers.Add(Issue(GivingMigrationConstants.IssueKinds.NameNotDerivable,
                               GivingMigrationConstants.Severities.Blocker,
                               campaign.LegacyFormId,
                               campaign.LegacyFormName,
                               campaign.LegacyPath,
                               detail: message));
        }
    }

    private void FlagUnderivableSlugs(IReadOnlyList<GivingMigrationCampaignRes> campaigns,
                                      List<GivingMigrationIssueRes> blockers) {
        foreach (var campaign in campaigns.Where(x => !IsUsableSlug(x.CampaignSlug))) {
            var message = "A usable slug cannot be derived from the name " + campaign.CampaignName;

            Block(campaign, message);

            blockers.Add(Issue(GivingMigrationConstants.IssueKinds.SlugNotDerivable,
                               GivingMigrationConstants.Severities.Blocker,
                               campaign.LegacyFormId,
                               campaign.LegacyFormName,
                               campaign.LegacyPath,
                               detail: message));
        }
    }

    private void ApplyReferenceCounts(IReadOnlyList<GivingMigrationCampaignRes> campaigns) {
        var counts = _referenceCounter.CountReferences(campaigns.Select(x => x.LegacyFormId).ToList());

        foreach (var campaign in campaigns) {
            campaign.PageReferences = counts.TryGetValue(campaign.LegacyFormId, out var count) ? count : 0;
        }
    }

    private void FlagFolderNames(IReadOnlyList<GivingMigrationCampaignRes> campaigns,
                                 List<GivingMigrationIssueRes> warnings) {
        foreach (var campaign in campaigns.Where(x => x.NameFromFolder)) {
            warnings.Add(Issue(GivingMigrationConstants.IssueKinds.NameDisambiguated,
                               GivingMigrationConstants.Severities.Warning,
                               campaign.LegacyFormId,
                               campaign.LegacyFormName,
                               campaign.LegacyPath,
                               detail: "Form name is not unique so the folder name was used: " +
                                       campaign.CampaignName));
        }
    }

    private IReadOnlyDictionary<string, ExistingCampaign> GetExistingCampaigns() {
        var existing = new Dictionary<string, ExistingCampaign>(StringComparer.OrdinalIgnoreCase);
        var composition = _contentTypeService.Get(PlatformsConstants.Campaigns.CompositionAlias);

        if (composition == null) {
            return existing;
        }

        var offeringTypeIds = GetOfferingContentTypeIds();

        foreach (var contentType in _contentTypeService.GetComposedOf(composition.Id)) {
            long page = 0;
            long total;

            do {
                var items = _contentService.GetPagedOfType(contentType.Id, page, PageSize, out total, null);

                foreach (var item in items.Where(x => !x.Trashed)) {
                    var slug = GenerateSlug(item.Name);

                    if (IsUsableSlug(slug) && !existing.ContainsKey(slug)) {
                        existing[slug] = new ExistingCampaign(item.Key, CountOfferings(item.Id, offeringTypeIds));
                    }
                }

                page++;
            } while (page * PageSize < total);
        }

        return existing;
    }

    private IReadOnlyList<int> GetOfferingContentTypeIds() {
        var composition = _contentTypeService.Get(PlatformsConstants.Offerings.CompositionAlias);

        if (composition == null) {
            return [];
        }

        return _contentTypeService.GetComposedOf(composition.Id)
                                  .Select(x => x.Id)
                                  .ToList();
    }

    private int CountOfferings(int campaignId, IReadOnlyList<int> offeringTypeIds) {
        if (offeringTypeIds.Count == 0) {
            return 0;
        }

        var count = 0;
        long page = 0;
        long total;

        do {
            var children = _contentService.GetPagedChildren(campaignId, page, PageSize, out total);

            count += children.Count(x => !x.Trashed && offeringTypeIds.Contains(x.ContentTypeId));

            page++;
        } while (page * PageSize < total);

        return count;
    }

    private string ResolveCampaignContentTypeAlias(List<GivingMigrationIssueRes> blockers) {
        var alias = PlatformsConstants.Campaigns.Standard;

        if (_contentTypeService.Get(alias) == null) {
            blockers.Add(Issue(GivingMigrationConstants.IssueKinds.TargetContentTypeMissing,
                               GivingMigrationConstants.Severities.Blocker,
                               detail: "The target campaign content type does not exist: " + alias));

            return null;
        }

        return alias;
    }

    private IReadOnlyDictionary<string, string> ResolveOfferingContentTypeAliases(
        List<GivingMigrationIssueRes> blockers) {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        map[GivingMigrationConstants.Legacy.FundDonationOptionAlias] = PlatformsConstants.Offerings.Fund;
        map[GivingMigrationConstants.Legacy.SponsorshipDonationOptionAlias] = PlatformsConstants.Offerings.Sponsorship;
        map[GivingMigrationConstants.Legacy.FeedbackDonationOptionAlias] = PlatformsConstants.Offerings.Feedback;

        var resolved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in map) {
            if (_contentTypeService.Get(pair.Value) == null) {
                blockers.Add(Issue(GivingMigrationConstants.IssueKinds.TargetContentTypeMissing,
                                   GivingMigrationConstants.Severities.Blocker,
                                   detail: "The target offering content type does not exist: " + pair.Value));

                continue;
            }

            resolved[pair.Key] = pair.Value;
        }

        return resolved;
    }

    private void ValidatePlatformsTree(List<GivingMigrationIssueRes> blockers) {
        if (_contentTypeService.Get(PlatformsConstants.Platforms.Alias) == null) {
            blockers.Add(Issue(GivingMigrationConstants.IssueKinds.PlatformsTreeMissing,
                               GivingMigrationConstants.Severities.Blocker,
                               detail: "The platforms content type does not exist: " +
                                       PlatformsConstants.Platforms.Alias));
        }
    }

    private GivingMigrationSummaryRes BuildSummary(IReadOnlyList<LegacyForm> forms,
                                                   IReadOnlyList<LegacyForm> skipped,
                                                   IReadOnlyList<GivingMigrationCampaignRes> campaigns,
                                                   IReadOnlyList<GivingMigrationIssueRes> blockers,
                                                   int upsellOffers) {
        var planned = campaigns.Where(x => x.Status == GivingMigrationConstants.EntryStatuses.Planned).ToList();

        var summary = new GivingMigrationSummaryRes();
        summary.LegacyForms = forms.Count;
        summary.LegacyFolders = _reader.CountOfType(GivingMigrationConstants.Legacy.DonationFormFolderAlias);
        summary.LegacyOptions = forms.Sum(x => x.Options.Count);
        summary.LegacyUpsellOffers = upsellOffers;
        summary.PlannedCampaigns = planned.Count;
        summary.PlannedOfferings = planned.SelectMany(x => x.Offerings)
                                          .Count(x => x.Status == GivingMigrationConstants.EntryStatuses.Planned);
        summary.AlreadyMigratedCampaigns =
            campaigns.Count(x => x.Status == GivingMigrationConstants.EntryStatuses.AlreadyMigrated);
        summary.SkippedEmptyForms = skipped.Count;
        summary.NamesTakenFromFolder = campaigns.Count(x => x.NameFromFolder);
        summary.SlugCollisions = blockers.Count(x => x.Kind == GivingMigrationConstants.IssueKinds.SlugCollision);
        summary.BlockedCampaigns =
            campaigns.Count(x => x.Status == GivingMigrationConstants.EntryStatuses.Blocked);
        summary.UnreferencedCampaigns = campaigns.Count(x => x.PageReferences == 0);
        summary.Ready = blockers.Count == 0 &&
                        campaigns.All(x => x.Ready) &&
                        summary.PlannedCampaigns + summary.AlreadyMigratedCampaigns > 0;

        return summary;
    }

    private string GenerateSlug(string name) {
        return string.IsNullOrWhiteSpace(name) ? null : _slugHelper.GenerateSlug(name);
    }

    private static bool IsUsableSlug(string slug) {
        return !string.IsNullOrWhiteSpace(slug) && slug.Any(char.IsLetterOrDigit);
    }

    private static void Block(GivingMigrationCampaignRes campaign, string message) {
        if (campaign.Status == GivingMigrationConstants.EntryStatuses.Blocked) {
            return;
        }

        campaign.Ready = false;
        campaign.Status = GivingMigrationConstants.EntryStatuses.Blocked;
        campaign.Message = message;
    }

    private static string ToLegacyUdi(Guid key) {
        return "umb://document/" + key.ToString("N").ToLowerInvariant();
    }

    private static GivingMigrationIssueRes Issue(string kind,
                                                 string severity,
                                                 Guid? legacyId = null,
                                                 string legacyName = null,
                                                 string legacyPath = null,
                                                 string propertyAlias = null,
                                                 string detail = null) {
        var res = new GivingMigrationIssueRes();
        res.Kind = kind;
        res.Severity = severity;
        res.LegacyId = legacyId;
        res.LegacyName = legacyName;
        res.LegacyPath = legacyPath;
        res.PropertyAlias = propertyAlias;
        res.Detail = detail;

        return res;
    }

    private class ExistingCampaign {
        public ExistingCampaign(Guid key, int offerings) {
            Key = key;
            Offerings = offerings;
        }

        public Guid Key { get; }
        public int Offerings { get; }
    }
}
