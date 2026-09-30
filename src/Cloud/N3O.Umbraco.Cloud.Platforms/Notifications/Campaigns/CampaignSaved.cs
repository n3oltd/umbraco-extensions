using Microsoft.Extensions.Logging;
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

public class CampaignSaved : INotificationAsyncHandler<ContentSavedNotification> {
    private readonly Lazy<IContentEditor> _contentEditor;
    private readonly IContentHelper _contentHelper;
    private readonly IContentTypeService _contentTypeService;
    private readonly ILogger<CampaignSaved> _logger;

    public CampaignSaved(Lazy<IContentEditor> contentEditor,
                         IContentHelper contentHelper,
                         IContentTypeService contentTypeService,
                         ILogger<CampaignSaved> logger) {
        _contentEditor = contentEditor;
        _contentHelper = contentHelper;
        _contentTypeService = contentTypeService;
        _logger = logger;
    }

    public Task HandleAsync(ContentSavedNotification notification, CancellationToken cancellationToken) {
        var campaigns = notification.SavedEntities.Where(x => x.IsCampaign(_contentTypeService)).ToList();

        if (campaigns.HasAny()) {
            var crowdfundingCampaigns = _contentHelper.GetCrowdfundingCampaigns();

            foreach (var campaign in campaigns) {
                SyncCrowdfundingCampaignNames(notification, crowdfundingCampaigns, campaign);
            }
        }

        return Task.CompletedTask;
    }

    private void SyncCrowdfundingCampaignName(IContent crowdfundingCampaign, IContent campaign) {
        if (crowdfundingCampaign.GetCampaignKey() != campaign.Key ||
            crowdfundingCampaign.Name.EqualsInvariant(campaign.Name)) {
            return;
        }

        var contentPublisher = _contentEditor.Value.ForExisting(crowdfundingCampaign.Key);

        contentPublisher.SetName(campaign.Name);

        var saved = crowdfundingCampaign.Published && !crowdfundingCampaign.Edited
                        ? contentPublisher.SaveAndPublish().Success
                        : contentPublisher.SaveUnpublished().Success;

        if (!saved) {
            throw new Exception($"Saving crowdfunding campaign {crowdfundingCampaign.Key} did not succeed");
        }
    }

    private void SyncCrowdfundingCampaignNames(ContentSavedNotification notification,
                                               IEnumerable<IContent> crowdfundingCampaigns,
                                               IContent campaign) {
        foreach (var crowdfundingCampaign in crowdfundingCampaigns) {
            try {
                SyncCrowdfundingCampaignName(crowdfundingCampaign, campaign);
            } catch (Exception ex) {
                _logger.LogError(ex,
                                 "Error renaming crowdfunding campaign {CrowdfundingCampaignKey} from {CampaignKey}",
                                 crowdfundingCampaign.Key,
                                 campaign.Key);

                var message = $"The crowdfunding campaign {crowdfundingCampaign.Name.Quote()} could not be renamed " +
                              "to match this campaign";

                notification.Messages.Add(new EventMessage("Warning", message, EventMessageType.Warning));
            }
        }
    }
}
