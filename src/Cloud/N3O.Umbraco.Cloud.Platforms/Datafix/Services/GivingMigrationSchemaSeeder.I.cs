using N3O.Umbraco.Cloud.Platforms.Models;

namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public interface IGivingMigrationSchemaSeeder {
    GivingMigrationSeedRes Seed();
}
