using N3O.Umbraco.Content;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace N3O.Umbraco.Notifications;

// Umbraco moves existing values into the default culture with SQL when a type or one of its properties starts varying
// by culture, which raises no content save. The change is read in Saving, while the stored type still has the old
// variation.
public class SeedCulturesOnVariationChanged :
    INotificationAsyncHandler<ContentTypeSavedNotification>,
    INotificationAsyncHandler<ContentTypeSavingNotification> {
    private const string VariationChangedKey = "N3O.SeedCulturesVariationChanged";

    private readonly ICultureSeeder _cultureSeeder;
    private readonly IContentTypeService _contentTypeService;

    public SeedCulturesOnVariationChanged(ICultureSeeder cultureSeeder, IContentTypeService contentTypeService) {
        _cultureSeeder = cultureSeeder;
        _contentTypeService = contentTypeService;
    }

    public Task HandleAsync(ContentTypeSavedNotification notification, CancellationToken cancellationToken) {
        if (notification.State.TryGetValue(VariationChangedKey, out var newlyVaryingPropertyTypeIds)) {
            _cultureSeeder.SeedAll((IEnumerable<int>) newlyVaryingPropertyTypeIds);
        }

        return Task.CompletedTask;
    }

    public Task HandleAsync(ContentTypeSavingNotification notification, CancellationToken cancellationToken) {
        var typeStartsVarying = false;
        var newlyVaryingPropertyTypeIds = new List<int>();

        foreach (var contentType in notification.SavedEntities.Where(x => x.HasIdentity && x.VariesByCulture())) {
            var storedContentType = _contentTypeService.Get(contentType.Id);

            typeStartsVarying |= !storedContentType.VariesByCulture();
            newlyVaryingPropertyTypeIds.AddRange(GetNewlyVaryingPropertyTypeIds(contentType, storedContentType));
        }

        if (typeStartsVarying || newlyVaryingPropertyTypeIds.Any()) {
            notification.State[VariationChangedKey] = newlyVaryingPropertyTypeIds;
        }

        return Task.CompletedTask;
    }

    private IReadOnlyList<int> GetNewlyVaryingPropertyTypeIds(IContentType contentType,
                                                              IContentType storedContentType) {
        var storedVaryingIds = storedContentType.PropertyTypes
                                                .Where(x => x.VariesByCulture())
                                                .Select(x => x.Id)
                                                .ToList();

        return contentType.PropertyTypes
                          .Where(x => x.HasIdentity && x.VariesByCulture() && !storedVaryingIds.Contains(x.Id))
                          .Select(x => x.Id)
                          .ToList();
    }
}
