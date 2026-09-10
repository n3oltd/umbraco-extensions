using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Hosting;
using N3O.Umbraco.Scheduler;
using N3O.Umbraco.Scheduler.Extensions;
using N3O.Umbraco.Webhooks.Commands;
using N3O.Umbraco.Webhooks.Models;
using System;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace N3O.Umbraco.Cloud.Platforms.Controllers;

[ApiDocument(PlatformsConstants.DevToolsApiName)]
public class PlatformsDevToolsController : BackofficeAuthorizedApiController {
    private const string CampaignsWebhookId = PlatformsConstants.Webhooks.HookIds.Campaigns;
    private const string OfferingsWebhookId = PlatformsConstants.Webhooks.HookIds.Offerings;

    private readonly IContentLocator _contentLocator;
    private readonly IUmbracoMapper _mapper;
    private readonly ICloudUrl _cloudUrl;
    private readonly IBackgroundJob _backgroundJob;
    private readonly IContentService _contentService;
    private readonly ILogger<PlatformsDevToolsController> _logger;

    public PlatformsDevToolsController(IContentLocator contentLocator,
                                       IUmbracoMapper mapper,
                                       ICloudUrl cloudUrl,
                                       IBackgroundJob backgroundJob,
                                       ILogger<PlatformsDevToolsController> logger,
                                       IContentService contentService) {
        _contentLocator = contentLocator;
        _mapper = mapper;
        _cloudUrl = cloudUrl;
        _backgroundJob = backgroundJob;
        _logger = logger;
        _contentService = contentService;
    }

    [HttpPost("webhooks/resend/campaigns/all")]
    public Task<ActionResult> ResendCampaignsWebhooks() {
        var campaigns = _contentLocator.All(x => x.IsComposedOf(AliasHelper<CampaignContent>.ContentTypeAlias()))
                                       .As<CampaignContent>();

        foreach (var campaign in campaigns) {
            var body = _mapper.Map<CampaignContent, CampaignWebhookBodyReq>(campaign);

            var req = new DispatchWebhookReq();
            req.Body = body;
            req.Url = _cloudUrl.ForWebhook(CampaignsWebhookId);

            _backgroundJob.EnqueueCommand<DispatchWebhookCommand, DispatchWebhookReq>(req, CampaignsWebhookId);
        }

        return Task.FromResult<ActionResult>(Ok());
    }

    [HttpPost("webhooks/resend/offerings/all")]
    public Task<ActionResult> ResendOfferingsWebhooks() {
        var offerings = _contentLocator.All(x => x.IsComposedOf(AliasHelper<OfferingContent>.ContentTypeAlias()))
                                       .As<OfferingContent>();

        foreach (var offering in offerings) {
            try {
                var parent = _contentService.GetParent(offering.Content()
                                                               .Id);

                if (parent?.Published == true) {
                    var body = _mapper.Map<OfferingContent, OfferingWebhookBodyReq>(offering);

                    var req = new DispatchWebhookReq();
                    req.Body = body;
                    req.Url = _cloudUrl.ForWebhook(OfferingsWebhookId);

                    _backgroundJob.EnqueueCommand<DispatchWebhookCommand, DispatchWebhookReq>(req, OfferingsWebhookId);
                }
            } catch (Exception ex) {
                _logger.LogError(ex,
                                 "There was an error while resending offering with id {offeringId}",
                                 offering.Content().Key.ToString());
            }
        }

        return Task.FromResult<ActionResult>(Ok());
    }
    
    [HttpPost("republish/campaigns")]
    public Task<ActionResult> RepublishAllCampaigns() {
        var campaigns = _contentLocator.All(x => x.IsComposedOf(AliasHelper<CampaignContent>.ContentTypeAlias()))
                                       .As<CampaignContent>();

        foreach (var campaign in campaigns) {
            try {
                var content = _contentService.GetById(campaign.Key);

                _contentService.SaveAndPublish(content);
            } catch (Exception ex) {
                _logger.LogError(ex,
                                 "There was an error publishing campaign with id {campaignId}",
                                 campaign.Content().Key.ToString());
            }
        }

        return Task.FromResult<ActionResult>(Ok());
    }
    
    [HttpPost("republish/offerings")]
    public Task<ActionResult> RepublishAllOfferings() {
        var offerings = _contentLocator.All(x => x.IsComposedOf(AliasHelper<OfferingContent>.ContentTypeAlias()))
                                       .As<OfferingContent>();

        foreach (var offering in offerings) {
            try {
                var content = _contentService.GetById(offering.Key);

                _contentService.SaveAndPublish(content);
            } catch (Exception ex) {
                _logger.LogError(ex,
                                 "There was an error publishing offering with id {offeringId}",
                                 offering.Content().Key.ToString());
            }
        }

        return Task.FromResult<ActionResult>(Ok());
    }
}