# N3O.Umbraco.ReactRuntime

**Package:** `@n3o/react-runtime`
**Source:** `src/N3O.Umbraco.Cms/Build/N3O.Umbraco.ReactRuntime/`
**Output:** `src/N3O.Umbraco.Cms/wwwroot/App_Plugins/N3O.Umbraco.ReactRuntime/`

---

## What it is / its role

React is a JavaScript UI library. Like many JavaScript libraries it is distributed as CommonJS — a format designed for Node.js, not browsers. If every backoffice plugin bundled its own copy of React, the browser would load React once per plugin, each copy would hold its own internal state, and features that depend on a single React instance (context, hooks, refs, the reconciler) would silently break across plugin boundaries.

`N3O.Umbraco.ReactRuntime` solves this by bundling React **once** and publishing the result to `App_Plugins`. Every other plugin references React via the browser's **import map** (a JSON table that redirects `import 'react'` to a URL), so they all share the exact same loaded instance — the same pattern the Umbraco backoffice itself uses for Lit and other shared libraries.

Think of it as an ASP.NET shared assembly that is loaded once by the AppDomain and referenced by every plugin, except the loading mechanism is the browser import map rather than .NET's assembly resolver.

The package has **no C# component** — it is a pure build-time npm package that produces three `.js` files and one `umbraco-package.json`.

---

## Files

| File | Role |
|------|------|
| `package.json` | npm package manifest; names the package `@n3o/react-runtime`; declares the two-step build script |
| `tsconfig.json` | TypeScript config; inherits shared base from `@n3o/build/tsconfig` |
| `vite.config.react.ts` | Vite config for the **first** build step — bundles React itself into `react.js` |
| `vite.config.rest.ts` | Vite config for the **second** build step — builds `react-dom.js` and `react-jsx-runtime.js` with React kept external |
| `src/react.js` | Shim source: imports React as default and re-exports its full API as named ES module exports |
| `src/react-dom.js` | Shim source: imports ReactDOM as default and re-exports its full API; also re-exports `createRoot`/`hydrateRoot` from `react-dom/client` |
| `src/react-jsx-runtime.js` | Shim source: re-exports `jsx`, `jsxs`, and `Fragment` from `react/jsx-runtime` |
| `wwwroot/.../umbraco-package.json` | Umbraco package manifest; declares the import-map entries that redirect `react`, `react-dom`, `react/jsx-runtime`, and `react-dom/client` to the built files |

---

## File-by-file walkthrough

### `package.json`

```json
{
    "name": "@n3o/react-runtime",
    "version": "1.0.0",
    "private": true,
    "type": "module",
    "scripts": {
        "build": "vite build -c vite.config.react.ts && vite build -c vite.config.rest.ts"
    },
    "dependencies": {
        "react": "^19.0.0",
        "react-dom": "^19.0.0"
    },
    "devDependencies": {
        "vite": "^6.0.0"
    }
}
```

- **`"name": "@n3o/react-runtime"`** — the scoped package name used to reference this package within the npm workspace. Other packages in the workspace do not depend on this package name; they reference React through the browser import map at runtime. The name is recorded here mainly for workspace tooling.
- **`"private": true`** — never published to a public registry (same purpose as a `<IsPackable>false</IsPackable>` NuGet property).
- **`"type": "module"`** — tells Node.js that `.js` files in this package use ES module syntax (`import`/`export`), not CommonJS (`require`). Required because Vite config files and shim sources use ES module syntax.
- **`"build": "vite build -c vite.config.react.ts && vite build -c vite.config.rest.ts"`** — the build runs in **two sequential steps** separated by `&&`. Step 1 bundles React. Step 2 builds react-dom and the JSX runtime with React marked external. They must be separate because step 2's `external: ['react']` declaration must refer to the same module specifier that step 1 wrote to the import map — running them in one Vite config would conflate the entries.
- **`"dependencies": { "react": "...", "react-dom": "..." }`** — React is a real `dependency` (not `devDependency`) because the shim sources `import` from it at build time and the output bundles it.

---

### `tsconfig.json`

```json
{
    "extends": "@n3o/build/tsconfig",
    "include": ["src", "vite.config.react.ts", "vite.config.rest.ts"]
}
```

Inherits all compiler settings from the shared base (see [BuildConfig](buildconfig.md) and [../concepts/05-vite-and-the-build.md](../concepts/05-vite-and-the-build.md)). The two vite config files are included explicitly so TypeScript type-checks them as part of the project.

---

### `vite.config.react.ts` — bundling React itself

```typescript
import { defineConfig } from 'vite';

export default defineConfig({
    define: { 'process.env.NODE_ENV': JSON.stringify('production') },
    build: {
        outDir: '../../wwwroot/App_Plugins/N3O.Umbraco.ReactRuntime',
        emptyOutDir: false,
        sourcemap: false,
        lib: { entry: { react: 'src/react.js' }, formats: ['es'] },
        rollupOptions: { output: { entryFileNames: '[name].js', chunkFileNames: 'react-internals-[hash].js' } },
    },
});
```

Key points line by line:

- **`define: { 'process.env.NODE_ENV': JSON.stringify('production') }`** — React's source code contains `process.env.NODE_ENV` checks (`if (process.env.NODE_ENV !== 'production') { ... warn ... }`). `process` does not exist in browsers; this `define` does a compile-time text substitution that replaces the expression with the string `"production"` so the browser-safe code path is taken and dead code (dev warnings) is eliminated by the bundler. Think of it as a `#if RELEASE` preprocessor directive in C#.
- **`outDir: '../../wwwroot/App_Plugins/N3O.Umbraco.ReactRuntime'`** — the output folder relative to the package root. This is a path inside the C# project's static web assets folder, which ASP.NET Core will serve at `/App_Plugins/N3O.Umbraco.ReactRuntime/`.
- **`emptyOutDir: false`** — do not wipe the output folder before building. The two build steps share the same `outDir`; each writes its own files. If the first step wiped the folder the second step could not run safely, and vice versa.
- **`lib: { entry: { react: 'src/react.js' }, formats: ['es'] }`** — "library mode" in Vite (the equivalent of building a class library DLL rather than a full application). The entry name `react` controls the output filename (see `entryFileNames` below). `formats: ['es']` means emit only the ES module format — no CommonJS, no UMD.
- **`entryFileNames: '[name].js'`** — the entry named `react` becomes `react.js`.
- **`chunkFileNames: 'react-internals-[hash].js'`** — React internally uses a package called `scheduler`. When Vite bundles React it may split `scheduler` into a separate chunk; this names it `react-internals-<hash>.js`.
- **No `external` declaration** — React and its scheduler are bundled in full. This is the one file that actually contains React code; every other file will externalise it.

---

### `vite.config.rest.ts` — building react-dom and jsx-runtime with React external

```typescript
export default defineConfig({
    define: { 'process.env.NODE_ENV': JSON.stringify('production') },
    build: {
        outDir: '../../wwwroot/App_Plugins/N3O.Umbraco.ReactRuntime',
        emptyOutDir: false,
        sourcemap: false,
        lib: {
            entry: {
                'react-jsx-runtime': 'src/react-jsx-runtime.js',
                'react-dom': 'src/react-dom.js',
            },
            formats: ['es'],
        },
        rollupOptions: {
            external: ['react', /^@umbraco/],
            output: { entryFileNames: '[name].js', chunkFileNames: 'react-dom-internals-[hash].js' },
        },
    },
});
```

Key differences from `vite.config.react.ts`:

- **Two entries** — `react-jsx-runtime` and `react-dom` become two separate output files.
- **`external: ['react', /^@umbraco/]`** — this is the critical instruction. When Rollup (the bundler Vite uses internally) encounters `import ... from 'react'` inside `react-dom`, it does **not** inline that code; instead it leaves the `import 'react'` statement in the output. At browser runtime the import map resolves `'react'` to the already-loaded `react.js` from step 1. This ensures `react-dom` and `react` share exactly one React instance. The `/^@umbraco/` regex excludes all Umbraco backoffice packages for the same reason — they are provided by Umbraco's own import map.
- **`chunkFileNames: 'react-dom-internals-[hash].js'`** — if react-dom is split into internal chunks they are named separately from react's scheduler chunk.

---

### `src/react.js` — the React shim

```javascript
// React, bundled once and re-exported as a single shared ESM module for the backoffice import map.
// NOTE: `export * from 'react'` does NOT carry React's CommonJS named exports through Vite's lib
// build — only `default` survives — so the public API is re-exported explicitly off the default
// object (this mirrors the react-dom shim's explicit re-exports). Keep in sync with the React major.
import React from 'react';

export default React;

export const {
    Children,
    Component,
    Fragment,
    // ... useState, useEffect, etc.
} = React;
```

**Why not just `export * from 'react'`?**

React is published as a CommonJS module (`module.exports = { useState, useEffect, ... }`). When Vite/Rollup converts CommonJS to ES modules it wraps the whole export object as the `default` export, and also tries to re-export each property as a named export. However, in library build mode (`formats: ['es']`) with tree-shaking enabled, Rollup **drops** the synthesised named re-exports because it cannot statically analyse which names a CommonJS `module.exports` object actually has — only the `default` export survives.

The consequence: if the shim used `export * from 'react'`, a consumer writing `import { useState } from 'react'` would get `undefined` at runtime, because `useState` would not be a named export of the compiled shim file.

The fix is to explicitly destructure the names off the default object:

```javascript
export const { useState, useEffect, useContext, /* ... */ } = React;
```

This is plain ES module syntax that Rollup can see at compile time and preserve. Think of it as the difference between a C# interface that lists every member explicitly versus one that tries to dynamically expose an object's properties through reflection — the static list survives compilation; the dynamic one does not.

The file lists every name in React 19's public API. If React adds a new export in a future version, it must be added here.

---

### `src/react-dom.js` — the ReactDOM shim

```javascript
import ReactDOM from 'react-dom';

export default ReactDOM;

export const {
    createPortal,
    flushSync,
    // ... other ReactDOM exports ...
} = ReactDOM;

export { createRoot, hydrateRoot } from 'react-dom/client';
```

Same pattern as `react.js`, with two points worth noting:

- **`react` stays external** (declared in `vite.config.rest.ts`). ReactDOM imports React internally; the `external` declaration means that import resolves at runtime via the import map to the same `react.js` file, not to an inlined copy.
- **`export { createRoot, hydrateRoot } from 'react-dom/client'`** — `react-dom/client` is a subpath of the `react-dom` package. The import map entry `"react-dom/client": ".../react-dom.js"` (see below) means that `import { createRoot } from 'react-dom/client'` is also resolved to this same built file. The re-export here ensures those names are available regardless of whether the consumer imports from `'react-dom'` or `'react-dom/client'`.

---

### `src/react-jsx-runtime.js` — the JSX runtime shim

```javascript
import jsxRuntime from 'react/jsx-runtime';

export const jsx = jsxRuntime.jsx;
export const jsxs = jsxRuntime.jsxs;
export const Fragment = jsxRuntime.Fragment;
```

When TypeScript or Babel compiles JSX with the "automatic" transform (the modern approach, used by setting `"jsx": "react-jsx"` in tsconfig), the compiler does **not** insert `import React from 'react'` at the top of each file. Instead it inserts:

```javascript
import { jsx as _jsx } from 'react/jsx-runtime';
```

`react/jsx-runtime` is a separate sub-entry of the react package that exports the functions the JSX transform uses. This shim re-exports those three names explicitly (same reason as above — `export * from 'react/jsx-runtime'` would not survive the Vite lib build) and marks `react` external so it shares the single instance.

---

### `wwwroot/App_Plugins/N3O.Umbraco.ReactRuntime/umbraco-package.json`

```json
{
    "$schema": "http://json.schemastore.org/umbraco-package.json",
    "id": "N3O.Umbraco.ReactRuntime",
    "name": "N3O React Runtime",
    "version": "17.0.0",
    "importmap": {
        "imports": {
            "react": "/App_Plugins/N3O.Umbraco.ReactRuntime/react.js",
            "react/jsx-runtime": "/App_Plugins/N3O.Umbraco.ReactRuntime/react-jsx-runtime.js",
            "react-dom": "/App_Plugins/N3O.Umbraco.ReactRuntime/react-dom.js",
            "react-dom/client": "/App_Plugins/N3O.Umbraco.ReactRuntime/react-dom.js"
        }
    }
}
```

This is the **runtime wiring**. Umbraco reads every `umbraco-package.json` from all `App_Plugins` folders at startup and merges their `importmap.imports` sections into a single `<script type="importmap">` block in the backoffice HTML page.

When the browser encounters `import { useState } from 'react'` anywhere in any plugin's JavaScript, it looks up `'react'` in this table and fetches `/App_Plugins/N3O.Umbraco.ReactRuntime/react.js`. That file is fetched and cached exactly once; every subsequent `import 'react'` from any other plugin gets the cached module.

Mappings explained:

| Import specifier | Resolves to |
|-----------------|-------------|
| `react` | `react.js` — the bundled React with all named exports |
| `react/jsx-runtime` | `react-jsx-runtime.js` — `jsx`, `jsxs`, `Fragment` for the automatic JSX transform |
| `react-dom` | `react-dom.js` — ReactDOM named exports |
| `react-dom/client` | `react-dom.js` — same file; `createRoot` and `hydrateRoot` are re-exported from it |

Both `react-dom` and `react-dom/client` map to the same output file because `react-dom.js` already re-exports the `client` subpath's exports.

---

## How it fits together: the full picture

```
Build time
──────────
src/react.js ──[vite.config.react.ts]──► react.js (React bundled in full)
src/react-dom.js ──[vite.config.rest.ts]──► react-dom.js (react external)
src/react-jsx-runtime.js ──[vite.config.rest.ts]──► react-jsx-runtime.js (react external)

Runtime
───────
Browser loads backoffice HTML
  └─ Umbraco injects <script type="importmap"> from all umbraco-package.json files
       "react" → /App_Plugins/.../react.js
       "react-dom" → /App_Plugins/.../react-dom.js
       ...
  └─ Plugin JS runs: import { useState } from 'react'
       Browser resolves 'react' via import map → fetches react.js (once, cached)
  └─ Second plugin: import { createRoot } from 'react-dom/client'
       Browser resolves via import map → fetches react-dom.js (once, cached)
       react-dom.js: import 'react' → resolved by map → the SAME cached module
```

This is exactly the C# analogy of a shared GAC assembly: loaded once, referenced by all.

---

## Concepts demonstrated

- **ES modules and import maps** — the entire mechanism depends on the browser's native import map. See [../concepts/04-es-modules-and-import-maps.md](../concepts/04-es-modules-and-import-maps.md).
- **The N3O bridge pattern** — this package is the React half of the "provide shared runtime via import map" pattern. See [../concepts/10-the-n3o-bridge-pattern.md](../concepts/10-the-n3o-bridge-pattern.md).
- **Vite library builds** — both vite configs use `build.lib` (library mode). See [../concepts/05-vite-and-the-build.md](../concepts/05-vite-and-the-build.md).
- **React** — what React is and why it needs a single shared instance. See [../concepts/08-react.md](../concepts/08-react.md).

---

## Gotchas

**Do not add a new React export without updating `src/react.js`.**
When React releases a new hook or utility (e.g. `useOptimistic` was new in React 19), it must be added to the explicit destructuring list in `src/react.js`. If you omit it, `import { useOptimistic } from 'react'` will succeed at the TypeScript compile step (types come from `@types/react`) but `useOptimistic` will be `undefined` at runtime.

**The `react` package must only appear in `dependencies`, not `devDependencies`.**
`vite.config.react.ts` bundles `react` into the output. Vite/Rollup must be able to resolve the actual package source at build time. If it were a `devDependency` it would still work locally but would break in CI environments that run `npm install --production`.

**Do not add `react` to `external` in `vite.config.react.ts`.**
The point of that config is to bundle React. Adding `external: ['react']` would produce an empty shim that immediately imports itself.

**Both vite configs write to the same `outDir` with `emptyOutDir: false`.**
Changing either to `emptyOutDir: true` will cause one step to delete the output of the other.

**`react-dom/client` is mapped to the same file as `react-dom`.**
This is intentional. Do not split them into separate output files without also updating the import-map entries in `umbraco-package.json` — and updating `src/react-dom.js` to not re-export the client API.

**The build does not use `@n3o/build`'s `n3oPluginConfig` preset.**
`N3O.Umbraco.ReactRuntime` pre-dates and underpins `n3oPluginConfig`. Its two Vite configs are written directly. The preset (`@n3o/build`) is for plugin apps; the runtime is the infrastructure those apps depend on. See [buildconfig.md](buildconfig.md).
