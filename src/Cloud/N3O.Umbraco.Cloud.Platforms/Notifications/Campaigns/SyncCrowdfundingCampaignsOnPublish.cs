using N3O.Umbraco.Attributes;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
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

    public SyncCrowdfundingCampaignsOnPublish(ICrowdfundingCampaignContentCopier contentCopier,
                                              IContentHelper contentHelper,
                                              IContentService contentService,
                                              IContentTypeService contentTypeService) {
        _contentCopier = contentCopier;
        _contentHelper = contentHelper;
        _contentService = contentService;
        _contentTypeService = contentTypeService;
    }

    public Task HandleAsync(ContentPublishedNotification notification, CancellationToken cancellationToken) {
        var campaigns = notification.PublishedEntities.Where(x => x.IsCampaign(_contentTypeService)).ToList();

        if (campaigns.HasAny()) {
            var crowdfundingCampaigns = _contentHelper.GetCrowdfundingCampaigns();

            foreach (var campaign in campaigns) {
                UpdateCrowdfundingCampaigns(crowdfundingCampaigns, campaign);
            }
        }

        return Task.CompletedTask;
    }

    private void UpdateCrowdfundingCampaigns(IEnumerable<IContent> crowdfundingCampaigns, IContent campaign) {
        foreach (var crowdfundingCampaign in crowdfundingCampaigns) {
            if (crowdfundingCampaign.GetCampaignKey() != campaign.Key ||
                !_contentCopier.UpdateFromCampaign(crowdfundingCampaign, campaign)) {
                continue;
            }

            if (crowdfundingCampaign.Published && !crowdfundingCampaign.Edited) {
                _contentService.SaveAndPublish(crowdfundingCampaign);
            } else {
                _contentService.Save(crowdfundingCampaign);
            }
        }
    }
}
