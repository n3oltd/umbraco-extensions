using N3O.Umbraco.Cloud;
using N3O.Umbraco.Cloud.Platforms.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public class GivingMigrationRunner : IGivingMigrationRunner {
    // A blocker attached to one campaign already keeps that campaign out of the planned set, so it does not have to
    // stop the campaigns that are fine. Anything about the site's schema does, because no write would be safe.
    private static readonly string[] PerCampaignIssueKinds = [
        GivingMigrationConstants.IssueKinds.NameNotDerivable
    ];

    private readonly IGivingMigrationPlanner _planner;
    private readonly IGivingMigrationWriter _writer;
    private readonly IGivingMigrationMedia _media;
    private readonly IGivingMigrationStore _store;
    private readonly ISubscriptionAccessor _subscriptionAccessor;

    public GivingMigrationRunner(IGivingMigrationPlanner planner,
                                 IGivingMigrationWriter writer,
                                 IGivingMigrationMedia media,
                                 IGivingMigrationStore store,
                                 ISubscriptionAccessor subscriptionAccessor) {
        _planner = planner;
        _writer = writer;
        _media = media;
        _store = store;
        _subscriptionAccessor = subscriptionAccessor;
    }

    public GivingMigrationRunRes Complete(CompleteGivingMigrationReq req) {
        var res = NewRes();

        var plan = _planner.BuildPlan();

        var migrated = plan.Campaigns
                           .Where(x => x.Status == GivingMigrationConstants.EntryStatuses.AlreadyMigrated)
                           .ToList();

        if (migrated.Count == 0) {
            res.Message = "No migrated campaign was found to publish";

            return res;
        }

        var limit = req.Limit ?? migrated.Count;

        var items = migrated.Take(limit).Select(_writer.PublishOfferings).ToList();

        res.Items = items;
        res.Attempted = items.Count;
        res.Published = items.Count(x => x.Outcome == GivingMigrationConstants.Outcomes.Published);
        res.Failed = items.Count(x => x.Outcome == GivingMigrationConstants.Outcomes.Failed);
        res.OfferingsCreated = items.Sum(x => x.OfferingsCreated);

        return res;
    }

    public async Task<GivingMigrationRunRes> MigrateAsync(MigrateGivingReq req,
                                                          CancellationToken cancellationToken) {
        var res = NewRes();

        var plan = _planner.BuildPlan();

        var fatal = plan.Blockers.Where(x => !PerCampaignIssueKinds.Contains(x.Kind)).ToList();

        if (fatal.Count > 0) {
            res.Issues = fatal;
            res.Message = "The plan has " +
                          fatal.Count +
                          " blockers that stop any campaign being written, so nothing was migrated";

            return res;
        }

        var containerId = _writer.GetCampaignsContainerId();

        if (containerId == null) {
            res.Message = "A single platforms campaigns container could not be found";

            return res;
        }

        var placeholders = await _media.ResolveAsync(req.DefaultIconMediaId,
                                                     req.DefaultImageMediaId,
                                                     req.DefaultHeroImageMediaId,
                                                     req.AllowPlaceholderMedia,
                                                     cancellationToken);

        res.IconMediaId = placeholders.Icon.Id;
        res.ImageMediaId = placeholders.Image.Id;
        res.HeroImageMediaId = placeholders.HeroImage.Id;
        res.PlaceholderMedia = placeholders.Icon.Created || placeholders.Image.Created ||
                               placeholders.HeroImage.Created;

        var limit = req.Limit ?? GivingMigrationConstants.DefaultMigrateLimit;
        var items = new List<GivingMigrationRunItemRes>();

        foreach (var campaign in plan.Campaigns
                                     .Where(x => x.Status == GivingMigrationConstants.EntryStatuses.Planned)
                                     .Take(limit)) {
            var entries = new List<GivingMigrationLedgerEntry>();

            items.Add(_writer.CreateCampaign(campaign, containerId.Value, placeholders, entries));

            // The campaign is already committed to Umbraco by this point, and a re-run finds it only through the
            // ledger, so each one is recorded before the next is started rather than after the whole loop.
            _store.AppendLedger(entries);
        }

        var alreadyMigrated = plan.Campaigns
                                  .Where(x => x.Status == GivingMigrationConstants.EntryStatuses.AlreadyMigrated)
                                  .ToList();

        res.AlreadyMigrated = alreadyMigrated.Count;
        res.Blocked = plan.Campaigns.Count(x => x.Status == GivingMigrationConstants.EntryStatuses.Blocked);

        if (res.Blocked > 0) {
            res.Issues = plan.Blockers.Where(x => PerCampaignIssueKinds.Contains(x.Kind)).ToList();
        }

        // A campaign that lost some of its offerings to a failure is already in the ledger, so the run that finds
        // it again is the only chance to finish it.
        var migratedOptionIds = _store.GetLedger()
                                      .Where(x => x.Kind == GivingMigrationConstants.LedgerKinds.Offering)
                                      .Select(x => x.LegacyId)
                                      .ToHashSet();

        foreach (var campaign in alreadyMigrated) {
            var entries = new List<GivingMigrationLedgerEntry>();

            var item = _writer.CreateMissingOfferings(campaign, migratedOptionIds, placeholders, entries);

            if (item.Outcome == GivingMigrationConstants.Outcomes.NotAttempted) {
                continue;
            }

            _store.AppendLedger(entries);

            items.Add(item);
        }

        res.Items = items;
        res.Attempted = items.Count;
        res.Created = items.Count(x => x.Outcome == GivingMigrationConstants.Outcomes.Created);
        res.Failed = items.Count(x => x.Outcome == GivingMigrationConstants.Outcomes.Failed);
        res.OfferingsCreated = items.Sum(x => x.OfferingsCreated);

        if (req.IncludeCrossSells) {
            var crossSells = MigrateCrossSells(plan, placeholders, limit - items.Count, res);

            res.Items = items.Concat(crossSells).ToList();
            res.Attempted += crossSells.Count;
            res.Failed += crossSells.Count(x => x.Outcome == GivingMigrationConstants.Outcomes.Failed);
        }

        return res;
    }

    // Counted and returned apart from the campaigns so the campaign totals stay the campaign totals.
    private IReadOnlyList<GivingMigrationRunItemRes> MigrateCrossSells(GivingMigrationPlanRes plan,
                                                                       GivingPlaceholders placeholders,
                                                                       int remaining,
                                                                       GivingMigrationRunRes res) {
        if (remaining <= 0) {
            return [];
        }

        var planned = plan.CrossSells
                          .Where(x => x.Status == GivingMigrationConstants.EntryStatuses.Planned)
                          .Take(remaining)
                          .ToList();

        if (planned.Count == 0) {
            return [];
        }

        var containerId = _writer.EnsureCrossSellsContainerId(out var containerProblem);

        if (containerId == null) {
            res.Message = "Cross sells were not migrated. " + containerProblem;

            return [];
        }

        var items = new List<GivingMigrationRunItemRes>();

        foreach (var crossSell in planned) {
            var entries = new List<GivingMigrationLedgerEntry>();

            items.Add(_writer.CreateCrossSell(crossSell, containerId.Value, placeholders, entries));

            // Recorded one at a time for the same reason the campaigns are: an interruption part way through would
            // otherwise leave every cross sell created so far absent from the ledger and duplicated on the next run.
            _store.AppendLedger(entries);
        }

        res.CrossSellsCreated = items.Count(x => x.Outcome == GivingMigrationConstants.Outcomes.Created);

        return items;
    }

    private GivingMigrationRunRes NewRes() {
        var res = new GivingMigrationRunRes();
        res.SubscriptionId = _subscriptionAccessor.GetSubscription().Id.ToString();

        return res;
    }
}
