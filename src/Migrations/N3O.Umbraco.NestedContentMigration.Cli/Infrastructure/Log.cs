using System;
using System.Collections.Generic;
using System.IO;

namespace N3O.Umbraco.NestedContentMigration.Cli;

public static class Log {
    private static StreamWriter _file;

    public static string FilePath { get; private set; }

    // Opens (overwriting) the on-disk log file that mirrors all console output. Call once at startup.
    public static void OpenFile(string path) {
        FilePath = path;
        _file = new StreamWriter(path, append: false) { AutoFlush = true };
    }

    public static void Close() {
        _file?.Flush();
        _file?.Dispose();
        _file = null;
    }

    public static void Info(string message) => Write(ConsoleColor.Gray, "INFO ", message);
    public static void Warn(string message) => Write(ConsoleColor.Yellow, "WARN ", message);
    public static void Error(string message) => Write(ConsoleColor.Red, "ERROR", message);
    public static void Success(string message) => Write(ConsoleColor.Green, "OK   ", message);

    public static void Verbose(bool enabled, string message) {
        if (enabled) {
            Write(ConsoleColor.DarkGray, "DEBUG", message);
        }
    }

    // One multi-line entry for an item needing manual attention, to console and log file alike.
    public static void Item(string header, IReadOnlyList<string> reasons) {
        WriteLine(ConsoleColor.Yellow, "[REVIEW] " + header);

        foreach (var reason in reasons) {
            WriteLine(ConsoleColor.Yellow, "    - " + reason);
        }
    }

    private static void Write(ConsoleColor color, string level, string message) {
        WriteLine(color, $"{DateTime.UtcNow:HH:mm:ss}Z [{level}] {message}");
    }

    private static void WriteLine(ConsoleColor color, string line) {
        var previous = Console.ForegroundColor;

        Console.ForegroundColor = color;
        Console.WriteLine(line);
        Console.ForegroundColor = previous;

        _file?.WriteLine(line);
    }
}
