using System;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;

namespace N3O.Umbraco.UserProvisioning.Migrations;

public class UserProvisioningMigrationsComponent : IAsyncComponent {
    private readonly IRuntimeState _runtimeState;
    private readonly Lazy<ICoreScopeProvider> _scopeProvider;
    private readonly Lazy<IMigrationPlanExecutor> _migrationPlanExecutor;
    private readonly Lazy<IKeyValueService> _keyValueService;

    public UserProvisioningMigrationsComponent(IRuntimeState runtimeState,
                                               Lazy<ICoreScopeProvider> scopeProvider,
                                               Lazy<IMigrationPlanExecutor> migrationPlanExecutor,
                                               Lazy<IKeyValueService> keyValueService) {
        _runtimeState = runtimeState;
        _scopeProvider = scopeProvider;
        _migrationPlanExecutor = migrationPlanExecutor;
        _keyValueService = keyValueService;
    }

    public async Task InitializeAsync(bool isRestarting, CancellationToken cancellationToken) {
        if (_runtimeState.Level == RuntimeLevel.Run) {
            var migrationPlan = new MigrationPlan(UserProvisioningConstants.MigrationPlanName);
            migrationPlan.From(string.Empty).To<ScimUserKeysMigration>("v1");

            var upgrader = new Upgrader(migrationPlan);
            await upgrader.ExecuteAsync(_migrationPlanExecutor.Value, _scopeProvider.Value, _keyValueService.Value);
        }
    }

    public Task TerminateAsync(bool isRestarting, CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }
}
