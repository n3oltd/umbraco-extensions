using System.Collections.Generic;

namespace N3O.Umbraco.NestedContentMigration.Cli;

// The outcome of converting a single Perplex.ContentBlocks property value from v3 (NestedContent-backed) to
// v4 (Block Editor shape). Json is null when the value is not a convertible v3 Perplex value (already v4,
// empty, or an unrecognised shape) and must be left untouched.
public sealed class PerplexConversionResult {
    public string Json { get; set; }
    public int Blocks { get; set; }

    // Block element-type aliases that could not be resolved to an element content type — block dropped.
    public List<string> SkippedAliases { get; set; } = new();

    // Blocks whose NestedContent item had a missing/invalid key — a new GUID was generated.
    public int GeneratedKeys { get; set; }

    // "<elementAlias>.<propertyAlias>" pairs whose editor alias could not be resolved because the property is
    // not on the element type (an orphaned value from a since-removed property). These are DROPPED from the v4
    // output — v4 ignores unknown-alias block values, and a null-editorAlias value can break the v4 reader /
    // ModelsBuilder — and flagged here for review.
    public List<string> OrphanedProperties { get; set; } = new();

    // A block carried Perplex v3 "variants" (culture/segment variant content). This converter does not carry
    // variant content across; the operator must verify those manually.
    public bool HadVariants { get; set; }

    // Block property values that were themselves Nested Content and were recursively converted to Block List.
    // Step 1 of the migration flips every in-use NC data type (including ones used only inside a Perplex block)
    // to Block List, so an unconverted inner value would leave the data type and its value disagreeing and the
    // page would throw on render ("Cannot deserialize the current JSON array ... into type 'BlockValue'").
    public int NestedContentConverted { get; set; }
    public int NestedContentBlocks { get; set; }

    // Block property aliases whose Nested Content value could NOT be converted — copied verbatim, needs review.
    public List<string> NestedContentLeftVerbatim { get; set; } = new();
}
