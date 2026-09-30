using System;
using System.IO;
using System.Linq;

namespace N3O.Umbraco.NestedContentMigration.Cli;

public static class Program {
    public static int Main(string[] args) {
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h")) {
            PrintHelp();

            return args.Length == 0 ? 1 : 0;
        }

        CliOptions options;

        try {
            options = Parse(args);
        } catch (ArgumentException ex) {
            Log.Error(ex.Message);
            Log.Info("Run with --help for usage.");

            return 1;
        }

        var logPath = string.IsNullOrWhiteSpace(options.LogFilePath)
            ? Path.Combine(Directory.GetCurrentDirectory(), $"nc-migrate-{DateTime.UtcNow:yyyyMMdd-HHmmss}Z.log")
            : options.LogFilePath;

        try {
            Log.OpenFile(logPath);
        } catch (Exception ex) {
            Log.Error($"Could not open log file '{logPath}': {ex.Message}");

            return 1;
        }

        try {
            Log.Info($"Logging to {logPath}");

            return new Migrator(options).Run() ? 0 : 1;
        } catch (Exception ex) {
            Log.Error($"Unhandled error: {ex.Message}");

            if (options.Verbose) {
                Log.Error(ex.ToString());
            }

            return 1;
        } finally {
            Log.Close();
        }
    }

    private static CliOptions Parse(string[] args) {
        string connection = null;
        var dryRun = false;
        var apply = false;
        var verbose = false;
        var includePerplex = false;
        string logPath = null;

        for (var i = 0; i < args.Length; i++) {
            switch (args[i]) {
                case "--connection":
                    connection = RequireValue(args, ref i, "--connection");
                    break;

                case "--dry-run":
                    dryRun = true;
                    break;

                case "--apply":
                    apply = true;
                    break;

                case "--include-perplex":
                    includePerplex = true;
                    break;

                case "--verbose":
                    verbose = true;
                    break;

                case "--log":
                    logPath = RequireValue(args, ref i, "--log");
                    break;

                default:
                    throw new ArgumentException($"Unknown argument '{args[i]}'.");
            }
        }

        if (string.IsNullOrWhiteSpace(connection)) {
            connection = Environment.GetEnvironmentVariable("NC_MIGRATE_CONNECTION");
        }

        if (string.IsNullOrWhiteSpace(connection)) {
            throw new ArgumentException("--connection is required (or set the NC_MIGRATE_CONNECTION environment variable).");
        }

        if (dryRun == apply) {
            throw new ArgumentException("Specify exactly one of --dry-run or --apply.");
        }

        return new CliOptions {
            ConnectionString = connection,
            DryRun = dryRun,
            Verbose = verbose,
            IncludePerplex = includePerplex,
            LogFilePath = logPath
        };
    }

    private static string RequireValue(string[] args, ref int i, string name) {
        if (i + 1 >= args.Length) {
            throw new ArgumentException($"{name} requires a value.");
        }

        i++;

        return args[i];
    }

    private static void PrintHelp() {
        Console.WriteLine();
        Console.WriteLine("nc-migrate — Nested Content → Block List data migration (standalone, SQL Server)");
        Console.WriteLine();
        Console.WriteLine("USAGE:");
        Console.WriteLine("  nc-migrate --connection \"<conn>\" (--dry-run | --apply) [--include-perplex]");
        Console.WriteLine("             [--verbose] [--log <path>]");
        Console.WriteLine();
        Console.WriteLine("Migrates an Umbraco 13 database in place to the v13 (udi-based) Block List shape. Run it");
        Console.WriteLine("before upgrading Umbraco; the 13->17 upgrade then converts the values to the v17 shape.");
        Console.WriteLine();
        Console.WriteLine("OPTIONS:");
        Console.WriteLine("  --connection  SQL Server connection string to the Umbraco 13 database (required,");
        Console.WriteLine("                or set the NC_MIGRATE_CONNECTION environment variable instead)");
        Console.WriteLine("  --dry-run     Run everything in a transaction, then roll back; report what WOULD change");
        Console.WriteLine("  --apply       Commit the changes (mutually exclusive with --dry-run)");
        Console.WriteLine("  --include-perplex  Also convert Perplex.ContentBlocks values from the v3");
        Console.WriteLine("                (NestedContent) shape to the v4 (Block Editor) shape, in the same");
        Console.WriteLine("                transaction. Off by default. Run offline on the v13 DB before the");
        Console.WriteLine("                13->17 + Perplex-4 upgrade (Perplex v4 ships no content migration).");
        Console.WriteLine("  --verbose     Log each data type / property value processed");
        Console.WriteLine("  --log <path>  Write the full log to this file (default: nc-migrate-<UTC>.log in");
        Console.WriteLine("                the current directory). Every item that wasn't cleanly migrated is");
        Console.WriteLine("                written there as a separated [REVIEW] block (node id + name +");
        Console.WriteLine("                property + reason) so you can find and manually check each one.");
        Console.WriteLine("  --help, -h    Show this help");
        Console.WriteLine();
        Console.WriteLine("NOTES:");
        Console.WriteLine("  - You must pass exactly one of --dry-run or --apply.");
        Console.WriteLine("  - EVERY Nested Content data type is converted, including ones no content property");
        Console.WriteLine("    points at. Umbraco.NestedContent does not exist from v14 on, so one left behind");
        Console.WriteLine("    shows as \"this property editor could not be found\" even with no values in it.");
        Console.WriteLine("  - Data types named \"Nested X (min, max)\" are renamed \"X Block List (min, max)\",");
        Console.WriteLine("    and minItems/maxItems carry across to the Block List validationLimit.");
        Console.WriteLine("  - Converted Block List data types have inline editing mode enabled.");
        Console.WriteLine("  - ALWAYS back up the database before --apply. The only rollback is a restore.");
        Console.WriteLine("  - Take the site OFFLINE first (one long transaction over umbracoPropertyData).");
        Console.WriteLine("  - The published cache invalidates itself: this tool clears Umbraco's cache-serializer");
        Console.WriteLine("    marker, so Umbraco rebuilds the whole cache on the next start. Delete the on-disk");
        Console.WriteLine("    NuCache.*.db before that start, and rebuild the Examine indexes afterwards.");
        Console.WriteLine("  - Runs on the Umbraco 13 database only; it refuses a v14+ schema. After --apply,");
        Console.WriteLine("    upgrade Umbraco normally — its own migrations convert udi-based values to key-based.");
        Console.WriteLine();
        Console.WriteLine("EXAMPLE:");
        Console.WriteLine("  nc-migrate --connection \"Server=(localdb)\\\\MSSQLLocalDB;Database=Umbraco;Trusted_Connection=True;TrustServerCertificate=True\" --dry-run --verbose");
        Console.WriteLine();
    }
}
