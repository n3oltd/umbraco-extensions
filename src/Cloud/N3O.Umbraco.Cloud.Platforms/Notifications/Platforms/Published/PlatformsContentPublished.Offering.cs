using Microsoft.Extensions.Logging;
using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Cloud.Platforms.Lookups;
using N3O.Umbraco.Cloud.Platforms.Models;
using N3O.Umbraco.Content;
using N3O.Umbraco.Scheduler;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class OfferingPublished : CloudContentPublished {
    private readonly IContentTypeService _contentTypeService;
    private readonly Lazy<IContentLocator> _contentLocator;
    private readonly IUmbracoMapper _mapper;
    private readonly IEnumerable<IPlatformsPageContentPublisher> _platformsPageContentPublishers;

    public OfferingPublished(ICloudUrl cloudUrl,
                             IBackgroundJob backgroundJob,
                             IContentTypeService contentTypeService,
                             Lazy<IContentLocator> contentLocator,
                             IUmbracoMapper mapper,
                             ILogger<CloudContentPublished> logger,
                             IEnumerable<IPlatformsPageContentPublisher> platformsPageContentPublishers)
        : base(cloudUrl, backgroundJob, logger) {
        _contentTypeService = contentTypeService;
        _contentLocator = contentLocator;
        _mapper = mapper;
        _platformsPageContentPublishers = platformsPageContentPublishers;
    }
    
    protected override bool CanProcess(IContent content) {
        return content.IsOffering(_contentTypeService);
    }
    
    protected override Task<object> GetBodyAsync(IContent content) {
        var offering = _contentLocator.Value.ById<OfferingContent>(content.Key);

        var platformsPageContentPublisher = _platformsPageContentPublishers.GetPublisher(PlatformsSchemas.OfferingPage);

        var offeringReq = _mapper.Map<OfferingContent, OfferingWebhookBodyReq>(offering, ctx => {
            ctx.Items[UpdateOfferingReqMapping.PageContentContext] = platformsPageContentPublisher.GetContentProperties(offering.Content());
        });

        return Task.FromResult<object>(offeringReq);
    }

    protected override string HookId => PlatformsConstants.Webhooks.HookIds.Offerings;
}