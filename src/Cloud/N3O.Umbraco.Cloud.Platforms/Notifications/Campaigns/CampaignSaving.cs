using Microsoft.Extensions.Logging;
using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Validation;
using Slugify;
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

public class CampaignSaving : INotificationAsyncHandler<ContentSavingNotification> {
    private const string GenericError = "Something went wrong, please try again. If this keeps happening, contact " +
                                        "support";

    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(4);

    private readonly Lazy<ClientFactory<PlatformsUmbracoClient>> _clientFactory;
    private readonly IContentTypeService _contentTypeService;
    private readonly ILogger<CampaignSaving> _logger;
    private readonly ISlugHelper _slugHelper;

    public CampaignSaving(Lazy<ClientFactory<PlatformsUmbracoClient>> clientFactory,
                          IContentTypeService contentTypeService,
                          ILogger<CampaignSaving> logger,
                          ISlugHelper slugHelper) {
        _clientFactory = clientFactory;
        _contentTypeService = contentTypeService;
        _logger = logger;
        _slugHelper = slugHelper;
    }

    public async Task HandleAsync(ContentSavingNotification notification, CancellationToken cancellationToken) {
        foreach (var content in notification.SavedEntities) {
            if (content.PublishedState != PublishedState.Publishing || !content.IsCampaign(_contentTypeService)) {
                continue;
            }

            var errors = await GetNameErrorsAsync(content, cancellationToken);

            // Cancelling stops the whole notification, not the one entity
            if (errors.HasAny()) {
                foreach (var error in errors) {
                    notification.CancelWithError(error);
                }

                return;
            }
        }
    }

    private async Task<IReadOnlyList<string>> GetNameErrorsAsync(IContent content,
                                                                 CancellationToken cancellationToken) {
        var req = new CampaignNameAvailableReq();
        req.Name = content.Name;
        req.Slug = _slugHelper.GenerateSlug(content.Name);

        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)) {
            timeout.CancelAfter(CheckTimeout);

            try {
                var client = _clientFactory.Value.Create(CloudApiTypes.Engage, bearerToken: null);

                await client.InvokeAsync(x => x.CampaignNameAvailableAsync(content.Key.ToString(), req, timeout.Token));

                return [];
            } catch (ValidationException ex) {
                return ex.Failures.Select(x => x.Error).ToList();
            } catch (Exception ex) {
                _logger.LogError(ex,
                                 "Error checking whether campaign {CampaignKey} name is available: {Error}",
                                 content.Key,
                                 ex.Message);

                return [GenericError];
            }
        }
    }
}
