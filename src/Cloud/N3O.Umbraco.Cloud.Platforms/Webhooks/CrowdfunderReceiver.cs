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

[WebhookReceiver(HookIds.Crowdfunder)]
public class CrowdfunderReceiver : WebhookReceiver {
    private readonly ICdnClient _cdnClient;
    private readonly IJsonProvider _jsonProvider;
    private readonly ILogger<CrowdfunderReceiver> _logger;

    public CrowdfunderReceiver(ICdnClient cdnClient,
                               IJsonProvider jsonProvider,
                               ILogger<CrowdfunderReceiver> logger) {
        _cdnClient = cdnClient;
        _jsonProvider = jsonProvider;
        _logger = logger;
    }

    protected override async Task ProcessAsync(WebhookPayload payload, CancellationToken cancellationToken) {
        var webhookCrowdfunder = payload.GetBody<WebhookCrowdfunder>(_jsonProvider);
        var eventType = payload.GetEventType();

        switch (eventType) {
            case EventTypes.Crowdfunder.Created:
            case EventTypes.Crowdfunder.Updated:
                await EvictAsync(webhookCrowdfunder, cancellationToken);

                break;
        }
    }

    private async Task EvictAsync(WebhookCrowdfunder webhookCrowdfunder, CancellationToken cancellationToken) {
        if (webhookCrowdfunder == null || !webhookCrowdfunder.HasValue(x => x.PagePublishedPath)) {
            _logger.LogWarning("Crowdfunder webhook carried no page published path, so nothing was evicted");

            return;
        }

        foreach (var pagePublishedPath in webhookCrowdfunder.OrEmpty(x => x.PagePublishedPathsHistory)) {
            _cdnClient.Evict(pagePublishedPath);
        }

        _cdnClient.Evict(webhookCrowdfunder.PagePublishedPath);

        // The read that follows is served fresh because of the eviction above, and consumes it.
        await EvictMergeModelsAsync(webhookCrowdfunder.PagePublishedPath, cancellationToken);

        // The CDN may not have had the new page yet, so the page is left marked for the next reader.
        _cdnClient.Evict(webhookCrowdfunder.PagePublishedPath);
    }

    private async Task EvictMergeModelsAsync(string pagePublishedPath, CancellationToken cancellationToken) {
        var publishedContentResult = await _cdnClient.DownloadPublishedContentAsync(pagePublishedPath,
                                                                                   cancellationToken);

        if (publishedContentResult.NotFound || publishedContentResult.Error) {
            _logger.LogWarning("Could not read the crowdfunder page at {PagePublishedPath}, so its merge models were not evicted",
                               pagePublishedPath);

            return;
        }

        var content = publishedContentResult.Content;
        var publishedPlatformsPage = _jsonProvider.DeserializeDynamicTo<PublishedPlatformsPage>(content);

        foreach (var mergeModel in publishedPlatformsPage.OrEmpty(x => x.MergeModels)) {
            _cdnClient.Evict(mergeModel.Path);
        }
    }

    public class WebhookCrowdfunder {
        public WebhookCrowdfunder(string pagePublishedPath, IEnumerable<string> pagePublishedPathsHistory) {
            PagePublishedPath = pagePublishedPath;
            PagePublishedPathsHistory = pagePublishedPathsHistory;
        }

        public string PagePublishedPath { get; }
        public IEnumerable<string> PagePublishedPathsHistory { get; }
    }
}
