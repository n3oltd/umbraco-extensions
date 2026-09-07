using Microsoft.Extensions.Logging;
using N3O.Umbraco.Cloud.Platforms.Models;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Json;
using N3O.Umbraco.Webhooks.Attributes;
using N3O.Umbraco.Webhooks.Extensions;
using N3O.Umbraco.Webhooks.Models;
using N3O.Umbraco.Webhooks.Receivers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static N3O.Umbraco.Cloud.Platforms.PlatformsConstants.Webhooks;

namespace N3O.Umbraco.Cloud.Platforms.Webhooks;

[WebhookReceiver(HookIds.PlatformsPages)]
[WebhookReceiver(HookIds.Crowdfunder)]
public class PlatformsPagesReceiver : WebhookReceiver {
    private readonly ICdnClient _cdnClient;
    private readonly IJsonProvider _jsonProvider;
    private readonly ILogger<PlatformsPagesReceiver> _logger;

    public PlatformsPagesReceiver(ICdnClient cdnClient,
                                  IJsonProvider jsonProvider,
                                  ILogger<PlatformsPagesReceiver> logger) {
        _cdnClient = cdnClient;
        _jsonProvider = jsonProvider;
        _logger = logger;
    }

    protected override async Task ProcessAsync(WebhookPayload payload, CancellationToken cancellationToken) {
        var eventType = payload.GetEventType();

        if (!EventTypes.PlatformsPages.Contains(eventType, true)) {
            _logger.LogWarning("Platforms pages webhook carried the unhandled event type {EventType}, so nothing " +
                               "was evicted",
                               eventType);

            return;
        }

        var webhookPage = payload.GetBody<WebhookPlatformsPage>(_jsonProvider);

        await EvictAsync(eventType, webhookPage, cancellationToken);
    }

    private async Task EvictAsync(string eventType,
                                  WebhookPlatformsPage webhookPage,
                                  CancellationToken cancellationToken) {
        if (webhookPage == null || !webhookPage.HasValue(x => x.PagePublishedPath)) {
            _logger.LogWarning("{EventType} webhook carried no page published path, so nothing was evicted", eventType);

            return;
        }

        foreach (var pagePublishedPath in webhookPage.OrEmpty(x => x.PagePublishedPathsHistory)) {
            _cdnClient.Evict(pagePublishedPath);
        }

        _cdnClient.Evict(webhookPage.PagePublishedPath);

        // The read that follows is served fresh because of the eviction above, and consumes it.
        await EvictMergeModelsAsync(webhookPage.PagePublishedPath, cancellationToken);

        // The CDN may not have had the new page yet, so the page is left marked for the next reader.
        _cdnClient.Evict(webhookPage.PagePublishedPath);
    }

    private async Task EvictMergeModelsAsync(string pagePublishedPath, CancellationToken cancellationToken) {
        var publishedContentResult = await _cdnClient.DownloadPublishedContentAsync(pagePublishedPath,
                                                                                   cancellationToken);

        if (publishedContentResult.NotFound || publishedContentResult.Error) {
            _logger.LogWarning("Could not read the page at {PagePublishedPath}, so its merge models were not evicted",
                               pagePublishedPath);

            return;
        }

        var content = publishedContentResult.Content;
        var publishedPlatformsPage = _jsonProvider.DeserializeDynamicTo<PublishedPlatformsPage>(content);

        foreach (var mergeModel in publishedPlatformsPage.OrEmpty(x => x.MergeModels)) {
            _cdnClient.Evict(mergeModel.Path);
        }
    }

    public class WebhookPlatformsPage {
        public WebhookPlatformsPage(string pagePublishedPath, IEnumerable<string> pagePublishedPathsHistory) {
            PagePublishedPath = pagePublishedPath;
            PagePublishedPathsHistory = pagePublishedPathsHistory;
        }

        public string PagePublishedPath { get; }
        public IEnumerable<string> PagePublishedPathsHistory { get; }
    }
}
