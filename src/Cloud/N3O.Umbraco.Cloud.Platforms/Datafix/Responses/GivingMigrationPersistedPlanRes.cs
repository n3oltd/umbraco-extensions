using NodaTime;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationPersistedPlanRes {
    public Instant SavedAt { get; set; }
    public GivingMigrationPlanRes Plan { get; set; }
}
