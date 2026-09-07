using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.ContentTypes;
using N3O.Umbraco.DataTypes;
using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;

namespace N3O.Umbraco.Cloud.Platforms;

public partial class PlatformsSchemaComponent : IComponent {
    private readonly IConfiguration _configuration;
    private readonly IContentTypeEditor _contentTypeEditor;
    private readonly Lazy<IPlatformsContentTypeSeeder> _contentTypeSeeder;
    private readonly IDataTypeEditor _dataTypeEditor;
    private readonly Lazy<IPlatformsDataTypeSeeder> _dataTypeSeeder;
    private readonly Lazy<IKeyValueService> _keyValueService;
    private readonly ILogger<PlatformsSchemaComponent> _logger;
    private readonly Lazy<IMigrationPlanExecutor> _migrationPlanExecutor;
    private readonly IRuntimeState _runtimeState;
    private readonly Lazy<IPlatformsSchemaAudit> _schemaAudit;
    private readonly Lazy<ICoreScopeProvider> _scopeProvider;

    public PlatformsSchemaComponent(IConfiguration configuration,
                                    IContentTypeEditor contentTypeEditor,
                                    Lazy<IPlatformsContentTypeSeeder> contentTypeSeeder,
                                    IContentTypeService contentTypeService,
                                    IDataTypeEditor dataTypeEditor,
                                    Lazy<IPlatformsDataTypeSeeder> dataTypeSeeder,
                                    Lazy<IKeyValueService> keyValueService,
                                    ILogger<PlatformsSchemaComponent> logger,
                                    Lazy<IMigrationPlanExecutor> migrationPlanExecutor,
                                    IRuntimeState runtimeState,
                                    Lazy<IPlatformsSchemaAudit> schemaAudit,
                                    Lazy<ICoreScopeProvider> scopeProvider,
                                    IShortStringHelper shortStringHelper) {
        _configuration = configuration;
        _contentTypeEditor = contentTypeEditor;
        _contentTypeSeeder = contentTypeSeeder;
        _contentTypeService = contentTypeService;
        _dataTypeEditor = dataTypeEditor;
        _dataTypeSeeder = dataTypeSeeder;
        _keyValueService = keyValueService;
        _logger = logger;
        _migrationPlanExecutor = migrationPlanExecutor;
        _runtimeState = runtimeState;
        _schemaAudit = schemaAudit;
        _scopeProvider = scopeProvider;
        _shortStringHelper = shortStringHelper;
    }

    public void Initialize() {
        if (_runtimeState.Level != RuntimeLevel.Run) {
            return;
        }

        AllowCrowdfundingCampaignsUnderPlatforms();

        if (!IsEnabled()) {
            return;
        }

        _dataTypeSeeder.Value.Seed();
        _contentTypeSeeder.Value.Seed();

        CorrectCrowdfundingCampaignNames();

        var blockers = FindMigrationBlockers();

        if (blockers.None()) {
            var upgrader = new Upgrader(new PlatformsSchemaPlan());

            var executed = upgrader.Execute(_migrationPlanExecutor.Value,
                                            _scopeProvider.Value,
                                            _keyValueService.Value);

            if (!executed.Successful) {
                _logger.LogError(executed.Exception, "Platforms schema migration plan failed");
            }
        } else {
            _logger.LogWarning("Platforms schema migrations blocked, waiting on {Blockers}",
                               string.Join(", ", blockers));
        }

        foreach (var gap in _schemaAudit.Value.FindGaps()) {
            _logger.LogInformation("Platforms schema gap: {Gap}", gap);
        }
    }

    public void Terminate() { }

    private void AllowCrowdfundingCampaignsUnderPlatforms() {
        var crowdfundingCampaigns = _contentTypeEditor.Find(PlatformsConstants.CrowdfundingCampaigns.Alias);
        var platforms = _contentTypeEditor.Find(PlatformsConstants.Platforms.Alias);

        if (crowdfundingCampaigns == null ||
            platforms == null ||
            platforms.AllowedContentTypes.OrEmpty().Any(x => x.Alias == crowdfundingCampaigns.Alias)) {
            return;
        }

        try {
            var designer = (IDocumentTypeDesigner) _contentTypeEditor.ForExisting(platforms.Alias);

            designer.AllowChildren(crowdfundingCampaigns.Alias);

            designer.Save();
        } catch (Exception ex) {
            _logger.LogError(ex, "Could not allow {Alias} under the platforms type", crowdfundingCampaigns.Alias);
        }
    }

    private IReadOnlyList<string> FindMigrationBlockers() {
        var blockers = new List<string>();

        blockers.AddRange(PlatformsSchemaPlan.RequiredContentTypes.Where(x => _contentTypeEditor.Find(x) == null));
        blockers.AddRange(PlatformsSchemaPlan.RequiredDataTypes.Where(x => _dataTypeEditor.Find(x) == null));

        return blockers;
    }

    private bool IsEnabled() {
        var section = _configuration.GetSection(PlatformsSchemaConstants.ConfigurationSection);

        return bool.TryParse(section[nameof(PlatformsFeatureSettings.Enabled)], out var enabled) && enabled;
    }
}
