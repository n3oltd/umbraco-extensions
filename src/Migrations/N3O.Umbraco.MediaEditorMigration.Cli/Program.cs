using System;
using System.IO;
using System.Linq;

namespace N3O.Umbraco.MediaEditorMigration.Cli;

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
            ? Path.Combine(Directory.GetCurrentDirectory(), $"media-migrate-{DateTime.UtcNow:yyyyMMdd-HHmmss}Z.log")
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
        var editor = EditorScope.Both;
        var target = MigrationTarget.Inline;
        var mediaParentId = -1;
        var mediaParentGiven = false;
        var dryRun = false;
        var apply = false;
        var verbose = false;
        string logPath = null;

        for (var i = 0; i < args.Length; i++) {
            switch (args[i]) {
                case "--connection":
                    connection = RequireValue(args, ref i, "--connection");
                    break;

                case "--editor":
                    editor = ParseEditor(RequireValue(args, ref i, "--editor"));
                    break;

                case "--target":
                    target = ParseTarget(RequireValue(args, ref i, "--target"));
                    break;

                case "--media-parent":
                    mediaParentId = ParseInt(RequireValue(args, ref i, "--media-parent"), "--media-parent");
                    mediaParentGiven = true;
                    break;

                case "--dry-run":
                    dryRun = true;
                    break;

                case "--apply":
                    apply = true;
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
            connection = Environment.GetEnvironmentVariable("MEDIA_MIGRATE_CONNECTION");
        }

        if (string.IsNullOrWhiteSpace(connection)) {
            throw new ArgumentException("--connection is required (or set the MEDIA_MIGRATE_CONNECTION environment variable).");
        }

        if (dryRun == apply) {
            throw new ArgumentException("Specify exactly one of --dry-run or --apply.");
        }

        // --target inline creates no media nodes, so there is no parent to put them under. Rejecting this is
        // better than ignoring it: someone passing --media-parent believes media nodes are about to be created.
        if (mediaParentGiven && target == MigrationTarget.Inline) {
            throw new ArgumentException("--media-parent only applies to --target mediapicker; --target inline " +
                                        "creates no media nodes. Drop --media-parent, or pass --target mediapicker.");
        }

        return new CliOptions {
            ConnectionString = connection,
            Editor = editor,
            Target = target,
            MediaParentId = mediaParentId,
            DryRun = dryRun,
            Verbose = verbose,
            LogFilePath = logPath
        };
    }

    private static EditorScope ParseEditor(string value) {
        switch (value.Trim().ToLowerInvariant()) {
            case "both": return EditorScope.Both;
            case "cropper": return EditorScope.Cropper;
            case "uploader": return EditorScope.Uploader;
            default: throw new ArgumentException($"--editor must be one of: cropper, uploader, both (got '{value}').");
        }
    }

    private static MigrationTarget ParseTarget(string value) {
        switch (value.Trim().ToLowerInvariant()) {
            case "inline": return MigrationTarget.Inline;
            case "mediapicker": return MigrationTarget.MediaPicker;
            default: throw new ArgumentException($"--target must be one of: inline, mediapicker (got '{value}').");
        }
    }

    private static int ParseInt(string value, string name) {
        if (!int.TryParse(value, out var result)) {
            throw new ArgumentException($"{name} requires an integer value (got '{value}').");
        }

        return result;
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
        Console.WriteLine("media-migrate — N3O Cropper/Uploader → native Umbraco editor data migration (Umbraco 17, SQL Server)");
        Console.WriteLine();
        Console.WriteLine("USAGE:");
        Console.WriteLine("  media-migrate --connection \"<conn>\" (--dry-run | --apply) [--editor cropper|uploader|both]");
        Console.WriteLine("                [--target inline|mediapicker] [--media-parent <id>] [--verbose] [--log <path>]");
        Console.WriteLine();
        Console.WriteLine("Run this AFTER deploying the N3O.Umbraco package build that removes the custom Cropper/Uploader");
        Console.WriteLine("editors. For each stored value it rewrites the property value to the chosen native shape, does the");
        Console.WriteLine("same for values nested inside Block List / Block Grid / Perplex blocks, and flips each affected data");
        Console.WriteLine("type to the native editor.");
        Console.WriteLine();
        Console.WriteLine("TARGETS:");
        Console.WriteLine("  inline (default)  Cropper → Umbraco.ImageCropper, Uploader → Umbraco.UploadField.");
        Console.WriteLine("                    The closest 1:1 match: like the N3O editors these keep the file path ON THE");
        Console.WriteLine("                    PROPERTY, so no media node is created, no file is moved or copied, and every");
        Console.WriteLine("                    /media/{...} path carries over verbatim. Cropper crops are converted from");
        Console.WriteLine("                    absolute pixels to relative edge insets and the crop definitions move to the");
        Console.WriteLine("                    data type config; Uploader's allowed extensions move to fileExtensions.");
        Console.WriteLine("                    Alt text is DROPPED — neither editor has a slot for it.");
        Console.WriteLine("  mediapicker       Both → Umbraco.MediaPicker3. Registers each distinct file as a media library");
        Console.WriteLine("                    node (reusing its existing path — no file is moved) and stores the node's key,");
        Console.WriteLine("                    so files become reusable and manageable in the Media section and the alt text");
        Console.WriteLine("                    survives as the media node's name. Costs one media node per distinct file.");
        Console.WriteLine();
        Console.WriteLine("Either way the run reports how many values carried alt text, so it can be re-authored.");
        Console.WriteLine();
        Console.WriteLine("OPTIONS:");
        Console.WriteLine("  --connection    SQL Server connection string to the Umbraco 17 database (required, or set the");
        Console.WriteLine("                  MEDIA_MIGRATE_CONNECTION environment variable instead)");
        Console.WriteLine("  --editor        Which editor(s) to migrate: cropper | uploader | both (default: both)");
        Console.WriteLine("  --target        Which native editors to migrate to: inline | mediapicker (default: inline)");
        Console.WriteLine("  --media-parent  --target mediapicker only: umbracoNode id of the media folder to create the new");
        Console.WriteLine("                  media nodes under (default: -1, the Media section root)");
        Console.WriteLine("  --dry-run       Run everything in a transaction, then roll back; report what WOULD change");
        Console.WriteLine("  --apply         Commit the changes (mutually exclusive with --dry-run)");
        Console.WriteLine("  --verbose       Log each data type and property value processed");
        Console.WriteLine("  --log <path>    Write the full log to this file (default: media-migrate-<UTC>.log in the current");
        Console.WriteLine("                  directory). Every item that needs manual attention (e.g. a missing file, or a");
        Console.WriteLine("                  dropped alt-text) is written as a separated [REVIEW] block.");
        Console.WriteLine("  --help, -h      Show this help");
        Console.WriteLine();
        Console.WriteLine("NOTES:");
        Console.WriteLine("  - Pass exactly one of --dry-run or --apply.");
        Console.WriteLine("  - Requires the Umbraco 17+ schema; it refuses to run on Umbraco 13 (umbracoMediaVersion absent).");
        Console.WriteLine("  - ALWAYS back up the DATABASE *and* the media store (disk/blob) before --apply.");
        Console.WriteLine("  - Take the site OFFLINE first (one long transaction over umbracoPropertyData + new media nodes).");
        Console.WriteLine("  - AFTER --apply you MUST rebuild caches: clear NuCache (delete the on-disk NuCache.*.db), then in");
        Console.WriteLine("    the backoffice run Settings > Published Cache > Rebuild Database Cache and Rebuild the Examine");
        Console.WriteLine("    indexes, so the new media nodes resolve and the migrated values render.");
        Console.WriteLine("  - Native media items have no alt-text slot. Where the old value had altText it is logged as");
        Console.WriteLine("    [REVIEW] (and used as the media node name) so you can re-apply it manually if needed.");
        Console.WriteLine();
        Console.WriteLine("EXAMPLE:");
        Console.WriteLine("  media-migrate --connection \"Server=(local);Database=Site;Trusted_Connection=True;TrustServerCertificate=True\" --dry-run --verbose");
        Console.WriteLine();
    }
}
