using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Cloud.Platforms.Models;
using N3O.Umbraco.Content;
using N3O.Umbraco.Dev;
using N3O.Umbraco.Cloud.Platforms.Commands;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Hosting;
using N3O.Umbraco.Mediator;
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
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IGivingMigrationPlanner _givingMigrationPlanner;
    private readonly IMediator _mediator;
    private readonly ISubscriptionAccessor _subscriptionAccessor;
    private readonly ILogger<PlatformsDevToolsController> _logger;

    public PlatformsDevToolsController(IContentLocator contentLocator,
                                       IUmbracoMapper mapper,
                                       ICloudUrl cloudUrl,
                                       IBackgroundJob backgroundJob,
                                       ILogger<PlatformsDevToolsController> logger,
                                       IContentService contentService,
                                       IWebHostEnvironment webHostEnvironment,
                                       IGivingMigrationPlanner givingMigrationPlanner,
                                       IMediator mediator,
                                       ISubscriptionAccessor subscriptionAccessor) {
        _mediator = mediator;
        _subscriptionAccessor = subscriptionAccessor;
        _contentLocator = contentLocator;
        _mapper = mapper;
        _cloudUrl = cloudUrl;
        _backgroundJob = backgroundJob;
        _logger = logger;
        _contentService = contentService;
        _webHostEnvironment = webHostEnvironment;
        _givingMigrationPlanner = givingMigrationPlanner;
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

    [HttpGet("giving/migration/environment")]
    public Task<ActionResult<GivingMigrationEnvironmentRes>> GetGivingMigrationEnvironment() {
        var campaignsUrl = _cloudUrl.ForWebhook(CampaignsWebhookId);
        var offeringsUrl = _cloudUrl.ForWebhook(OfferingsWebhookId);
        var host = GetHost(campaignsUrl);

        var res = new GivingMigrationEnvironmentRes();
        res.EnvironmentName = _webHostEnvironment.EnvironmentName;
        res.IsDevelopment = _webHostEnvironment.IsDevelopment();
        res.EnableLiveTesting = DevFlags.IsSet(DevFlags.EnableLiveTesting);
        res.CampaignsWebhookUrl = campaignsUrl;
        res.OfferingsWebhookUrl = offeringsUrl;
        res.WebhookHost = host;
        res.IsLiveCloud = string.Equals(host,
                                        GivingMigrationConstants.LiveCloudHost,
                                        StringComparison.OrdinalIgnoreCase);
        res.SubscriptionId = _subscriptionAccessor.GetSubscription().Id.ToString();

        return Task.FromResult<ActionResult<GivingMigrationEnvironmentRes>>(Ok(res));
    }

    [HttpGet("giving/migration/plan")]
    public Task<ActionResult<GivingMigrationPlanRes>> GetGivingMigrationPlan() {
        var res = _givingMigrationPlanner.BuildPlan();

        return Task.FromResult<ActionResult<GivingMigrationPlanRes>>(Ok(res));
    }

    [HttpPost("giving/migration/migrate")]
    public async Task<ActionResult<GivingMigrationRunRes>> MigrateGiving([FromQuery] string expectSubscriptionId,
                                                                         [FromQuery] Guid? placeholderMediaId,
                                                                         [FromQuery] string analyticsTag,
                                                                         [FromQuery] int? limit) {
        var mismatch = GetSubscriptionMismatch(expectSubscriptionId);

        if (mismatch != null) {
            return Conflict(mismatch);
        }

        if (placeholderMediaId == null) {
            return BadRequest("A placeholderMediaId must be supplied to populate the mandatory image properties");
        }

        if (!analyticsTag.HasValue()) {
            return BadRequest("An analyticsTag must be supplied to populate the mandatory tags property");
        }

        var model = new MigrateGivingReq();
        model.ExpectSubscriptionId = expectSubscriptionId;
        model.PlaceholderMediaId = placeholderMediaId;
        model.AnalyticsTag = analyticsTag;
        model.Limit = limit;

        var res = await _mediator.SendAsync<MigrateGivingCommand, MigrateGivingReq, GivingMigrationRunRes>(model);

        if (res.Message.HasValue() && res.Attempted == 0) {
            return BadRequest(res);
        }

        return Ok(res);
    }

    [HttpPost("giving/migration/complete")]
    public async Task<ActionResult<GivingMigrationCompleteRes>> CompleteGivingMigration(
        [FromQuery] string expectSubscriptionId) {
        var mismatch = GetSubscriptionMismatch(expectSubscriptionId);

        if (mismatch != null) {
            return Conflict(mismatch);
        }

        var model = new CompleteGivingMigrationReq();
        model.ExpectSubscriptionId = expectSubscriptionId;

        var res = await _mediator
                        .SendAsync<CompleteGivingMigrationCommand,
                                   CompleteGivingMigrationReq,
                                   GivingMigrationCompleteRes>(model);

        if (res.Message.HasValue()) {
            return BadRequest(res);
        }

        return Ok(res);
    }

    private string GetSubscriptionMismatch(string expectSubscriptionId) {
        var subscriptionId = _subscriptionAccessor.GetSubscription().Id.ToString();

        if (string.Equals(expectSubscriptionId, subscriptionId, StringComparison.OrdinalIgnoreCase)) {
            return null;
        }

        return "This site runs subscription " +
               subscriptionId +
               " but expectSubscriptionId was " +
               (expectSubscriptionId.HasValue() ? expectSubscriptionId : "not supplied");
    }

    private static string GetHost(string url) {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : null;
    }
}
