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

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class QurbaniSeasonPublished : CloudContentPublished {
    private readonly Lazy<IContentLocator> _contentLocator;
    private readonly IUmbracoMapper _mapper;

    public QurbaniSeasonPublished(ICloudUrl cloudUrl,
                                  IBackgroundJob backgroundJob,
                                  Lazy<IContentLocator> contentLocator,
                                  IUmbracoMapper mapper,
                                  ILogger<CloudContentPublished> logger)
        : base(cloudUrl, backgroundJob, logger) {
        _contentLocator = contentLocator;
        _mapper = mapper;
    }
    
    protected override bool CanProcess(IContent content) {
        return content.IsQurbaniSeasonContent() || content.IsQurbaniSeasonCategoryContent();
    }

    protected override Task<object> GetBodyAsync(IContent content) {
        var seasonContent = _contentLocator.Value.Single<QurbaniSeasonContent>();

        var settingsReq = _mapper.Map<QurbaniSeasonContent, QurbaniSeasonWebhookBodyReq>(seasonContent);

        return Task.FromResult<object>(settingsReq);
    }

    protected override string HookId => PlatformsConstants.Webhooks.HookIds.QurbaniSeason;
}
