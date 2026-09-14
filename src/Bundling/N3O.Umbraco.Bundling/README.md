# N3O.Umbraco.Bundling

Build-time asset bundling for N3O Umbraco sites. Assets are compiled, minified and content-hashed at
build time by `@n3oltd/asset-build`, which writes an `assets-manifest.json`; this package reads that
manifest and emits the right `<link>` / `<script>` tags.

There is no runtime bundler. Umbraco removed Smidge in v14 and ships no replacement; the file set for
a site front-end is fully known at build time, so it is resolved there.

## Usage

Register the tag helpers in `_ViewImports.cshtml`:

```cshtml
@addTagHelper *, N3O.Umbraco.Bundling
```

Then reference a bundle by name (defaults to `main`):

```cshtml
<n3o-css-bundle />
<n3o-js-bundle name="checkout" />
```

Each tag emits one element per asset in the bundle, in manifest order, using the content-hashed URL.
A bundle that is not in the manifest emits nothing.

## Settings

Bound from the `N3O:Bundling` configuration section:

| Setting | Default | Purpose |
| --- | --- | --- |
| `ManifestPath` | `assets/bundles/assets-manifest.json` | Manifest location, relative to the web root |
| `BaseUrl` | *(none)* | Prefix for emitted URLs, for serving assets from a CDN |
| `ServeSourceMaps` | `false` | When false, `.map` requests under the bundles folder return 404 |

`ServeSourceMaps` matters only when the build emitted sourcemaps. It lets one artifact serve maps in
staging and withhold them in production, where a published `.map` would expose original source. It is
enforced by a middleware added through an `IStartupFilter`, because `CmsStartup` calls `UseStaticFiles`
before `UseUmbraco` and every Umbraco pipeline hook therefore runs too late to withhold the file.
