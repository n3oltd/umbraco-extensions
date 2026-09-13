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

    public CampaignSaved(Lazy<IContentEditor> contentEditor,
                         IContentHelper contentHelper,
                         IContentTypeService contentTypeService) {
        _contentEditor = contentEditor;
        _contentHelper = contentHelper;
        _contentTypeService = contentTypeService;
    }

    public Task HandleAsync(ContentSavedNotification notification, CancellationToken cancellationToken) {
        var campaigns = notification.SavedEntities.Where(x => x.IsCampaign(_contentTypeService)).ToList();

        if (campaigns.HasAny()) {
            var crowdfundingCampaigns = _contentHelper.GetCrowdfundingCampaigns();

            foreach (var campaign in campaigns) {
                SyncCrowdfundingCampaignNames(crowdfundingCampaigns, campaign);
            }
        }

        return Task.CompletedTask;
    }

    private void SyncCrowdfundingCampaignNames(IEnumerable<IContent> crowdfundingCampaigns, IContent campaign) {
        foreach (var crowdfundingCampaign in crowdfundingCampaigns) {
            if (crowdfundingCampaign.GetCampaignKey() != campaign.Key ||
                crowdfundingCampaign.Name.EqualsInvariant(campaign.Name)) {
                continue;
            }

            var contentPublisher = _contentEditor.Value.ForExisting(crowdfundingCampaign.Key);

            contentPublisher.SetName(campaign.Name);

            if (crowdfundingCampaign.Published && !crowdfundingCampaign.Edited) {
                contentPublisher.SaveAndPublish();
            } else {
                contentPublisher.SaveUnpublished();
            }
        }
    }
}
