using System;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationPurgeItemRes {
    public Guid LegacyId { get; set; }
    public string LegacyPath { get; set; }
    public string ContentTypeAlias { get; set; }
    public string Outcome { get; set; }
    public string Message { get; set; }
}
