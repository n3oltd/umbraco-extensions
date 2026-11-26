# N3O.Umbraco.Blocks.Perplex

Adds Perplex Content Blocks as a block source, so blocks authored in that editor go through the same
rendering pipeline as Umbraco's own block lists. It supplies the property editor and value editor,
the cloner and renderer for the Perplex format, and a layout builder for the editor's rows and
columns.

Perplex block definitions carry a category and a layout that Umbraco's block list has no equivalent
for; those are read here and exposed on the view model so a view can branch on them.

A view renders a page's Perplex blocks with `Html.RenderContentBlocksAsync(blocks, renderer)` from
`N3O.Umbraco.Blocks.Perplex.Extensions`, passing the `IContentBlocksRenderer` it injects. Perplex
Content Blocks 4 has no Razor helper of its own, and its renderer takes the view component and
partial view renderers as delegates, which this supplies from the current view.
