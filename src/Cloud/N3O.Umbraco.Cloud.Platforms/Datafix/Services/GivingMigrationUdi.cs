using System;

namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public static class GivingMigrationUdi {
    public static string ForDocument(Guid key) {
        return "umb://document/" + key.ToString("N").ToLowerInvariant();
    }
}
