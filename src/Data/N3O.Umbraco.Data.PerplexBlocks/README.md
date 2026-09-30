# N3O.Umbraco.Data.PerplexBlocks

Teaches content import and export about Perplex Content Blocks, by supplying the property converter
for that property editor.

Without it a Perplex blocks property is skipped, because converters are matched on the property
editor's alias and no other package claims this one.
