using N3O.Umbraco.Content;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;

namespace N3O.Umbraco.Notifications;

public class SeedCulturesOnLanguageAdded : INotificationAsyncHandler<LanguageSavedNotification> {
    private readonly ICultureSeeder _cultureSeeder;

    public SeedCulturesOnLanguageAdded(ICultureSeeder cultureSeeder) {
        _cultureSeeder = cultureSeeder;
    }

    // Umbraco itself treats a language whose ID was just assigned as a new one.
    public Task HandleAsync(LanguageSavedNotification notification, CancellationToken cancellationToken) {
        if (notification.SavedEntities.Any(x => x.WasPropertyDirty(nameof(ILanguage.Id)))) {
            _cultureSeeder.SeedAll();
        }

        return Task.CompletedTask;
    }
}
