# N3O.Umbraco.Blocks.Perplex

Adds Perplex Content Blocks as a block source, so blocks authored in that editor go through the same
rendering pipeline as Umbraco's own block lists. It supplies the property editor and value editor,
the cloner and renderer for the Perplex format, and a layout builder for the editor's rows and
columns.

Perplex block definitions carry a category and a layout that Umbraco's block list has no equivalent
for; those are read here and exposed on the view model so a view can branch on them.
