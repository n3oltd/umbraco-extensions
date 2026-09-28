# N3O.Umbraco.Blocks.Perplex.StaticAssets

Ships the back office view for Perplex content blocks: an App_Plugins wrapper around the Perplex editor
that shows a property-level validation message, such as a mandatory property that holds no blocks.

The Perplex editor removes the property's own error message when it initialises, so without this view
a failed publish marks the tab as invalid but says nothing on the property itself.

It contains no C#. `N3O.Umbraco.Blocks.Perplex`, which this package depends on, points the Perplex
property editor at this view, so a site that references that package needs this one as well or the
editor will not render.
