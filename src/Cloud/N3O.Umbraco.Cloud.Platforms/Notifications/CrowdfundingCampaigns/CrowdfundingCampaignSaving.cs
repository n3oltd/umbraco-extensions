using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.Cloud.Exceptions;
using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class CrowdfundingCampaignSaving : INotificationAsyncHandler<ContentSavingNotification> {
    private const string ServicePath = "eu1/api/crowdfunding";
    private const string CampaignNotFound = "Campaign not found";
    private const string CheckUnavailable = "Could not check whether this campaign allows crowdfunding, please try again";
    private const string NotPermitted = "This campaign cannot be used for crowdfunding";

    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(10);

    private readonly IContentHelper _contentHelper;
    private readonly IContentService _contentService;
    private readonly Lazy<ClientFactory<CrowdfundingClient>> _clientFactory;
    private readonly ILogger<CrowdfundingCampaignSaving> _logger;

    public CrowdfundingCampaignSaving(IContentHelper contentHelper,
                                      IContentService contentService,
                                      Lazy<ClientFactory<CrowdfundingClient>> clientFactory,
                                      ILogger<CrowdfundingCampaignSaving> logger) {
        _contentHelper = contentHelper;
        _contentService = contentService;
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task HandleAsync(ContentSavingNotification notification, CancellationToken cancellationToken) {
        foreach (var content in notification.SavedEntities) {
            if (!content.IsCrowdfundingCampaign()) {
                continue;
            }

            var campaignKey = content.GetCampaignKey();

            if (campaignKey == null) {
                continue;
            }

            var blockers = await GetCrowdfundingBlockersAsync(campaignKey.Value, cancellationToken);

            if (blockers.Any()) {
                foreach (var blocker in blockers) {
                    notification.CancelWithError(blocker);
                }

                continue;
            }

            if (AnotherCrowdfundingCampaignExistsFor(content, campaignKey.Value)) {
                notification.CancelWithError("This campaign already has a crowdfunding campaign");

                continue;
            }

            var campaign = _contentService.GetById(campaignKey.Value);

            if (campaign != null) {
                content.Name = campaign.Name;
            }
        }
    }

    private async Task<IReadOnlyCollection<string>> GetCrowdfundingBlockersAsync(Guid campaignKey,
                                                                                CancellationToken cancellationToken) {
        CanEnableCrowdfundingCampaignRes res;

        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)) {
            timeout.CancelAfter(CheckTimeout);

            try {
                var client = _clientFactory.Value.Create(CloudApiTypes.Engage, ServicePath, bearerToken: null);

                res = await client.InvokeAsync(x => x.CanEnableCrowdfundingCampaignAsync(campaignKey.ToString(),
                                                                                         timeout.Token));
            } catch (Exception ex) when (IsNotFound(ex)) {
                return new[] { CampaignNotFound };
            } catch (Exception ex) {
                _logger.LogError(ex,
                                 "Error checking whether campaign {CampaignKey} allows crowdfunding: {Error}",
                                 campaignKey,
                                 ex.Message);

                return new[] { CheckUnavailable };
            }
        }

        if (res?.Permitted == true) {
            return Array.Empty<string>();
        }

        var reasons = res?.Reasons.OrEmpty().Select(x => x.Name).Where(x => x.HasValue()).ToList();

        return reasons.OrEmpty().Any() ? reasons : new[] { NotPermitted };
    }

    private bool AnotherCrowdfundingCampaignExistsFor(IContent crowdfundingCampaign, Guid campaignKey) {
        return _contentHelper.GetCrowdfundingCampaigns()
                             .Any(x => x.Key != crowdfundingCampaign.Key && x.GetCampaignKey() == campaignKey);
    }

    private static bool IsNotFound(Exception exception) {
        return GetStatusCode(exception) == StatusCodes.Status404NotFound;
    }

    private static int? GetStatusCode(Exception exception) {
        return exception switch {
            ApiException apiException => apiException.StatusCode,
            CloudApiException cloudApiException => GetStatusCode(cloudApiException.Exception),
            _ => null
        };
    }
}
