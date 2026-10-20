using N3O.Umbraco.Attributes;
using N3O.Umbraco.Content;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace N3O.Umbraco.Notifications;

// Saved is raised after any publish in the same operation, so seeding copies the values that are now published.
[SkipDuringSync]
public class SeedCulturesOnSave : INotificationAsyncHandler<ContentSavedNotification> {
    private readonly ICultureSeeder _cultureSeeder;

    public SeedCulturesOnSave(ICultureSeeder cultureSeeder) {
        _cultureSeeder = cultureSeeder;
    }

    public Task HandleAsync(ContentSavedNotification notification, CancellationToken cancellationToken) {
        foreach (var content in notification.SavedEntities) {
            _cultureSeeder.Seed(content, content.WriterId);
        }

        return Task.CompletedTask;
    }
}
