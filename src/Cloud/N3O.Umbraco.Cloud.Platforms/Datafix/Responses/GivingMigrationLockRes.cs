using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationLockRes {
    public bool Locked { get; set; }
    public string Message { get; set; }
    public IEnumerable<string> LockedContentTypes { get; set; } = [];
    public IEnumerable<string> AlreadyLockedContentTypes { get; set; } = [];
    public IEnumerable<string> RestoredContentTypes { get; set; } = [];
    public IEnumerable<string> UnrestoredContentTypes { get; set; } = [];
}
