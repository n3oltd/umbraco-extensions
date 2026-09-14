# @n3oltd/asset-build

Build-time bundling, minification and content-hashing for N3O Umbraco site assets. Emits an
`assets-manifest.json` that `N3O.Umbraco.Bundling` reads to render `<link>` / `<script>` tags.

CSS goes through **dart-sass** (when the entry is `.scss`) and then **Lightning CSS**, which minifies
and applies browser targets. JS goes through **esbuild**.

## Two kinds of entry

- **`compile`** — build this source file.
- **`adopt`** — take an artifact another tool already produced, hash it and record it. Ten sites build
  their JS with Rollup and Terser; adopting that output gives it cache-busting without re-bundling it
  and changing its semantics.

## Usage

```js
// assets.build.mjs
import {buildAssets} from '@n3oltd/asset-build';

await buildAssets({
    outDir: 'wwwroot/assets/bundles',
    publicPath: '/assets/bundles',
    bundles: {
        main: {
            css: [{compile: 'template/css/all.scss', out: 'css/template.css'}],
            js: [{adopt: 'wwwroot/assets/js/site.js', out: 'js/site.js', module: false}]
        }
    }
});
```

`out` is the path within `outDir`; the content hash is inserted before the extension, so
`js/site.js` is written as `js/site.3f2a91c4.js`.

Emit into a folder named `bundles`. The repo's `.gitignore` keys on that name to keep generated,
content-hashed output out of git.

## Options

Set on an entry, a bundle, or the whole config — the most specific wins.

| Option | Default | Purpose |
| --- | --- | --- |
| `sourcemaps` | `'none'` | `'external'` emits a `.map` and appends a `sourceMappingURL` comment |
| `integrity` | `false` | Emits a `sha384-` digest into the manifest, rendered as an `integrity` attribute |
| `targets` | *(none)* | Lightning CSS browser targets, for prefixing and syntax downleveling |

`module` is per JS entry and defaults to `false`. It controls whether the tag helper renders
`type="module"`. An iife bundle must load as a classic script — marking it a module would defer it,
scope it and force strict mode.

Sourcemaps and integrity are both off by default. A published `.map` exposes original source, and
integrity breaks the page outright if anything transforms the asset in transit.
