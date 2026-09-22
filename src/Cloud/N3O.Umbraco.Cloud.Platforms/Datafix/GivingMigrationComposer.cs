using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace N3O.Umbraco.Cloud.Platforms;

// Registrations live here rather than in PlatformsComposer so the whole migration is removed by deleting this
// folder.
// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public class GivingMigrationComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        builder.Services.AddTransient<IGivingBlockRewriter, GivingBlockRewriter>();
        builder.Services.AddTransient<IGivingMigrationMedia, GivingMigrationMedia>();
        builder.Services.AddTransient<IGivingMigrationPlanner, GivingMigrationPlanner>();
        builder.Services.AddTransient<IGivingMigrationReporter, GivingMigrationReporter>();
        builder.Services.AddTransient<IGivingMigrationRunner, GivingMigrationRunner>();
        builder.Services.AddTransient<IGivingMigrationSchemaSeeder, GivingMigrationSchemaSeeder>();
        builder.Services.AddTransient<IGivingMigrationStore, GivingMigrationStore>();
        builder.Services.AddTransient<IGivingMigrationWriter, GivingMigrationWriter>();
        builder.Services.AddTransient<ILegacyGivingPurger, LegacyGivingPurger>();
        builder.Services.AddTransient<ILegacyGivingTreeLock, LegacyGivingTreeLock>();
        builder.Services.AddTransient<ILegacyGivingTreeReader, LegacyGivingTreeReader>();
    }
}
