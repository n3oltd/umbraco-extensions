# N3O.Umbraco.EditorJs

A rich text property editor built on Editor.js, which stores content as structured blocks rather
than as a blob of HTML, so each block can be rendered by its own view.

A block type is handled by an `IBlockDataConverter`, which claims a type identifier and turns the
editor's JSON into a typed block. Converters are discovered automatically, so supporting a new block
type is a matter of declaring one. Exactly one converter must claim each type: a block whose type
nothing claims fails to deserialise rather than being skipped, so enabling a tool in the editor
without adding its converter makes the whole property unreadable.

Block tunes — the per-block settings Editor.js attaches, such as alignment — are read alongside the
block data and exposed on the model.
