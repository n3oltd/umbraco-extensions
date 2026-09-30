using System.Collections.Generic;

namespace N3O.Umbraco.NestedContentMigration.Cli;

public sealed class PerplexConversionResult {
    public string Json { get; set; }
    public int Blocks { get; set; }

    public List<string> SkippedAliases { get; set; } = new();

    public int GeneratedKeys { get; set; }

    public List<string> OrphanedProperties { get; set; } = new();

    public bool HadVariants { get; set; }

    public int NestedContentConverted { get; set; }
    public int NestedContentBlocks { get; set; }

    public List<string> NestedContentLeftVerbatim { get; set; } = new();
}
