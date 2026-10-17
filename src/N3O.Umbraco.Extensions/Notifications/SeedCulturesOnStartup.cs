using N3O.Umbraco.Content;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;

namespace N3O.Umbraco.Notifications;

// Runs once per site, after start-up so that its saves raise the notifications that refresh the published cache on
// every server and the search indexes.
public class SeedCulturesOnStartup : INotificationAsyncHandler<UmbracoApplicationStartedNotification> {
    private const string SeededKey = "N3O.CultureSeeding";

    private readonly IRuntimeState _runtimeState;
    private readonly IServerRoleAccessor _serverRoleAccessor;
    private readonly IKeyValueService _keyValueService;
    private readonly ICultureSeeder _cultureSeeder;

    public SeedCulturesOnStartup(IRuntimeState runtimeState,
                                 IServerRoleAccessor serverRoleAccessor,
                                 IKeyValueService keyValueService,
                                 ICultureSeeder cultureSeeder) {
        _runtimeState = runtimeState;
        _serverRoleAccessor = serverRoleAccessor;
        _keyValueService = keyValueService;
        _cultureSeeder = cultureSeeder;
    }

    public Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken) {
        if (_runtimeState.Level == RuntimeLevel.Run &&
            _serverRoleAccessor.CurrentServerRole != ServerRole.Subscriber &&
            _keyValueService.GetValue(SeededKey) == null) {
            _cultureSeeder.SeedAll();

            _keyValueService.SetValue(SeededKey, "v1");
        }

        return Task.CompletedTask;
    }
}
