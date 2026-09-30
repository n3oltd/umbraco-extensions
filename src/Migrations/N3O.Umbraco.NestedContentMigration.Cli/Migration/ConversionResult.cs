using System.Collections.Generic;

namespace N3O.Umbraco.NestedContentMigration.Cli;

// The outcome of converting a single Nested Content property value. Json is null when the value is not a
// Nested Content array (e.g. already migrated, or some other shape) and must be left untouched.
public sealed class ConversionResult {
    public string Json { get; set; }
    public int Blocks { get; set; }
    public List<string> SkippedAliases { get; set; } = new();
    public int GeneratedKeys { get; set; }

    // Element-property aliases whose value was itself Nested Content and could NOT be converted — copied
    // verbatim. Only a genuine failure lands here; a successful recursive conversion is counted in
    // NestedContentConvertedNames instead.
    public List<string> NestedContentPropertyNames { get; set; } = new();

    // Element-property aliases whose Nested Content value was itself recursively converted to Block List.
    public List<string> NestedContentConvertedNames { get; set; } = new();

    // Element-property aliases skipped because they collide with reserved Block List identity field names.
    public List<string> PropertyCollisionNames { get; set; } = new();
}
