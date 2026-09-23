# N3O.Umbraco.Bundling

Builds CSS and JavaScript bundles on top of Smidge, which Umbraco already ships. `IBundler` collects
the paths a request needs and returns the URLs to emit, so a view declares its assets where it uses
them rather than in a layout.

An `IAssetBundle` declares a bundle once and is discovered automatically across our assemblies,
which lets a package ship its own assets without the site registering them.
