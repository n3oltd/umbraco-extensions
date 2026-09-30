# 05 — Vite and the Build

**Prerequisites:** [04 — ES Modules and Import Maps](./04-es-modules-and-import-maps.md), [03 — Node, npm, and the Workspace](./03-node-npm-and-the-workspace.md).

---

## What is a build tool?

In .NET, you write C# source files and `dotnet build` compiles them to a `.dll`. The compiler (`Roslyn`) resolves references, type-checks, and produces an optimised binary that the runtime loads.

For the browser, you write TypeScript + JSX source files and a **build tool** compiles them to plain JavaScript files that the browser understands. Vite is that build tool. The mapping:

| .NET                           | Frontend                          |
|--------------------------------|-----------------------------------|
| `dotnet build` / MSBuild       | `vite build` / npm run build      |
| Roslyn (compiler)              | esbuild (transpiler, inside Vite) |
| Rollup (linker, inside Vite)   | (no direct equivalent — see below)|
| `.dll` output artefact         | `.js` output file(s)              |
| `bin/Release/net10.0/`         | `wwwroot/App_Plugins/<plugin>/`   |

Vite itself is two tools in one:
1. **Dev server** — a fast hot-reloading server used during active development. Not used in this repo's normal `dotnet build` flow.
2. **Bundler** — invoked by `vite build`. Bundles and transpiles source files into the output `.js`. This is what runs in CI and in `dotnet build`.

---

## `defineConfig`

Every `vite.config.ts` calls `defineConfig(...)` from the `vite` package. This is the typed entry point — it accepts a plain JavaScript object and returns it unchanged, but gives TypeScript full IntelliSense over every option. Think of it as a strongly-typed settings record.

---

## Library mode vs app mode

Vite has two primary build modes:

**App mode** (the default) — builds an HTML-entry-point application: it produces an `index.html` plus hashed JS/CSS bundles. This is for standalone web apps.

**Library mode** — builds a reusable JavaScript module from a TypeScript/JS entry point, with no `index.html`. This is what every N3O plugin uses, because the output isn't a whole page — it's a `.js` file that the Umbraco backoffice (itself an app) loads at runtime.

Library mode is activated by the `build.lib` key:

```js
build: {
    lib: {
        entry: entries,   // { 'output-name': 'src/entry.ts', ... }
        formats: ['es'],  // output format: 'es' = ESM (.js files)
    },
    ...
    rollupOptions: {
        output: { entryFileNames: '[name].js' },
    },
}
```

- `entry` — the TypeScript file(s) Vite starts from. Every `import` it encounters is followed and bundled in (unless it is external — see below).
- `formats: ['es']` — output as ESM (`export` / `import`). The browser's native module format (see [04 — ES Modules and Import Maps](./04-es-modules-and-import-maps.md)).
- `entryFileNames: '[name].js'` — output file gets the entry key name, no content hash in the filename. A hash would break the manifest (`umbraco-package.json`) that references the file by a fixed name.

---

## Externals — "don't bundle, expect at runtime"

The single most important build concept for this codebase.

In .NET terms: imagine telling the compiler "don't embed `Newtonsoft.Json` — assume it will be in the GAC at runtime." The compiler then emits a reference but no copy. Externals are exactly this.

```js
rollupOptions: {
    external: [/^@umbraco/, 'react', 'react-dom', 'react-dom/client', 'react/jsx-runtime'],
}
```

Any module matching these patterns is **excluded from the bundle**. The output JS will contain `import { ... } from 'react'` literally — the bundle does not contain React's code. The browser resolves that bare specifier at runtime via the import map (see [04 — ES Modules and Import Maps](./04-es-modules-and-import-maps.md)).

Why this matters:
- Every plugin omits React and `@umbraco-cms/*` from its output, keeping bundles small.
- All plugins share the **one copy** of React loaded from `N3O.Umbraco.ReactRuntime`. If each bundled its own copy, React's rules-of-hooks would break (React throws if there are two instances).
- `@umbraco-cms/*` is provided by the backoffice itself at runtime — plugins never need to ship it.

---

## The shared `@n3o/build` preset

Rather than duplicating the Vite config in every one of the sixteen+ plugin apps, the repo defines a single shared preset at:

```
src/N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/vite-config.js
```

It exports one function, `n3oPluginConfig`, which every app calls from its `vite.config.ts`:

```js
// src/N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/vite-config.js

import { defineConfig } from 'vite';

export function n3oPluginConfig(options) {
    const { entries, outDir, react = false, additionalExternals = [], sourcemap = true } = options;

    const external = [/^@umbraco/];

    if (react) {
        external.push('react', 'react-dom', 'react-dom/client', 'react/jsx-runtime');
    }

    external.push(...additionalExternals);

    return defineConfig({
        ...(react ? { esbuild: { jsx: 'automatic' } } : {}),
        build: {
            lib: {
                entry: entries,
                formats: ['es'],
            },
            outDir,
            emptyOutDir: false,
            sourcemap,
            rollupOptions: {
                external,
                output: { entryFileNames: '[name].js' },
            },
        },
    });
}
```

This package is published within the npm workspace as `@n3o/build` (its `package.json` `name` is `"@n3o/build"`), so any plugin app can import it by name.

### Parameters

| Parameter            | Type                            | Meaning                                                              |
|---------------------|---------------------------------|----------------------------------------------------------------------|
| `entries`           | `{ 'out-name': 'src/file.ts' }` | Entry points — the TypeScript source files to compile                |
| `outDir`            | `string`                        | Output directory (relative to the `vite.config.ts`)                  |
| `react`             | `boolean` (default `false`)     | Adds React/JSX-runtime to externals; enables JSX transform           |
| `additionalExternals` | `string[]` (default `[]`)    | Extra modules to exclude from the bundle (e.g. `@n3o/backoffice-core`) |
| `sourcemap`         | `boolean` (default `true`)      | Whether to emit `.js.map` files for debugging                        |

---

## A real app config — Data Export

```ts
// src/Data/N3O.Umbraco.Data.StaticAssets/Apps/N3O.Umbraco.Data.Export/vite.config.ts

import { n3oPluginConfig } from '@n3o/build';

export default n3oPluginConfig({
    entries: {
        'N3O.Umbraco.Data.Export/data-export': 'src/data-export.ts',
    },
    outDir: '../../wwwroot/App_Plugins',
    react: true,
    additionalExternals: ['@n3o/backoffice-core'],
});
```

Walking through each line:

- `entries` — one entry: `src/data-export.ts`. The key `'N3O.Umbraco.Data.Export/data-export'` becomes the output path **relative to `outDir`**, producing `wwwroot/App_Plugins/N3O.Umbraco.Data.Export/data-export.js`.
- `outDir: '../../wwwroot/App_Plugins'` — two levels up from `Apps/N3O.Umbraco.Data.Export/`, landing at `Apps/../wwwroot/App_Plugins` which is the .NET project's `wwwroot`. Umbraco's static-web-assets pipeline then serves everything under `wwwroot/App_Plugins/`.
- `react: true` — React is used in this plugin; exclude it from the bundle.
- `additionalExternals: ['@n3o/backoffice-core']` — the shared auth-fetch library is also external (loaded via the import map at runtime).

The Scheduler is simpler — no `additionalExternals`:

```ts
// src/Scheduler/N3O.Umbraco.Scheduler.StaticAssets/Apps/vite.config.ts

import { n3oPluginConfig } from '@n3o/build';

export default n3oPluginConfig({
    entries: { 'scheduler-dashboard': 'src/scheduler-dashboard.ts' },
    outDir: '../wwwroot/App_Plugins/N3O.Umbraco.Scheduler',
    react: true,
});
```

Here `outDir` is the plugin's own subfolder directly — the entry key has no path component, so the output is `wwwroot/App_Plugins/N3O.Umbraco.Scheduler/scheduler-dashboard.js`.

---

## `emptyOutDir: false`

By default Vite deletes `outDir` before writing. This would wipe the entire `wwwroot/App_Plugins/` on every build, deleting other plugins' output. `emptyOutDir: false` disables that — each plugin writes only its own files and leaves the rest alone.

---

## Sourcemaps

`sourcemap: true` (the default in the preset) tells Vite to emit a `<file>.js.map` alongside every output JS file. A sourcemap is a JSON file that maps positions in the compiled output back to the original TypeScript lines. With sourcemaps, browser DevTools show the original TypeScript source when you set a breakpoint or inspect a stack trace — exactly like `.pdb` files for .NET.

---

## JSX and `esbuild: { jsx: 'automatic' }`

JSX is the XML-like syntax used in React components:

```tsx
return <div className="header">{title}</div>;
```

JSX is not valid JavaScript. It must be **compiled** to `React.createElement(...)` calls. Before React 17 this required `import React from 'react'` at the top of every file containing JSX. The **automatic JSX transform** (React 17+) removes that requirement — the compiler inserts the correct import from `react/jsx-runtime` automatically.

Setting `esbuild: { jsx: 'automatic' }` tells Vite's esbuild transpiler to use the automatic transform. Files ending in `.tsx` are treated as containing JSX. You do not need to write `import React from 'react'` at the top of a component file.

The `react/jsx-runtime` module used by the automatic transform is listed as an external (when `react: true`), so the import the compiler inserts refers to the runtime copy — not a bundled copy.

---

## The `?inline` CSS import

Normal CSS imports in Vite cause the styles to be injected into the page's `<head>` as a `<style>` tag. This does not work inside a **Shadow DOM** (see [06 — Web Components and Shadow DOM](./06-web-components-and-shadow-dom.md)), because Shadow DOM is encapsulated — styles in `<head>` don't reach inside it.

The `?inline` query suffix tells Vite to instead export the CSS as a raw string:

```ts
// src/Scheduler/N3O.Umbraco.Scheduler.StaticAssets/Apps/src/scheduler-dashboard.ts
import cssText from './scheduler-dashboard-app.css?inline';
```

After this import, `cssText` is a `string` containing the full CSS text. The custom element can then inject it directly into its shadow root using `adoptedStyleSheets` (explained in [06 — Web Components and Shadow DOM](./06-web-components-and-shadow-dom.md)):

```ts
const sheet = new CSSStyleSheet();
sheet.replaceSync(cssText);
shadow.adoptedStyleSheets = [sheet];
```

TypeScript doesn't know about the `?inline` suffix by default, so the shared `@n3o/build` preset provides an ambient type declaration at `src/N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/vite-env.d.ts`:

```ts
declare module '*.css?inline' {
    const css: string;
    export default css;
}
```

This tells TypeScript "any import ending in `?inline` from a `.css` file has a `string` default export." All apps inherit this via the shared `tsconfig` base (`base.json`).

---

## How `dotnet build` triggers the JS build

There is no Node.js process running at Umbraco runtime — Node is a build-time tool only. The integration between MSBuild and npm happens in two places.

### 1. Root `Directory.Build.targets` (repo root)

```xml
<!-- D:/AI Migration Test/N3O.Umbraco/Directory.Build.targets -->

<Target Name="RestoreClientWorkspace"
        Condition="'$(MSBuildProjectName)' == 'N3O.Umbraco.Extensions'"
        BeforeTargets="BeforeBuild"
        Inputs="$(MSBuildThisFileDirectory)src\package-lock.json"
        Outputs="$(MSBuildThisFileDirectory)src\node_modules\.n3o-restore-stamp">
    <Exec Command="npm ci" WorkingDirectory="$(MSBuildThisFileDirectory)src" />
    <Touch Files="$(MSBuildThisFileDirectory)src\node_modules\.n3o-restore-stamp" AlwaysCreate="true" />
</Target>

<Target Name="BuildClientApp"
        Condition="Exists('$(MSBuildProjectDirectory)\Apps') And '$(MSBuildProjectName)' != 'N3O.Umbraco.Cms'"
        BeforeTargets="BeforeBuild"
        DependsOnTargets="ResolveProjectReferences">
    <ItemGroup>
        <_N3OClientAppPackage Include="$(MSBuildProjectDirectory)\Apps\**\package.json"
                              Exclude="$(MSBuildProjectDirectory)\Apps\**\node_modules\**" />
    </ItemGroup>
    <Exec Condition="'@(_N3OClientAppPackage)' != ''"
          Command="npm run build"
          WorkingDirectory="%(_N3OClientAppPackage.RootDir)%(_N3OClientAppPackage.Directory)" />
</Target>
```

`Directory.Build.targets` is an MSBuild file that is automatically imported by every `.csproj` in the directory tree — exactly like a base class. Two targets defined here:

- **`RestoreClientWorkspace`** runs only when building `N3O.Umbraco.Extensions` (the graph root). It runs `npm ci` in the `src/` workspace root to install all packages from `package-lock.json`. The `Inputs`/`Outputs` stamping makes this **incremental** — it only re-runs when `package-lock.json` changes, not on every build.

- **`BuildClientApp`** runs for every project that has an `Apps/` folder (except `N3O.Umbraco.Cms`, which has its own target — see below). It globs `Apps/**/package.json`, skipping `node_modules`, and calls `npm run build` once per match. Each `package.json`'s `build` script runs `tsc --noEmit && vite build` — TypeScript type-check first, then Vite.

### 2. `N3O.Umbraco.Cms.csproj` (inline target)

`N3O.Umbraco.Cms` is excluded from `BuildClientApp` because it also builds the React runtime (`Build/N3O.Umbraco.ReactRuntime`) which lives under `Build/` not `Apps/`. Its `.csproj` contains an equivalent inline `BuildClientApps` target:

```xml
<!-- src/N3O.Umbraco.Cms/N3O.Umbraco.Cms.csproj (excerpt) -->
<ItemGroup>
    <N3OClientApp Include="$(MSBuildProjectDirectory)\Apps\*\package.json" />
    <N3OClientApp Include="$(MSBuildProjectDirectory)\Build\N3O.Umbraco.ReactRuntime\package.json" />
</ItemGroup>
<Target Name="BuildClientApps" BeforeTargets="BeforeBuild" DependsOnTargets="ResolveProjectReferences">
    <Exec Command="npm run build" WorkingDirectory="%(N3OClientApp.RootDir)%(N3OClientApp.Directory)" />
</Target>
```

This builds all `Apps/*` sub-apps **and** the React runtime in one target.

### Sequencing guarantee

`DependsOnTargets="ResolveProjectReferences"` ensures that when a `*.StaticAssets` project runs `BuildClientApp`, MSBuild has already resolved its references — which means `N3O.Umbraco.Extensions` (the workspace root, which runs `npm ci`) has already been built. `node_modules` therefore exists before any `vite build` is attempted. A guard target `EnsureClientWorkspaceRestored` enforces this and fails fast with a clear message if `node_modules` is missing.

### Build-time only

At Umbraco runtime (IIS / Kestrel serving the site), Node.js is not present and is not needed. The compiled `.js` files in `wwwroot/App_Plugins/` are static files served directly by ASP.NET Core's static-file middleware. The build step is a compile step, not a runtime dependency.

---

## Quick reference — preset parameters and their effects

| `n3oPluginConfig` option | Vite config key it sets                       | Effect                                              |
|--------------------------|-----------------------------------------------|-----------------------------------------------------|
| `entries`                | `build.lib.entry`                             | Source TypeScript entry files                       |
| `outDir`                 | `build.outDir`                                | Where compiled JS lands                             |
| `react: true`            | `rollupOptions.external` + `esbuild.jsx`      | Excludes React; enables automatic JSX transform     |
| `additionalExternals`    | `rollupOptions.external` (appended)           | Any extra modules excluded from the bundle          |
| `sourcemap` (default `true`) | `build.sourcemap`                         | Emits `.js.map` debug mapping files                 |
| always set               | `build.lib.formats: ['es']`                   | ESM output                                          |
| always set               | `build.emptyOutDir: false`                    | Never wipe `wwwroot/App_Plugins/`                   |
| always set               | `rollupOptions.output.entryFileNames: '[name].js'` | Deterministic filenames (no content hash)      |

---

**Next:** [06 — Web Components and Shadow DOM](./06-web-components-and-shadow-dom.md)
