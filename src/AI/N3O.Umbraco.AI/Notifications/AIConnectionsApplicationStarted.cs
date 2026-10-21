using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.AI.Connections;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Profiles;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.AI.Notifications;

[NoRegisterAll]
public class AIConnectionsApplicationStarted : INotificationAsyncHandler<UmbracoApplicationStartedNotification> {
    private readonly IConfiguration _configuration;
    private readonly IEnumerable<AIConnectionDefinition> _connectionDefinitions;
    private readonly IAIConnectionService _connectionService;
    private readonly ILogger _logger;
    private readonly IAIProfileService _profileService;
    private readonly IRuntimeState _runtimeState;

    public AIConnectionsApplicationStarted(IConfiguration configuration,
                                           IEnumerable<AIConnectionDefinition> connectionDefinitions,
                                           IAIConnectionService connectionService,
                                           ILogger<AIConnectionsApplicationStarted> logger,
                                           IAIProfileService profileService,
                                           IRuntimeState runtimeState) {
        _configuration = configuration;
        _connectionDefinitions = connectionDefinitions;
        _connectionService = connectionService;
        _logger = logger;
        _profileService = profileService;
        _runtimeState = runtimeState;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification,
                                  CancellationToken cancellationToken) {
        if (_runtimeState.Level == RuntimeLevel.Run) {
            foreach (var connectionDefinition in _connectionDefinitions) {
                try {
                    await ConvergeAsync(connectionDefinition, cancellationToken);
                } catch (Exception ex) {
                    _logger.LogError(ex,
                                     "Failed to converge the Umbraco AI connection {Alias}",
                                     connectionDefinition.Alias);
                }
            }
        }
    }

    private async Task ConvergeAsync(AIConnectionDefinition connectionDefinition, CancellationToken cancellationToken) {
        var connection = await _connectionService.GetConnectionByAliasAsync(connectionDefinition.Alias,
                                                                            cancellationToken);

        if (connection != null && connection.ProviderId != connectionDefinition.ProviderId) {
            _logger.LogWarning("Umbraco AI connection {Alias} uses provider {ProviderId} rather than " +
                               "{ExpectedProviderId}, so it is left unchanged",
                               connection.Alias,
                               connection.ProviderId,
                               connectionDefinition.ProviderId);
        } else if (IsConfigured(connectionDefinition)) {
            await CreateOrUpdateAsync(connectionDefinition, connection, cancellationToken);
        } else if (connection != null) {
            await DeleteAsync(connection, cancellationToken);
        }
    }

    private async Task CreateOrUpdateAsync(AIConnectionDefinition connectionDefinition,
                                           AIConnection connection,
                                           CancellationToken cancellationToken) {
        var settings = GetSettings(connectionDefinition);

        if (connection == null) {
            var alias = connectionDefinition.Alias;
            var name = connectionDefinition.Name;
            var providerId = connectionDefinition.ProviderId;

            connection = new AIConnection { Alias = alias, Name = name, ProviderId = providerId };
            connection.Settings = settings;

            await _connectionService.SaveConnectionAsync(connection, cancellationToken);

            _logger.LogInformation("Created the Umbraco AI connection {Alias}", alias);
        } else if (!SettingsMatch(connection.Settings, settings)) {
            connection.Settings = settings;

            await _connectionService.SaveConnectionAsync(connection, cancellationToken);

            _logger.LogInformation("Reset the settings of the Umbraco AI connection {Alias}", connection.Alias);
        }
    }

    private async Task DeleteAsync(AIConnection connection, CancellationToken cancellationToken) {
        try {
            await _connectionService.DeleteConnectionAsync(connection.Id, cancellationToken);

            _logger.LogInformation("Deleted the Umbraco AI connection {Alias} as its settings are not configured",
                                   connection.Alias);
        } catch (InvalidOperationException) {
            var profileNames = await GetProfileNamesAsync(connection, cancellationToken);

            if (profileNames.Any()) {
                _logger.LogWarning("Umbraco AI connection {Alias} has no configured settings but is kept because " +
                                   "it is used by the profiles {Profiles}",
                                   connection.Alias,
                                   string.Join(", ", profileNames));
            } else {
                throw;
            }
        }
    }

    private async Task<IReadOnlyList<string>> GetProfileNamesAsync(AIConnection connection,
                                                                   CancellationToken cancellationToken) {
        var profiles = await _profileService.GetAllProfilesAsync(cancellationToken);

        return profiles.Where(x => x.ConnectionId == connection.Id).Select(x => x.Name).ToList();
    }

    private Dictionary<string, string> GetSettings(AIConnectionDefinition connectionDefinition) {
        var settings = new Dictionary<string, string>();

        foreach (var setting in connectionDefinition.Settings) {
            var key = setting.GetConfigurationKey(connectionDefinition.KeyPrefix);

            if (_configuration[key].HasValue()) {
                settings[setting.Field] = $"${key}";
            }
        }

        return settings;
    }

    private bool IsConfigured(AIConnectionDefinition connectionDefinition) {
        var requiredKeys = connectionDefinition.Settings
                                               .Where(x => x.IsRequired)
                                               .Select(x => x.GetConfigurationKey(connectionDefinition.KeyPrefix));

        return requiredKeys.All(x => _configuration[x].HasValue());
    }

    private bool SettingsMatch(object currentSettings, Dictionary<string, string> settings) {
        var currentValues = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        var element = JsonSerializer.SerializeToElement(currentSettings);

        if (element.ValueKind == JsonValueKind.Object) {
            foreach (var property in element.EnumerateObject()) {
                if (property.Value.ValueKind != JsonValueKind.Null) {
                    currentValues[property.Name] = property.Value.ToString();
                }
            }
        }

        return currentValues.Count == settings.Count &&
               settings.All(x => currentValues.TryGetValue(x.Key, out var value) && value == x.Value);
    }
}
