using N3O.Umbraco.Cloud.Platforms.Search.Lookups;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud.Platforms.Search;

public interface IPlatformsCollectionNameResolver {
    Task<string> ResolveAsync(PlatformsSearchCollection collection, CancellationToken cancellationToken = default);
}
