namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationRepointItemRes {
    public string ContentTypeAlias { get; set; }
    public string DeclaringContentTypeAlias { get; set; }
    public string PropertyAlias { get; set; }
    public string FromEditorAlias { get; set; }
    public string ToEditorAlias { get; set; }
    public string Outcome { get; set; }
    public string Message { get; set; }
}
