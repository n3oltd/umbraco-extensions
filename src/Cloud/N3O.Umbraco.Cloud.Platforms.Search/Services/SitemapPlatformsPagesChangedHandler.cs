using N3O.Umbraco.Cloud.Extensions;
using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Scheduler;
using N3O.Umbraco.Scheduler.Extensions;
using N3O.Umbraco.Search.Commands;
using NodaTime;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud.Platforms.Search;

public class SitemapPlatformsPagesChangedHandler : IPlatformsPagesChangedHandler {
    private static readonly Duration RegenerateDelay = Duration.FromSeconds(60);

    private readonly IBackgroundJob _backgroundJob;
    private readonly ICdnClient _cdnClient;

    public SitemapPlatformsPagesChangedHandler(IBackgroundJob backgroundJob, ICdnClient cdnClient) {
        _backgroundJob = backgroundJob;
        _cdnClient = cdnClient;
    }

    public Task HandleAsync(CancellationToken cancellationToken = default) {
        _cdnClient.EvictSubscriptionContent(SubscriptionFiles.Campaigns);

        _backgroundJob.ScheduleCommand<GenerateSitemapCommand>(RegenerateDelay);

        return Task.CompletedTask;
    }
}
