using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationSeedRes {
    public string DataTypeName { get; set; }
    public string PropertyEditorAlias { get; set; }
    public bool AlreadyExisted { get; set; }
    public bool Exists { get; set; }
    public IEnumerable<string> MissingElementTypes { get; set; } = [];
    public string Message { get; set; }
}
