using System;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud.Platforms;

public interface IGivingMigrationMedia {
    Task<GivingPlaceholders> ResolveAsync(Guid? iconMediaId,
                                          Guid? imageMediaId,
                                          Guid? heroImageMediaId,
                                          CancellationToken cancellationToken);
}
