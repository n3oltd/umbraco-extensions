using N3O.Umbraco.Content;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace N3O.Umbraco.Notifications;

// Umbraco moves an existing type's values into the default culture with SQL when the type starts varying by
// culture, which raises no content save. The change is detected in Saving, which shares its state with Saved,
// because a second save of the same type before Saved is raised clears what it remembers as changed.
public class SeedCulturesOnVariationChanged :
    INotificationAsyncHandler<ContentTypeSavedNotification>,
    INotificationAsyncHandler<ContentTypeSavingNotification> {
    private const string VariationChangedKey = "N3O.SeedCulturesVariationChanged";

    private readonly ICultureSeeder _cultureSeeder;

    public SeedCulturesOnVariationChanged(ICultureSeeder cultureSeeder) {
        _cultureSeeder = cultureSeeder;
    }

    public Task HandleAsync(ContentTypeSavedNotification notification, CancellationToken cancellationToken) {
        if (notification.State.ContainsKey(VariationChangedKey)) {
            _cultureSeeder.SeedAll();
        }

        return Task.CompletedTask;
    }

    public Task HandleAsync(ContentTypeSavingNotification notification, CancellationToken cancellationToken) {
        if (notification.SavedEntities.Any(IsNowVaryingByCulture)) {
            notification.State[VariationChangedKey] = true;
        }

        return Task.CompletedTask;
    }

    private bool IsNowVaryingByCulture(IContentType contentType) {
        return contentType.HasIdentity &&
               contentType.IsPropertyDirty(nameof(IContentType.Variations)) &&
               contentType.VariesByCulture();
    }
}
