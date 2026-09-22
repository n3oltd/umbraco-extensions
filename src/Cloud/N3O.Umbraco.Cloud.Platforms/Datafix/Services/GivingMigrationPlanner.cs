using N3O.Umbraco.Cloud.Platforms.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using UmbracoUdi = Umbraco.Cms.Core.Constants.UdiEntityType;

namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public class GivingMigrationPlanner : IGivingMigrationPlanner {
    private readonly ILegacyGivingTreeReader _reader;
    private readonly IGivingMigrationStore _store;
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;

    public GivingMigrationPlanner(ILegacyGivingTreeReader reader,
                                  IGivingMigrationStore store,
                                  IContentService contentService,
                                  IContentTypeService contentTypeService) {
        _reader = reader;
        _store = store;
        _contentService = contentService;
        _contentTypeService = contentTypeService;
    }

    public GivingMigrationPlanRes BuildPlan() {
        var blockers = new List<GivingMigrationIssueRes>();
        var warnings = new List<GivingMigrationIssueRes>();
        var dataLoss = new List<GivingMigrationIssueRes>();

        var campaignTypeAlias = ResolveCampaignContentTypeAlias(blockers);
        var offeringTypeAliases = ResolveOfferingContentTypeAliases(blockers);
        var crossSellTypeAlias = ResolveCrossSellContentTypeAlias(warnings);

        ValidatePlatformsTree(blockers);

        var forms = _reader.GetForms();

        if (forms.Count == 0 && _reader.GetFormContentTypes().Count == 0) {
            blockers.Add(Issue(GivingMigrationConstants.IssueKinds.LegacyTreeNotRecognised,
                               GivingMigrationConstants.Severities.Blocker,
                               detail: "No legacy content type allows any donation option type as a child"));
        }

        // Every form migrates, including those with no options, so a page that still points at one keeps working.
        var campaigns = forms.Select(x => BuildCampaign(x, campaignTypeAlias, offeringTypeAliases)).ToList();

        // A site can nest a second grouping layer that is itself a form type. Each nested form migrates to its own
        // campaign, so a page picking the outer form has to be pointed at one of them by hand afterwards.
        foreach (var form in forms.Where(x => x.NestedForms.Count > 0)) {
            warnings.Add(Issue(GivingMigrationConstants.IssueKinds.AmbiguousReference,
                               GivingMigrationConstants.Severities.Warning,
                               form.Content.Key,
                               form.Content.Name,
                               form.Path,
                               detail: "The form contains " +
                                       form.NestedForms.Count +
                                       " nested forms, each migrating to its own campaign, so this form's own " +
                                       "campaign holds only the options that are not inside one"));
        }

        foreach (var campaign in campaigns.Where(x => x.ExpectedOfferings == 0)) {
            warnings.Add(Issue(GivingMigrationConstants.IssueKinds.EmptyForm,
                               GivingMigrationConstants.Severities.Warning,
                               campaign.LegacyFormId,
                               campaign.LegacyFormName,
                               campaign.LegacyPath,
                               detail: "The form has no donation options so the campaign will have no offerings"));
        }

        FlagNameCollisions(campaigns, blockers);
        FlagAlreadyMigrated(campaigns);
        FlagExistingCampaignNames(campaigns, blockers);
        FlagPrefixedNames(campaigns, warnings);

        var crossSells = BuildCrossSells(crossSellTypeAlias);

        RecordDroppedProperties(forms, dataLoss);

        var res = new GivingMigrationPlanRes();
        res.Summary = BuildSummary(forms, campaigns, crossSells, blockers);
        res.Campaigns = campaigns.OrderBy(x => x.CampaignName, StringComparer.OrdinalIgnoreCase).ToList();
        res.CrossSells = crossSells;
        res.Blockers = blockers;
        res.Warnings = warnings;
        res.DataLoss = dataLoss;

        return res;
    }

    private GivingMigrationCampaignRes BuildCampaign(LegacyForm form,
                                                     string campaignTypeAlias,
                                                     IReadOnlyDictionary<string, string> offeringTypeAliases) {
        var campaignName = BuildCampaignName(form.Content.Name, form.FolderName);

        var res = new GivingMigrationCampaignRes();
        res.LegacyFormId = form.Content.Key;
        res.LegacyUdi = Udi.Create(UmbracoUdi.Document, form.Content.Key).ToString();
        res.LegacyFormName = form.Content.Name;
        res.LegacyFolderName = form.FolderName;
        res.LegacyPath = form.Path;
        res.LegacyPublished = form.Content.Published;
        res.CampaignName = campaignName;
        res.CampaignContentTypeAlias = campaignTypeAlias;
        res.NameFromFolder = !string.Equals(campaignName, form.Content.Name, StringComparison.Ordinal);
        res.Status = GivingMigrationConstants.EntryStatuses.Planned;
        res.ExpectedOfferings = form.Options.Count;
        res.Ready = campaignTypeAlias != null;
        res.Offerings = form.Options.Select(x => BuildOffering(x, offeringTypeAliases)).ToList();

        return res;
    }

    // The campaign tree is flat, so a form name that is only unique within its folder has to carry the folder into
    // its campaign name or the picker shows several identically named campaigns.
    private static string BuildCampaignName(string formName, string folderName) {
        if (string.IsNullOrWhiteSpace(folderName)) {
            return formName;
        }

        if (string.Equals(folderName, formName, StringComparison.OrdinalIgnoreCase)) {
            return formName;
        }

        return folderName + GivingMigrationConstants.NameSeparator + formName;
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

    private IReadOnlyList<GivingMigrationCrossSellRes> BuildCrossSells(string crossSellTypeAlias) {
        var ledger = _store.GetLedger()
                           .Where(x => x.Kind == GivingMigrationConstants.LedgerKinds.CrossSell)
                           .ToDictionary(x => x.LegacyId, x => x.NewId);

        var crossSells = new List<GivingMigrationCrossSellRes>();

        foreach (var upsell in _reader.GetUpsellOffers()) {
            var res = new GivingMigrationCrossSellRes();
            res.LegacyUpsellId = upsell.Key;
            res.LegacyUpsellName = upsell.Name;
            res.LegacyPath = _reader.BuildPath(upsell);
            res.CrossSellName = upsell.Name;
            res.CrossSellContentTypeAlias = crossSellTypeAlias;
            res.Ready = crossSellTypeAlias != null;

            // The ledger records what was created, not what still exists, so the target is resolved the same way
            // FlagAlreadyMigrated resolves a campaign's. A deleted cross sell falls back to being planned again.
            var existing = ledger.TryGetValue(upsell.Key, out var existingId)
                               ? _contentService.GetById(existingId)
                               : null;

            if (existing != null && !existing.Trashed) {
                res.Status = GivingMigrationConstants.EntryStatuses.AlreadyMigrated;
                res.TargetCrossSellId = existingId;
            } else if (crossSellTypeAlias == null) {
                res.Status = GivingMigrationConstants.EntryStatuses.Blocked;
                res.Message = "The target cross sell content type does not exist";
            } else {
                res.Status = GivingMigrationConstants.EntryStatuses.Planned;
            }

            crossSells.Add(res);
        }

        return crossSells;
    }

    // These legacy properties have no platforms equivalent and are deliberately not carried over, so only an option
    // that actually sets one loses anything.
    private static void RecordDroppedProperties(IReadOnlyList<LegacyForm> forms,
                                                List<GivingMigrationIssueRes> dataLoss) {
        var dropped = new[] {
            GivingMigrationConstants.Properties.HideDonation,
            GivingMigrationConstants.Properties.HideQuantity,
            GivingMigrationConstants.Properties.HideRegularGiving
        };

        var affected = forms.SelectMany(x => x.Options).Count(x => dropped.Any(a => x.GetValue<bool>(a)));

        if (affected == 0) {
            return;
        }

        dataLoss.Add(Issue(GivingMigrationConstants.IssueKinds.DroppedProperty,
                           GivingMigrationConstants.Severities.DataLoss,
                           detail: string.Join(", ", dropped) + " have no platforms equivalent and will not be " +
                                   "migrated for " + affected + " options that set one"));
    }

    private void FlagNameCollisions(IReadOnlyList<GivingMigrationCampaignRes> campaigns,
                                    List<GivingMigrationIssueRes> blockers) {
        var collisions = campaigns.GroupBy(x => x.CampaignName, StringComparer.OrdinalIgnoreCase)
                                  .Where(x => x.Count() > 1);

        foreach (var collision in collisions) {
            var claimants = collision.ToList();
            var message = "The campaign name " + collision.Key + " is claimed by " + claimants.Count + " forms";

            foreach (var campaign in claimants) {
                Block(campaign, message);
            }

            blockers.Add(Issue(GivingMigrationConstants.IssueKinds.NameNotDerivable,
                               GivingMigrationConstants.Severities.Blocker,
                               detail: message + ": " + string.Join("; ", claimants.Select(x => x.LegacyPath))));
        }

    }

    // Comparing the forms only against each other would still let the migration create the twin of a campaign that
    // is already in the container. Runs after the ledger pass, so a campaign matching its own migrated target is
    // left alone.
    private void FlagExistingCampaignNames(IReadOnlyList<GivingMigrationCampaignRes> campaigns,
                                           List<GivingMigrationIssueRes> blockers) {
        var existingNames = GetExistingCampaignNames();

        if (existingNames.Count == 0) {
            return;
        }

        foreach (var campaign in campaigns.Where(x => x.Status == GivingMigrationConstants.EntryStatuses.Planned &&
                                                      !x.TargetExists &&
                                                      existingNames.Contains(x.CampaignName))) {
            var message = "A campaign named " + campaign.CampaignName + " already exists in the campaigns container";

            Block(campaign, message);

            blockers.Add(Issue(GivingMigrationConstants.IssueKinds.NameNotDerivable,
                               GivingMigrationConstants.Severities.Blocker,
                               campaign.LegacyFormId,
                               campaign.LegacyFormName,
                               campaign.LegacyPath,
                               detail: message));
        }
    }

    private IReadOnlyCollection<string> GetExistingCampaignNames() {
        var containers = GivingMigrationContent.GetAllOfAlias(_contentService,
                                                              _contentTypeService,
                                                              GivingMigrationConstants.Platforms.CampaignsAlias)
                                               .Where(x => !x.Trashed)
                                               .ToList();

        return containers.SelectMany(x => GivingMigrationContent.GetChildren(_contentService, x.Id))
                         .Select(x => x.Name)
                         .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    // The ledger is the only authority on what has already been migrated; matching on name would silently adopt
    // a campaign an editor happened to create by hand.
    private void FlagAlreadyMigrated(IReadOnlyList<GivingMigrationCampaignRes> campaigns) {
        var ledger = _store.GetLedger()
                           .Where(x => x.Kind == GivingMigrationConstants.LedgerKinds.Campaign)
                           .ToDictionary(x => x.LegacyId, x => x.NewId);

        if (ledger.Count == 0) {
            return;
        }

        var offeringTypeIds = GivingMigrationContent.GetOfferingContentTypeIds(_contentTypeService);

        foreach (var campaign in campaigns) {
            if (!ledger.TryGetValue(campaign.LegacyFormId, out var targetId)) {
                continue;
            }

            var target = _contentService.GetById(targetId);

            if (target == null || target.Trashed) {
                continue;
            }

            campaign.TargetExists = true;
            campaign.TargetCampaignId = targetId;
            campaign.CreatedOfferings = CountOfferings(target.Id, offeringTypeIds);

            // A campaign blocked by a name collision keeps that status, otherwise it reports as already migrated
            // while staying not ready, with nothing saying why.
            if (campaign.Status == GivingMigrationConstants.EntryStatuses.Blocked) {
                continue;
            }

            campaign.Status = GivingMigrationConstants.EntryStatuses.AlreadyMigrated;
            campaign.Message = "A platforms campaign already exists with " + campaign.CreatedOfferings + " of " +
                               campaign.ExpectedOfferings + " offerings";
        }
    }

    private void FlagPrefixedNames(IReadOnlyList<GivingMigrationCampaignRes> campaigns,
                                   List<GivingMigrationIssueRes> warnings) {
        foreach (var campaign in campaigns.Where(x => x.NameFromFolder)) {
            warnings.Add(Issue(GivingMigrationConstants.IssueKinds.NameDisambiguated,
                               GivingMigrationConstants.Severities.Warning,
                               campaign.LegacyFormId,
                               campaign.LegacyFormName,
                               campaign.LegacyPath,
                               detail: "The campaign name was prefixed with the folder: " + campaign.CampaignName));
        }
    }

    private int CountOfferings(int campaignId, IReadOnlyList<int> offeringTypeIds) {
        if (offeringTypeIds.Count == 0) {
            return 0;
        }

        return GivingMigrationContent.GetChildren(_contentService, campaignId)
                                     .Count(x => offeringTypeIds.Contains(x.ContentTypeId));
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

    private string ResolveCrossSellContentTypeAlias(List<GivingMigrationIssueRes> warnings) {
        var alias = PlatformsConstants.CrossSells.Fund;

        if (_contentTypeService.Get(alias) == null) {
            warnings.Add(Issue(GivingMigrationConstants.IssueKinds.TargetContentTypeMissing,
                               GivingMigrationConstants.Severities.Warning,
                               detail: "The target cross sell content type does not exist: " + alias));

            return null;
        }

        // The container is site owned, like the campaigns container, and nothing here creates it. Without it the
        // cross sells stay blocked and the migration cannot complete, so the plan says which one is missing rather
        // than leaving it to be discovered at purge time.
        if (_contentTypeService.Get(PlatformsConstants.CrossSells.ContainerAlias) == null) {
            warnings.Add(Issue(GivingMigrationConstants.IssueKinds.TargetContentTypeMissing,
                               GivingMigrationConstants.Severities.Warning,
                               detail: "The cross sell container content type does not exist: " +
                                       PlatformsConstants.CrossSells.ContainerAlias +
                                       ". Create it and a container node before migrating cross sells"));

            return null;
        }

        return alias;
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
                                                   IReadOnlyList<GivingMigrationCampaignRes> campaigns,
                                                   IReadOnlyList<GivingMigrationCrossSellRes> crossSells,
                                                   IReadOnlyList<GivingMigrationIssueRes> blockers) {
        var planned = campaigns.Where(x => x.Status == GivingMigrationConstants.EntryStatuses.Planned).ToList();

        var summary = new GivingMigrationSummaryRes();
        summary.LegacyForms = forms.Count;
        summary.LegacyFolders = _reader.CountOfType(GivingMigrationConstants.Legacy.DonationFormFolderAlias);
        summary.LegacyOptions = forms.Sum(x => x.Options.Count);
        summary.LegacyUpsellOffers = crossSells.Count;
        summary.PlannedCampaigns = planned.Count;
        summary.PlannedOfferings = planned.SelectMany(x => x.Offerings)
                                          .Count(x => x.Status == GivingMigrationConstants.EntryStatuses.Planned);
        summary.PlannedCrossSells =
            crossSells.Count(x => x.Status == GivingMigrationConstants.EntryStatuses.Planned);
        summary.AlreadyMigratedCampaigns =
            campaigns.Count(x => x.Status == GivingMigrationConstants.EntryStatuses.AlreadyMigrated);
        summary.EmptyForms = campaigns.Count(x => x.ExpectedOfferings == 0);
        summary.FormsWithNestedForms = forms.Count(x => x.NestedForms.Count > 0);
        summary.NamesPrefixedWithFolder = campaigns.Count(x => x.NameFromFolder);
        summary.BlockedCampaigns = campaigns.Count(x => x.Status == GivingMigrationConstants.EntryStatuses.Blocked);
        summary.Ready = blockers.Count == 0 &&
                        campaigns.All(x => x.Ready) &&
                        summary.PlannedCampaigns + summary.AlreadyMigratedCampaigns > 0;

        return summary;
    }

    private static void Block(GivingMigrationCampaignRes campaign, string message) {
        if (campaign.Status == GivingMigrationConstants.EntryStatuses.Blocked) {
            return;
        }

        campaign.Ready = false;
        campaign.Status = GivingMigrationConstants.EntryStatuses.Blocked;
        campaign.Message = message;
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
}
