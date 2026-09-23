# N3O.Umbraco.Analytics

Renders Google Analytics 4 and Google Tag Manager onto pages, and builds the data layer they read.
The measurement and container identifiers come from Umbraco settings content rather than
configuration, so they differ per site without a deployment.

The data layer is assembled from every `IDataLayerProvider` in our assemblies, which lets another
package contribute its own keys without this package knowing about it; giving analytics does exactly
that. Tag helpers place the snippets and emit events from a view, and page modules add them to every
page that opts in.

Tags collected earlier in a visit are held in a cookie and read back through `ITagsAccessor`, so a
value captured on one page is still available when a later page builds its data layer.
