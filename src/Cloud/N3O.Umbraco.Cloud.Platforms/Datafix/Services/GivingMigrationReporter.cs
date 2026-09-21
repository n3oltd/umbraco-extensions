using N3O.Umbraco.Cloud;
using N3O.Umbraco.Cloud.Platforms.Models;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public class GivingMigrationReporter : IGivingMigrationReporter {
    private readonly IGivingMigrationPlanner _planner;
    private readonly IGivingMigrationStore _store;
    private readonly ILegacyGivingTreeReader _reader;
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly ISubscriptionAccessor _subscriptionAccessor;

    public GivingMigrationReporter(IGivingMigrationPlanner planner,
                                   IGivingMigrationStore store,
                                   ILegacyGivingTreeReader reader,
                                   IContentService contentService,
                                   IContentTypeService contentTypeService,
                                   ISubscriptionAccessor subscriptionAccessor) {
        _planner = planner;
        _store = store;
        _reader = reader;
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _subscriptionAccessor = subscriptionAccessor;
    }

    public GivingMigrationLedgerRes BuildLedger() {
        var entries = _store.GetLedger();

        var res = new GivingMigrationLedgerRes();
        res.Campaigns = entries.Count(x => x.Kind == GivingMigrationConstants.LedgerKinds.Campaign);
        res.Offerings = entries.Count(x => x.Kind == GivingMigrationConstants.LedgerKinds.Offering);
        res.CrossSells = entries.Count(x => x.Kind == GivingMigrationConstants.LedgerKinds.CrossSell);
        res.Entries = entries.Select(Map).ToList();

        return res;
    }

    public GivingMigrationStatusRes BuildStatus() {
        var res = new GivingMigrationStatusRes();
        res.SubscriptionId = _subscriptionAccessor.GetSubscription().Id.ToString();
        var forms = _reader.GetForms();

        res.LegacyForms = forms.Count;
        res.LegacyDonationOptions = forms.Sum(x => x.Options.Count);

        var optionCounts = forms.ToDictionary(x => x.Content.Key, x => x.Options.Count);
        var formNames = forms.ToDictionary(x => x.Content.Key, x => x.Content.Name);
        var offeringTypeIds = GivingMigrationContent.GetOfferingContentTypeIds(_contentTypeService);
        var items = new List<GivingMigrationStatusItemRes>();

        foreach (var entry in _store.GetLedger()
                                    .Where(x => x.Kind == GivingMigrationConstants.LedgerKinds.Campaign)) {
            var campaign = _contentService.GetById(entry.NewId);

            if (campaign == null || campaign.Trashed) {
                continue;
            }

            var offerings = GivingMigrationContent.GetChildren(_contentService, campaign.Id)
                            .Where(x => offeringTypeIds.Contains(x.ContentTypeId))
                            .ToList();

            var item = new GivingMigrationStatusItemRes();
            item.LegacyFormId = entry.LegacyId;
            item.LegacyFormName = formNames.GetValueOrDefault(entry.LegacyId);
            item.LegacyDonationOptions = optionCounts.GetValueOrDefault(entry.LegacyId);
            item.CampaignId = campaign.Key;
            item.CampaignName = campaign.Name;
            item.Published = campaign.Published;
            item.Offerings = offerings.Count;
            item.PublishedOfferings = offerings.Count(x => x.Published);
            item.OfferingsMatchOptions = item.Offerings == item.LegacyDonationOptions;

            items.Add(item);
        }

        var plan = _planner.BuildPlan();

        // A ledger entry for a form that no longer exists would otherwise stand in for a form that was never
        // migrated, so the forms themselves are compared rather than how many of them there are.
        var migratedFormIds = items.Select(x => x.LegacyFormId).ToHashSet();

        res.Items = items;
        res.Campaigns = items.Count;
        res.PublishedCampaigns = items.Count(x => x.Published);
        res.Offerings = items.Sum(x => x.Offerings);
        res.PublishedOfferings = items.Sum(x => x.PublishedOfferings);
        res.UnmigratedForms = forms.Count(x => !migratedFormIds.Contains(x.Content.Key));
        res.UnmigratedCrossSells =
            plan.CrossSells.Count(x => x.Status != GivingMigrationConstants.EntryStatuses.AlreadyMigrated);
        res.CampaignsWithOfferingMismatch = items.Count(x => !x.OfferingsMatchOptions);
        res.Complete = res.Campaigns > 0 &&
                       res.UnmigratedForms == 0 &&
                       res.UnmigratedCrossSells == 0 &&
                       res.Campaigns == res.PublishedCampaigns &&
                       res.Offerings == res.PublishedOfferings &&
                       res.CampaignsWithOfferingMismatch == 0;

        return res;
    }

    private static GivingMigrationLedgerEntryRes Map(GivingMigrationLedgerEntry entry) {
        var res = new GivingMigrationLedgerEntryRes();
        res.LegacyId = entry.LegacyId;
        res.NewId = entry.NewId;
        res.Kind = entry.Kind;
        res.Name = entry.Name;

        return res;
    }
}
