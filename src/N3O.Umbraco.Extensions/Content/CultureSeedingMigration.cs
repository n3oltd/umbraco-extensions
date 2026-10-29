using Umbraco.Cms.Infrastructure.Migrations;

namespace N3O.Umbraco.Content;

// Unscoped, because a scoped migration suppresses the notifications its saves raise, and those refresh the published
// cache on every server and the search indexes.
public class CultureSeedingMigration : UnscopedMigrationBase {
    private readonly ICultureSeeder _cultureSeeder;

    public CultureSeedingMigration(IMigrationContext context, ICultureSeeder cultureSeeder) : base(context) {
        _cultureSeeder = cultureSeeder;
    }

    protected override void Migrate() {
        _cultureSeeder.SeedAll();
    }
}
