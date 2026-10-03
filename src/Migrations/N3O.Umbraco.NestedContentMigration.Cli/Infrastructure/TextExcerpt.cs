using System;

namespace N3O.Umbraco.NestedContentMigration.Cli;

public static class TextExcerpt {
    private const int Length = 120;

    public static string From(string text, int start) {
        return text.Substring(start, Math.Min(Length, text.Length - start));
    }
}
