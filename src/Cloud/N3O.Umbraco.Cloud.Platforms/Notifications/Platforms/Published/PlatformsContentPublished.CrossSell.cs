using Microsoft.Extensions.Logging;
using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Content;
using N3O.Umbraco.Scheduler;
using System;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class CrossSellPublished : CloudContentPublished {
    private readonly IContentTypeService _contentTypeService;
    private readonly Lazy<IContentLocator> _contentLocator;
    private readonly IUmbracoMapper _mapper;

    public CrossSellPublished(ICloudUrl cloudUrl,
                              IBackgroundJob backgroundJob,
                              IContentTypeService contentTypeService,
                              Lazy<IContentLocator> contentLocator,
                              IUmbracoMapper mapper,
                              ILogger<CrossSellPublished> logger)
        : base(cloudUrl, backgroundJob, logger) {
        _contentTypeService = contentTypeService;
        _contentLocator = contentLocator;
        _mapper = mapper;
    }

    protected override bool CanProcess(IContent content) {
        return content.IsCrossSell(_contentTypeService);
    }

    protected override Task<object> GetBodyAsync(IContent content) {
        var crossSell = _contentLocator.Value.ById<CrossSellContent>(content.Key);
        var crossSellReq = _mapper.Map<CrossSellContent, CrossSellWebhookBodyReq>(crossSell);

        return Task.FromResult<object>(crossSellReq);
    }

    protected override string HookId => PlatformsConstants.Webhooks.HookIds.CrossSells;
}
