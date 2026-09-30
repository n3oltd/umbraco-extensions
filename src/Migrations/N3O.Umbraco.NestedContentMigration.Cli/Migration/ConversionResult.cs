using System.Collections.Generic;

namespace N3O.Umbraco.NestedContentMigration.Cli;

public sealed class ConversionResult {
    public string Json { get; set; }
    public int Blocks { get; set; }
    public List<string> SkippedAliases { get; set; } = new();
    public int GeneratedKeys { get; set; }

    public List<string> NestedContentPropertyNames { get; set; } = new();

    public List<string> NestedContentConvertedNames { get; set; } = new();

    public List<string> PropertyCollisionNames { get; set; } = new();
}
