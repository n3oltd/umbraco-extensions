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
    private const string CampaignNotFound = "Campaign not found";
    private const string CheckUnavailable = "Could not check whether this campaign allows crowdfunding, please try " +
                                            "again. If this keeps happening, contact support";
    private const string NotPermitted = "This campaign cannot be used for crowdfunding";

    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(3);

    private readonly Lazy<ClientFactory<CrowdfundingClient>> _clientFactory;
    private readonly ICrowdfundingCampaignContentCopier _contentCopier;
    private readonly IContentHelper _contentHelper;
    private readonly IContentService _contentService;
    private readonly ILogger<CrowdfundingCampaignSaving> _logger;

    public CrowdfundingCampaignSaving(Lazy<ClientFactory<CrowdfundingClient>> clientFactory,
                                      ICrowdfundingCampaignContentCopier contentCopier,
                                      IContentHelper contentHelper,
                                      IContentService contentService,
                                      ILogger<CrowdfundingCampaignSaving> logger) {
        _clientFactory = clientFactory;
        _contentCopier = contentCopier;
        _contentHelper = contentHelper;
        _contentService = contentService;
        _logger = logger;
    }

    public async Task HandleAsync(ContentSavingNotification notification, CancellationToken cancellationToken) {
        foreach (var content in notification.SavedEntities) {
            if (!content.IsCrowdfundingCampaign()) {
                continue;
            }

            var creating = !content.HasIdentity;
            var campaignKey = content.GetCampaignKey();

            // Cancelling stops the whole notification, not the one entity
            if (campaignKey == null) {
                if (creating) {
                    notification.CancelWithError("A campaign must be selected");

                    return;
                }

                continue;
            }

            var blockers = await GetCrowdfundingBlockersAsync(campaignKey.Value, cancellationToken);

            if (blockers.HasAny()) {
                foreach (var blocker in blockers) {
                    notification.CancelWithError(blocker);
                }

                return;
            }

            if (AnotherCrowdfundingCampaignExistsFor(content, campaignKey.Value)) {
                notification.CancelWithError("This campaign already has a crowdfunding campaign");

                return;
            }

            var campaign = _contentService.GetById(campaignKey.Value);

            if (campaign == null) {
                notification.CancelWithError("The selected campaign no longer exists");

                return;
            }

            content.Name = campaign.Name;

            if (creating) {
                _contentCopier.CopyFromCampaign(content, campaign);
            }
        }
    }

    private async Task<IReadOnlyCollection<string>> GetCrowdfundingBlockersAsync(Guid campaignKey,
                                                                                CancellationToken cancellationToken) {
        CanEnableCrowdfundingCampaignRes res;

        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)) {
            timeout.CancelAfter(CheckTimeout);

            try {
                var client = _clientFactory.Value.Create(CloudApiTypes.Engage, bearerToken: null);

                res = await client.InvokeAsync(x => x.CanEnableCrowdfundingCampaignAsync(campaignKey.ToString(),
                                                                                         timeout.Token));
            } catch (Exception ex) when (IsNotFound(ex)) {
                _logger.LogWarning(ex,
                                   "The crowdfunding service has no campaign {CampaignKey}",
                                   campaignKey);

                return [CampaignNotFound];
            } catch (Exception ex) {
                _logger.LogError(ex,
                                 "Error checking whether campaign {CampaignKey} allows crowdfunding: {Error}",
                                 campaignKey,
                                 ex.Message);

                return [CheckUnavailable];
            }
        }

        if (res?.Permitted == null) {
            _logger.LogError("The crowdfunding service did not say whether campaign {CampaignKey} allows crowdfunding",
                             campaignKey);

            return [CheckUnavailable];
        }

        if (res.Permitted.Value) {
            return [];
        }

        var reasons = res.Reasons.OrEmpty().Select(x => x?.Name).Where(x => x.HasValue()).ToList();

        return reasons.HasAny() ? reasons : [NotPermitted];
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
