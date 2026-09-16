using N3O.Umbraco.Cloud.Platforms.Commands;
using N3O.Umbraco.Cloud.Platforms.Models;
using N3O.Umbraco.Mediator;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud.Platforms.Handlers;

public class CompleteGivingMigrationHandler :
    IRequestHandler<CompleteGivingMigrationCommand, CompleteGivingMigrationReq, GivingMigrationCompleteRes> {
    private readonly IGivingMigrationPlanner _planner;
    private readonly IGivingMigrationWriter _writer;
    private readonly ILegacyGivingTreeLock _treeLock;
    private readonly ISubscriptionAccessor _subscriptionAccessor;

    public CompleteGivingMigrationHandler(IGivingMigrationPlanner planner,
                                          IGivingMigrationWriter writer,
                                          ILegacyGivingTreeLock treeLock,
                                          ISubscriptionAccessor subscriptionAccessor) {
        _planner = planner;
        _writer = writer;
        _treeLock = treeLock;
        _subscriptionAccessor = subscriptionAccessor;
    }

    public Task<GivingMigrationCompleteRes> Handle(CompleteGivingMigrationCommand req,
                                                   CancellationToken cancellationToken) {
        var res = new GivingMigrationCompleteRes();
        res.SubscriptionId = _subscriptionAccessor.GetSubscription().Id.ToString();
        res.Lock = _treeLock.GetStatus();

        var plan = _planner.BuildPlan();

        var migrated = plan.Campaigns
                           .Where(x => x.Status == GivingMigrationConstants.EntryStatuses.AlreadyMigrated)
                           .ToList();

        if (!plan.Summary.Ready) {
            res.Message = "The migration is not ready to be completed: " +
                          plan.Summary.BlockedCampaigns +
                          " blocked campaigns, " +
                          plan.Summary.PlannedCampaigns +
                          " campaigns still to migrate and " +
                          plan.Blockers.Count +
                          " blockers";

            return Task.FromResult(res);
        }

        var items = new List<GivingMigrationRunItemRes>();

        foreach (var campaign in migrated) {
            items.Add(_writer.PublishOfferings(campaign));
        }

        res.Items = items;
        res.CampaignsVerified = migrated.Count;
        res.OfferingsPublished = items.Sum(x => x.OfferingsCreated);
        res.OfferingsFailed = items.Count(x => x.Outcome == GivingMigrationConstants.Outcomes.Failed);

        return Task.FromResult(res);
    }
}
