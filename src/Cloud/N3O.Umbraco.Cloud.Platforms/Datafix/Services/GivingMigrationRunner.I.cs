using N3O.Umbraco.Cloud.Platforms.Models;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public interface IGivingMigrationRunner {
    GivingMigrationRunRes Complete(CompleteGivingMigrationReq req);
    Task<GivingMigrationRunRes> MigrateAsync(MigrateGivingReq req, CancellationToken cancellationToken);
}
