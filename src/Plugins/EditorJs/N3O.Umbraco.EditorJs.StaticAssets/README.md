# N3O.Umbraco.EditorJs.StaticAssets

Ships the back office editor for the Editor.js property and the Razor partials that render each
block type, with a targets file that copies them into the consuming site.

Unlike the other `*.StaticAssets` packages it carries views the front end uses, not only back office
assets: one partial per block type and a partial that dispatches over them. A site overrides the
rendering of a block type by replacing the corresponding partial.
