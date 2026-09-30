using Microsoft.Extensions.Logging;
using N3O.Umbraco.Cloud.Content.Clients;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Scheduler;
using System;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cloud.Platforms.Notifications;

public class FeedItemPublished : CloudContentPublished {
    private readonly IContentTypeService _contentTypeService;
    private readonly Lazy<IContentLocator> _contentLocator;
    private readonly IUmbracoMapper _mapper;

    public FeedItemPublished(ICloudUrl cloudUrl,
                             IBackgroundJob backgroundJob,
                             IContentTypeService contentTypeService,
                             Lazy<IContentLocator> contentLocator,
                             IUmbracoMapper mapper,
                             ILogger<FeedItemPublished> logger)
        : base(cloudUrl, backgroundJob, logger) {
        _contentTypeService = contentTypeService;
        _contentLocator = contentLocator;
        _mapper = mapper;
    }
    
    protected override bool CanProcess(IContent content) {
        return content.IsFeedItem(_contentTypeService) && IsApprovedOrRejectedOrArchivedFolder(content);
    }

    protected override Task<object> GetBodyAsync(IContent content) {
        var feedItem = _contentLocator.Value.ById<IPublishedContent>(content.Key);

        var req = _mapper.Map<IPublishedContent, ManagedContentWebhookBodyReq>(feedItem);

        return Task.FromResult<object>(req);
    }
    
    protected override string HookId => PlatformsConstants.Webhooks.HookIds.ManagedContent;

    private bool IsApprovedOrRejectedOrArchivedFolder(IContent content) {
        var parent = _contentLocator.Value.ById<IPublishedContent>(content.ParentId);

        if (parent.ContentType.Alias.IsAnyOf(PlatformsConstants.Feeds.Folders.ApprovedFolder,
                                             PlatformsConstants.Feeds.Folders.ArchivedFolder,
                                             PlatformsConstants.Feeds.Folders.RejectedFolder)) {
            return true;
        } else {
            return false;
        }
    }
}