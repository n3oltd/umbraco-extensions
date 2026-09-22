using Microsoft.Extensions.Logging;
using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Cloud.Platforms.Lookups;
using N3O.Umbraco.Cloud.Platforms.Models;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Scheduler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Models;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class CrowdfundingCampaignPublished : CloudContentPublished {
    private readonly Lazy<IContentLocator> _contentLocator;
    private readonly ILogger<CrowdfundingCampaignPublished> _logger;
    private readonly IUmbracoMapper _mapper;
    private readonly IReadOnlyList<IPlatformsPageContentPublisher> _platformsPageContentPublishers;

    public CrowdfundingCampaignPublished(ICloudUrl cloudUrl,
                                         IBackgroundJob backgroundJob,
                                         Lazy<IContentLocator> contentLocator,
                                         ILogger<CrowdfundingCampaignPublished> logger,
                                         IUmbracoMapper mapper,
                                         IEnumerable<IPlatformsPageContentPublisher> platformsPageContentPublishers)
        : base(cloudUrl, backgroundJob, logger) {
        _contentLocator = contentLocator;
        _logger = logger;
        _mapper = mapper;
        _platformsPageContentPublishers = platformsPageContentPublishers.ToList();
    }

    protected override bool CanProcess(IContent content) {
        if (!content.IsCrowdfundingCampaign()) {
            return false;
        }

        if (content.GetCampaignKey() == null) {
            _logger.LogWarning("Crowdfunding campaign {Name} ({Key}) has no campaign picked, so it was not sent",
                               content.Name,
                               content.Key);

            return false;
        }

        return true;
    }

    protected override Task<object> GetBodyAsync(IContent content) {
        var crowdfundingCampaign = _contentLocator.Value.ById<CrowdfundingCampaignContent>(content.Key);
        var campaignKey = content.GetCampaignKey().GetValueOrThrow();

        var crowdfunderPagePublisher = _platformsPageContentPublishers.GetPublisher(PlatformsSchemas.CrowdfunderPage);

        var crowdfundingCampaignPagePublisher =
            _platformsPageContentPublishers.SingleOrDefault(x => x.IsPublisherFor(PlatformsSchemas.CrowdfundingCampaignPage));

        var req = _mapper.Map<CrowdfundingCampaignContent, CrowdfundingCampaignWebhookBodyReq>(crowdfundingCampaign, ctx => {
            ctx.Items[CrowdfundingCampaignWebhookBodyReqMapping.CampaignKeyContext] = campaignKey;

            ctx.Items[CrowdfundingCampaignWebhookBodyReqMapping.CrowdfunderPageContentContext] =
                crowdfunderPagePublisher.GetContentProperties(crowdfundingCampaign.Content());

            if (crowdfundingCampaignPagePublisher != null) {
                ctx.Items[CrowdfundingCampaignWebhookBodyReqMapping.CrowdfundingCampaignPageContentContext] =
                    crowdfundingCampaignPagePublisher.GetContentProperties(crowdfundingCampaign.Content());
            }
        });

        return Task.FromResult<object>(req);
    }

    protected override string HookId => PlatformsConstants.Webhooks.HookIds.CrowdfundingCampaigns;
}
