using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud.Platforms;

public interface IPlatformsPagesChangedHandler {
    Task HandleAsync(CancellationToken cancellationToken = default);
}
