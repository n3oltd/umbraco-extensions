namespace N3O.Umbraco.NestedContentMigration.Cli;

public sealed class CliOptions {
    public string ConnectionString { get; set; }

    public bool DryRun { get; set; }

    public bool Verbose { get; set; }
    public string LogFilePath { get; set; }

    public bool IncludePerplex { get; set; }
}
