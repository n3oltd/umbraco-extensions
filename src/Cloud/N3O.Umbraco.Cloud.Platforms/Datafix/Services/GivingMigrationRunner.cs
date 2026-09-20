using N3O.Umbraco.Cloud;
using N3O.Umbraco.Cloud.Platforms.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public class GivingMigrationRunner : IGivingMigrationRunner {
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

    public async Task<GivingMigrationRunRes> MigrateAsync(MigrateGivingReq req,
                                                          CancellationToken cancellationToken) {
        var res = NewRes();

        var plan = _planner.BuildPlan();

        if (plan.Blockers.Count > 0) {
            res.Message = "The plan has " + plan.Blockers.Count + " blockers so nothing was migrated";

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
                                                     cancellationToken);

        res.IconMediaId = placeholders.Icon.Id;
        res.ImageMediaId = placeholders.Image.Id;
        res.HeroImageMediaId = placeholders.HeroImage.Id;

        var limit = req.Limit ?? GivingMigrationConstants.DefaultMigrateLimit;
        var ledger = new List<GivingMigrationLedgerEntry>();
        var items = new List<GivingMigrationRunItemRes>();

        foreach (var campaign in plan.Campaigns
                                     .Where(x => x.Status == GivingMigrationConstants.EntryStatuses.Planned)
                                     .Take(limit)) {
            items.Add(_writer.CreateCampaign(campaign, containerId.Value, placeholders, ledger));
        }

        res.AlreadyMigrated =
            plan.Campaigns.Count(x => x.Status == GivingMigrationConstants.EntryStatuses.AlreadyMigrated);

        if (req.IncludeCrossSells) {
            items.AddRange(MigrateCrossSells(plan, placeholders, ledger, res));
        }

        // The ledger is written even on a partial run so a re-run knows exactly what already exists.
        _store.AppendLedger(ledger);

        res.Items = items;
        res.Attempted = items.Count;
        res.Created = items.Count(x => x.Outcome == GivingMigrationConstants.Outcomes.Created);
        res.Failed = items.Count(x => x.Outcome == GivingMigrationConstants.Outcomes.Failed);
        res.OfferingsCreated = items.Sum(x => x.OfferingsCreated);

        return res;
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
        res.Created = items.Count(x => x.Outcome == GivingMigrationConstants.Outcomes.Published);
        res.Failed = items.Count(x => x.Outcome == GivingMigrationConstants.Outcomes.Failed);
        res.OfferingsCreated = items.Sum(x => x.OfferingsCreated);

        return res;
    }

    private IReadOnlyList<GivingMigrationRunItemRes> MigrateCrossSells(GivingMigrationPlanRes plan,
                                                                       GivingPlaceholders placeholders,
                                                                       ICollection<GivingMigrationLedgerEntry> ledger,
                                                                       GivingMigrationRunRes res) {
        var planned = plan.CrossSells
                          .Where(x => x.Status == GivingMigrationConstants.EntryStatuses.Planned)
                          .ToList();

        if (planned.Count == 0) {
            return [];
        }

        var containerId = _writer.EnsureCrossSellsContainerId();

        if (containerId == null) {
            res.Message = "The cross sells container could not be created so cross sells were not migrated";

            return [];
        }

        var items = planned.Select(x => _writer.CreateCrossSell(x, containerId.Value, placeholders, ledger))
                           .ToList();

        res.CrossSellsCreated = items.Count(x => x.Outcome == GivingMigrationConstants.Outcomes.Created);

        return items;
    }

    private GivingMigrationRunRes NewRes() {
        var res = new GivingMigrationRunRes();
        res.SubscriptionId = _subscriptionAccessor.GetSubscription().Id.ToString();

        return res;
    }
}
