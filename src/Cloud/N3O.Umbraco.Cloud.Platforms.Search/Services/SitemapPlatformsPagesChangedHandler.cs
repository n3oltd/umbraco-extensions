using N3O.Umbraco.Cloud.Extensions;
using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Cloud.Platforms.Models;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Scheduler;
using N3O.Umbraco.Scheduler.Extensions;
using N3O.Umbraco.Search.Commands;
using NodaTime;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static N3O.Umbraco.Cloud.Platforms.PlatformsConstants.Webhooks;

namespace N3O.Umbraco.Cloud.Platforms.Search;

public class SitemapPlatformsPagesChangedHandler : IPlatformsPagesChangedHandler {
    // The campaigns file is built from campaign documents alone
    private static readonly IReadOnlyList<string> CampaignEventTypes = [
        EventTypes.Campaign.Created,
        EventTypes.Campaign.Updated,
        EventTypes.Offering.Created,
        EventTypes.Offering.Updated
    ];

    private static readonly Duration RegenerateDelay = Duration.FromMinutes(1);

    private readonly IBackgroundJob _backgroundJob;
    private readonly ICdnClient _cdnClient;

    public SitemapPlatformsPagesChangedHandler(IBackgroundJob backgroundJob, ICdnClient cdnClient) {
        _backgroundJob = backgroundJob;
        _cdnClient = cdnClient;
    }

    public Task HandleAsync(string eventType,
                            WebhookPlatformsPage page,
                            CancellationToken cancellationToken = default) {
        if (!CampaignEventTypes.Contains(eventType, true)) {
            return Task.CompletedTask;
        }

        _cdnClient.EvictSubscriptionContent(SubscriptionFiles.Campaigns);

        _backgroundJob.ScheduleCommand<GenerateSitemapCommand>(RegenerateDelay);

        return Task.CompletedTask;
    }
}
