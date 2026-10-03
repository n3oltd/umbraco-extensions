using System;

namespace N3O.Umbraco.NestedContentMigration.Cli;

public class LocalLinkTarget {
    public const string DocumentEntityType = "document";
    public const string MediaEntityType = "media";

    public LocalLinkTarget(Guid key, string entityType) {
        Key = key;
        EntityType = entityType;
    }

    public Guid Key { get; }
    public string EntityType { get; }
}
