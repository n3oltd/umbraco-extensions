using Umbraco.Cms.Infrastructure.Migrations;

namespace N3O.Umbraco.Content;

// Umbraco suppresses notifications while a migration runs, so the published cache is rebuilt once it completes.
public class CultureSeedingMigration : MigrationBase {
    private readonly ICultureSeeder _cultureSeeder;

    public CultureSeedingMigration(IMigrationContext context, ICultureSeeder cultureSeeder) : base(context) {
        _cultureSeeder = cultureSeeder;
    }

    protected override void Migrate() {
        _cultureSeeder.SeedAll();

        RebuildCache = true;
    }
}
