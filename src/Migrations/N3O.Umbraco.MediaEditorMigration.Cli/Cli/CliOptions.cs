namespace N3O.Umbraco.MediaEditorMigration.Cli;

public sealed class CliOptions {
    public string ConnectionString { get; set; }
    public EditorScope Editor { get; set; } = EditorScope.Both;
    public MigrationTarget Target { get; set; } = MigrationTarget.Inline;
    public int MediaParentId { get; set; } = -1; // -1 = the Media section root (--target mediapicker only)

    // --apply is the absence of --dry-run: Program requires exactly one of the two, so one flag holds both
    // states and there is no way for them to disagree.
    public bool DryRun { get; set; }

    public bool Verbose { get; set; }
    public string LogFilePath { get; set; }
}

public enum EditorScope {
    Both,
    Cropper,
    Uploader
}

// Which native editors the retired N3O ones are replaced with.
public enum MigrationTarget {
    // Umbraco.ImageCropper / Umbraco.UploadField. Closest 1:1 match: like the N3O editors, the file path lives
    // on the property, so no media nodes are created and no /media path changes. Alt text is lost.
    Inline,

    // Umbraco.MediaPicker3 for both. Each file is registered in the media library first, which makes files
    // reusable and manageable and lets the alt text survive as the media node's name — at the cost of
    // inventing one media node per distinct file.
    MediaPicker
}
