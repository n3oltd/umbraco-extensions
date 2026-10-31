using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Notifications;

// Read in Saving: a second save of the language before Saved forgets that it is new, and a new default language has
// no content to seed from.
public class SeedCulturesOnLanguageAdded :
    INotificationAsyncHandler<LanguageSavedNotification>,
    INotificationAsyncHandler<LanguageSavingNotification> {
    private const string SourceCultureKey = "N3O.SeedCulturesSource";

    private readonly ICultureSeeder _cultureSeeder;
    private readonly ILocalizationService _localizationService;

    public SeedCulturesOnLanguageAdded(ICultureSeeder cultureSeeder, ILocalizationService localizationService) {
        _cultureSeeder = cultureSeeder;
        _localizationService = localizationService;
    }

    public Task HandleAsync(LanguageSavedNotification notification, CancellationToken cancellationToken) {
        if (notification.State.TryGetValue(SourceCultureKey, out var sourceCulture)) {
            _cultureSeeder.SeedAll((string) sourceCulture);
        }

        return Task.CompletedTask;
    }

    public Task HandleAsync(LanguageSavingNotification notification, CancellationToken cancellationToken) {
        if (notification.SavedEntities.Any(x => !x.HasIdentity)) {
            notification.State[SourceCultureKey] = _localizationService.GetDefaultCultureCode();
        }

        return Task.CompletedTask;
    }
}
