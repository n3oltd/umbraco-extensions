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

A block whose settings offer a choice of layout keeps one partial per layout in a folder named after
the block: `Views/Partials/Blocks/<block alias>/<layout>.cshtml`, beside a `default.cshtml`.
`Html.RenderBlockLayoutAsync` renders the partial for the layout the settings hold, and
`default.cshtml` when no layout is chosen or the layout has no partial. A layout is named after its
thumbnail: a stored `Alt1`, or a stored path ending `Alt1.png`, renders `alt1.cshtml`, and
`ToBlockLayoutName` gives that name to code outside a view.
The thumbnail's file name is also what a layout picker can show as the layout's caption, so name
the file for what the layout is (`Timeline.png`).
