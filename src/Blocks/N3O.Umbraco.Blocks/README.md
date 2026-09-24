# N3O.Umbraco.Blocks

Turns Umbraco block lists into rendered HTML through a pipeline, so a block's view is given a view
model rather than the raw `IPublishedElement`. `IBlocksRenderer.RenderBlocksAsync` renders every
block in a property; `IBlockPipeline` runs one block through the stages below.

Three seams are discovered automatically across our assemblies. An `IBlockModule` contributes named
data to a block's view model when `ShouldExecute` accepts the block, which is how a feature package
attaches its own state to a block it does not own. An `IBlocksCloner` copies a block, and an
`IBlocksRendererPostProcessor` transforms the rendered HTML afterwards.

Razor runtime compilation is enabled here, with the site content root added as a file provider,
because blocks are rendered outside the normal view resolution path.
