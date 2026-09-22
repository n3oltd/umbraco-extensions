using System;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public interface IGivingMigrationMedia {
    Task<GivingPlaceholders> ResolveAsync(Guid? iconMediaId,
                                          Guid? imageMediaId,
                                          Guid? heroImageMediaId,
                                          bool allowPlaceholder,
                                          CancellationToken cancellationToken);
}
