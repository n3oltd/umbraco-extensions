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

// Saved is raised after any publish in the same operation, so seeding copies the values that are now published.
[SkipDuringSync]
public class SeedCulturesOnSave : INotificationAsyncHandler<ContentSavedNotification> {
    private readonly ILogger<SeedCulturesOnSave> _logger;
    private readonly ICultureSeeder _cultureSeeder;

    public SeedCulturesOnSave(ILogger<SeedCulturesOnSave> logger, ICultureSeeder cultureSeeder) {
        _logger = logger;
        _cultureSeeder = cultureSeeder;
    }

    public Task HandleAsync(ContentSavedNotification notification, CancellationToken cancellationToken) {
        foreach (var content in notification.SavedEntities) {
            bool seeded;

            try {
                seeded = _cultureSeeder.Seed(content, content.WriterId);
            } catch (Exception ex) {
                _logger.LogError(ex, "Error seeding the missing cultures of content {ID}", content.Key);

                seeded = false;
            }

            if (!seeded) {
                AddWarning(notification, content);
            }
        }

        return Task.CompletedTask;
    }

    private void AddWarning(ContentSavedNotification notification, IContent content) {
        var message = $"The other language versions of {content.Name.Quote()} could not be created or published";

        notification.Messages.Add(new EventMessage("Warning", message, EventMessageType.Warning));
    }
}
