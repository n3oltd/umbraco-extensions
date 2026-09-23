# N3O.Umbraco.Cells

A property editor that stores a grid of cells, exposing the value as a two-dimensional array of
objects so a view indexes it by row and column.

The editor's configuration is a single grid configuration field holding the Handsontable settings,
so the shape and behaviour of the grid are set per data type rather than per property. It is
free-form JSON, which means a malformed value is only discovered when an editor opens the property.
