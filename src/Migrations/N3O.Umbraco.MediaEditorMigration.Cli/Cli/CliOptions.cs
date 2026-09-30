namespace N3O.Umbraco.MediaEditorMigration.Cli;

public sealed class CliOptions {
    public string ConnectionString { get; set; }
    public EditorScope Editor { get; set; } = EditorScope.Both;
    public MigrationTarget Target { get; set; } = MigrationTarget.Inline;
    public int MediaParentId { get; set; } = -1;

    public bool DryRun { get; set; }

    public bool Verbose { get; set; }
    public string LogFilePath { get; set; }
}

public enum EditorScope {
    Both,
    Cropper,
    Uploader
}

public enum MigrationTarget {
    Inline,

    MediaPicker
}
