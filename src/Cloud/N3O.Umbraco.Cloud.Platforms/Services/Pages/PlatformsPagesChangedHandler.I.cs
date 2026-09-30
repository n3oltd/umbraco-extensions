using N3O.Umbraco.Cloud.Platforms.Models;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud.Platforms;

public interface IPlatformsPagesChangedHandler {
    Task HandleAsync(string eventType,
                     WebhookPlatformsPage page,
                     CancellationToken cancellationToken = default);
}
