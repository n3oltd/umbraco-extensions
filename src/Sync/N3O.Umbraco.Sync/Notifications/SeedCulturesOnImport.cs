using N3O.Umbraco.Content;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using uSync.BackOffice;

namespace N3O.Umbraco.Sync;

// A content import unpublishes every culture missing from its files and seeding is skipped while it runs, so the
// seeded cultures are restored once it completes.
public class SeedCulturesOnImport : INotificationAsyncHandler<uSyncImportCompletedNotification> {
    private readonly ICultureSeeder _cultureSeeder;

    public SeedCulturesOnImport(ICultureSeeder cultureSeeder) {
        _cultureSeeder = cultureSeeder;
    }

    public Task HandleAsync(uSyncImportCompletedNotification notification, CancellationToken cancellationToken) {
        _cultureSeeder.SeedAll();

        return Task.CompletedTask;
    }
}
