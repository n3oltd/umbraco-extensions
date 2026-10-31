using Microsoft.Extensions.Logging;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;

namespace N3O.Umbraco.Notifications;

[SkipDuringSync]
public class SeedCulturesOnPublish : INotificationAsyncHandler<ContentPublishedNotification> {
    private readonly ILogger<SeedCulturesOnPublish> _logger;
    private readonly ICultureSeeder _cultureSeeder;

    public SeedCulturesOnPublish(ILogger<SeedCulturesOnPublish> logger, ICultureSeeder cultureSeeder) {
        _logger = logger;
        _cultureSeeder = cultureSeeder;
    }

    public Task HandleAsync(ContentPublishedNotification notification, CancellationToken cancellationToken) {
        foreach (var content in notification.PublishedEntities) {
            if (!TrySeed(content)) {
                AddWarning(notification, content);
            }
        }

        return Task.CompletedTask;
    }

    private void AddWarning(ContentPublishedNotification notification, IContent content) {
        var message = $"The other language versions of {content.Name.Quote()} could not be created or published";

        notification.Messages.Add(new EventMessage("Warning", message, EventMessageType.Warning));
    }

    private bool TrySeed(IContent content) {
        try {
            return _cultureSeeder.Seed(content, content.WriterId);
        } catch (Exception ex) {
            _logger.LogError(ex, "Error seeding the missing cultures of content {ID}", content.Key);

            return false;
        }
    }
}
