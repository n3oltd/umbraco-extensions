# N3O.Umbraco.Blocks.StaticAssets

Ships the back office preview for blocks: an App_Plugins bundle that renders a block through the
server-side pipeline and shows the result while an editor is working on it.

It contains no C#. The preview calls into `N3O.Umbraco.Blocks`, which this package depends on; a
site needs both for preview to work, and only the other one to render blocks on the front end.
