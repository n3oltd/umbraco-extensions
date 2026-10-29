using N3O.Umbraco.Content;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace N3O.Umbraco.Notifications;

// Umbraco moves existing values into the default culture with SQL when a type or one of its properties starts varying
// by culture, which raises no content save. The change is read in Saving, while the type still records it.
public class SeedCulturesOnVariationChanged :
    INotificationAsyncHandler<ContentTypeSavedNotification>,
    INotificationAsyncHandler<ContentTypeSavingNotification> {
    private const string VariationChangedKey = "N3O.SeedCulturesVariationChanged";

    private readonly ICultureSeeder _cultureSeeder;

    public SeedCulturesOnVariationChanged(ICultureSeeder cultureSeeder) {
        _cultureSeeder = cultureSeeder;
    }

    public Task HandleAsync(ContentTypeSavedNotification notification, CancellationToken cancellationToken) {
        if (notification.State.TryGetValue(VariationChangedKey, out var newlyVaryingPropertyTypeIds)) {
            _cultureSeeder.SeedAll((IReadOnlyCollection<int>) newlyVaryingPropertyTypeIds);
        }

        return Task.CompletedTask;
    }

    public Task HandleAsync(ContentTypeSavingNotification notification, CancellationToken cancellationToken) {
        var contentTypes = notification.SavedEntities.Where(IsNowVaryingByCulture).ToList();

        if (contentTypes.Any()) {
            notification.State[VariationChangedKey] = contentTypes.SelectMany(GetNewlyVaryingPropertyTypes)
                                                                  .Select(x => x.Id)
                                                                  .ToList();
        }

        return Task.CompletedTask;
    }

    private IEnumerable<IPropertyType> GetNewlyVaryingPropertyTypes(IContentType contentType) {
        return contentType.PropertyTypes.Where(x => x.HasIdentity &&
                                                    x.IsPropertyDirty(nameof(IPropertyType.Variations)) &&
                                                    x.VariesByCulture());
    }

    private bool IsNowVaryingByCulture(IContentType contentType) {
        if (!contentType.HasIdentity || !contentType.VariesByCulture()) {
            return false;
        }

        return contentType.IsPropertyDirty(nameof(IContentType.Variations)) ||
               GetNewlyVaryingPropertyTypes(contentType).Any();
    }
}
