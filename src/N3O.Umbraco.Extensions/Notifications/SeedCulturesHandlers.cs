using N3O.Umbraco.Attributes;
using N3O.Umbraco.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace N3O.Umbraco.Notifications;

// Umbraco hands a save's Saving and Saved notifications the same state, which carries the seeded cultures across.
[SkipDuringSync]
public class SeedCulturesHandlers :
    INotificationAsyncHandler<ContentSavedNotification>,
    INotificationAsyncHandler<ContentSavingNotification> {
    private const string SeededCulturesKey = "N3O.SeededCultures";

    private readonly ICultureSeeder _cultureSeeder;

    public SeedCulturesHandlers(ICultureSeeder cultureSeeder) {
        _cultureSeeder = cultureSeeder;
    }

    public Task HandleAsync(ContentSavedNotification notification, CancellationToken cancellationToken) {
        if (notification.State.TryGetValue(SeededCulturesKey, out var state)) {
            var seededCultures = (Dictionary<Guid, IReadOnlyList<string>>) state;

            foreach (var content in notification.SavedEntities.Where(x => seededCultures.ContainsKey(x.Key))) {
                _cultureSeeder.PublishSeeded(content, seededCultures[content.Key]);
            }
        }

        return Task.CompletedTask;
    }

    public Task HandleAsync(ContentSavingNotification notification, CancellationToken cancellationToken) {
        var seededCultures = new Dictionary<Guid, IReadOnlyList<string>>();

        foreach (var content in notification.SavedEntities) {
            var cultures = _cultureSeeder.Seed(content);

            if (cultures.Any()) {
                seededCultures[content.Key] = cultures;
            }
        }

        if (seededCultures.Any()) {
            notification.State[SeededCulturesKey] = seededCultures;
        }

        return Task.CompletedTask;
    }
}
