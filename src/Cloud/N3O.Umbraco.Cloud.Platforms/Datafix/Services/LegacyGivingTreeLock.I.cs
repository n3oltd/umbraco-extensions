using N3O.Umbraco.Cloud.Platforms.Models;

namespace N3O.Umbraco.Cloud.Platforms;

public interface ILegacyGivingTreeLock {
    GivingMigrationLockRes GetStatus();
    GivingMigrationLockRes Lock();
}
