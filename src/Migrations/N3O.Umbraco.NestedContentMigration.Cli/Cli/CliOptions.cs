namespace N3O.Umbraco.NestedContentMigration.Cli;

public sealed class CliOptions {
    public string ConnectionString { get; set; }

    // --apply is the absence of --dry-run: Program requires exactly one of the two, so one flag holds both
    // states and there is no way for them to disagree.
    public bool DryRun { get; set; }

    public bool Verbose { get; set; }
    public string LogFilePath { get; set; }

    // Also convert Perplex.ContentBlocks property values from the v3 (NestedContent) shape to the v4 (Block
    // Editor) shape, in the same transaction. Opt-in: off by default so the standard Nested Content → Block
    // List behaviour is unchanged. Run offline on the v13 database before the 13→17 + Perplex-4 upgrade.
    public bool IncludePerplex { get; set; }
}
