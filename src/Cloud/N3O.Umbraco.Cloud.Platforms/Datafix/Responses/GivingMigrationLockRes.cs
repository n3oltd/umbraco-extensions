using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationLockRes {
    public bool Locked { get; set; }
    public IReadOnlyList<string> LockedContentTypes { get; set; } = [];
    public IReadOnlyList<string> AlreadyLockedContentTypes { get; set; } = [];
}
