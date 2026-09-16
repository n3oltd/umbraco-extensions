using N3O.Umbraco.Cloud.Platforms.Commands;
using N3O.Umbraco.Cloud.Platforms.Models;
using N3O.Umbraco.Mediator;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud.Platforms.Handlers;

public class MigrateGivingHandler : IRequestHandler<MigrateGivingCommand, MigrateGivingReq, GivingMigrationRunRes> {
    private readonly IGivingMigrationPlanner _planner;
    private readonly IGivingMigrationWriter _writer;
    private readonly ILegacyGivingTreeLock _treeLock;
    private readonly ISubscriptionAccessor _subscriptionAccessor;

    public MigrateGivingHandler(IGivingMigrationPlanner planner,
                                IGivingMigrationWriter writer,
                                ILegacyGivingTreeLock treeLock,
                                ISubscriptionAccessor subscriptionAccessor) {
        _planner = planner;
        _writer = writer;
        _treeLock = treeLock;
        _subscriptionAccessor = subscriptionAccessor;
    }

    public Task<GivingMigrationRunRes> Handle(MigrateGivingCommand req, CancellationToken cancellationToken) {
        var model = req.Model;

        var res = new GivingMigrationRunRes();
        res.SubscriptionId = _subscriptionAccessor.GetSubscription().Id.ToString();

        var containerId = _writer.GetCampaignsContainerId();

        if (containerId == null) {
            res.Lock = _treeLock.GetStatus();
            res.Message = "A single campaigns container of type " +
                          GivingMigrationConstants.Platforms.CampaignsAlias +
                          " could not be found, so no campaigns can be created";

            return Task.FromResult(res);
        }

        var placeholders = _writer.BuildPlaceholders(model.PlaceholderMediaId.Value, model.AnalyticsTag);

        if (placeholders == null) {
            res.Lock = _treeLock.GetStatus();
            res.Message = "The placeholder media item could not be resolved to an image file";

            return Task.FromResult(res);
        }

        res.Lock = _treeLock.Lock();

        var plan = _planner.BuildPlan();

        res.SkippedAlreadyMigrated =
            plan.Campaigns.Count(x => x.Status == GivingMigrationConstants.EntryStatuses.AlreadyMigrated);
        res.SkippedBlocked =
            plan.Campaigns.Count(x => x.Status == GivingMigrationConstants.EntryStatuses.Blocked);

        var planned = plan.Campaigns
                          .Where(x => x.Status == GivingMigrationConstants.EntryStatuses.Planned)
                          .ToList();

        var limit = model.Limit ?? GivingMigrationConstants.DefaultMigrateLimit;
        var batch = planned.Take(limit).ToList();
        var items = new List<GivingMigrationRunItemRes>();

        foreach (var campaign in batch) {
            items.Add(_writer.CreateCampaign(campaign, containerId.Value, placeholders));
        }

        res.Items = items;
        res.Attempted = items.Count;
        res.CampaignsCreated = items.Count(x => x.Outcome == GivingMigrationConstants.Outcomes.Created);
        res.OfferingsCreated = items.Sum(x => x.OfferingsCreated);
        res.Failed = items.Count(x => x.Outcome == GivingMigrationConstants.Outcomes.Failed);
        res.RemainingPlanned = planned.Count - res.CampaignsCreated;

        return Task.FromResult(res);
    }
}
