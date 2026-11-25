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

A block whose settings offer a choice of variant keeps one partial per variant in a folder named
after the block: `Views/Partials/Blocks/<block alias>/<variant>.cshtml`, beside a `default.cshtml`.
`Html.RenderBlockVariantAsync` renders the partial for the variant the settings hold, and
`default.cshtml` when no variant is chosen or the variant has no partial. A variant is named after
its thumbnail: a stored `Alt1`, or a stored path ending `Alt1.png`, renders `alt1.cshtml`, and
`ToBlockVariantName` gives that name to code outside a view.
The thumbnail's file name is also what a variant picker can show as the variant's caption, so name
the file for what the variant is (`Timeline.png`).
