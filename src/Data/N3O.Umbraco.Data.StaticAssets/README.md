# N3O.Umbraco.Data.StaticAssets

Ships the back office interface for content import and export: the export and import content apps,
the viewer for the notices an import produces, and the editor for reviewing import data.

It contains no C#. `N3O.Umbraco.Data` does the work and registers the content apps; this package
carries the App_Plugins views and a targets file that copies them into the consuming site.
