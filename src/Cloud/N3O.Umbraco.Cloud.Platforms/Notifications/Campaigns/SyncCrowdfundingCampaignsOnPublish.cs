using Microsoft.Extensions.Logging;
using N3O.Umbraco.Attributes;
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

[SkipDuringSync]
public class SyncCrowdfundingCampaignsOnPublish : INotificationAsyncHandler<ContentPublishedNotification> {
    private readonly ICrowdfundingCampaignContentCopier _contentCopier;
    private readonly IContentHelper _contentHelper;
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly ILogger<SyncCrowdfundingCampaignsOnPublish> _logger;

    public SyncCrowdfundingCampaignsOnPublish(ICrowdfundingCampaignContentCopier contentCopier,
                                              IContentHelper contentHelper,
                                              IContentService contentService,
                                              IContentTypeService contentTypeService,
                                              ILogger<SyncCrowdfundingCampaignsOnPublish> logger) {
        _contentCopier = contentCopier;
        _contentHelper = contentHelper;
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _logger = logger;
    }

    public Task HandleAsync(ContentPublishedNotification notification, CancellationToken cancellationToken) {
        var campaigns = notification.PublishedEntities.Where(x => x.IsCampaign(_contentTypeService)).ToList();

        if (campaigns.HasAny()) {
            var crowdfundingCampaigns = _contentHelper.GetCrowdfundingCampaigns();

            foreach (var campaign in campaigns) {
                UpdateCrowdfundingCampaigns(notification, crowdfundingCampaigns, campaign);
            }
        }

        return Task.CompletedTask;
    }

    private void UpdateCrowdfundingCampaign(IContent crowdfundingCampaign, IContent campaign) {
        if (!_contentCopier.UpdateFromCampaign(crowdfundingCampaign, campaign)) {
            return;
        }

        if (crowdfundingCampaign.Published && !crowdfundingCampaign.Edited) {
            _contentService.SaveAndPublish(crowdfundingCampaign);
        } else {
            _contentService.Save(crowdfundingCampaign);
        }
    }

    private void UpdateCrowdfundingCampaigns(ContentPublishedNotification notification,
                                             IEnumerable<IContent> crowdfundingCampaigns,
                                             IContent campaign) {
        foreach (var crowdfundingCampaign in crowdfundingCampaigns) {
            if (crowdfundingCampaign.GetCampaignKey() != campaign.Key) {
                continue;
            }

            try {
                UpdateCrowdfundingCampaign(crowdfundingCampaign, campaign);
            } catch (Exception ex) {
                _logger.LogError(ex,
                                 "Error updating crowdfunding campaign {CrowdfundingCampaignKey} from {CampaignKey}",
                                 crowdfundingCampaign.Key,
                                 campaign.Key);

                var message = $"The crowdfunding campaign {crowdfundingCampaign.Name.Quote()} could not be updated " +
                              "from this campaign";

                notification.Messages.Add(new EventMessage("Warning", message, EventMessageType.Warning));
            }
        }
    }
}
