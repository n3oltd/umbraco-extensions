# N3O.Umbraco.BuildConfig (`@n3o/build`)

**Package:** `@n3o/build`
**Source:** `src/N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/`
**Output:** none — this package is never built; it is pure configuration consumed at build time

---

## What it is / its role

`@n3o/build` is the shared build-configuration package for every N3O backoffice ClientApp. It contains:

1. A **Vite preset function** (`n3oPluginConfig`) that every plugin's `vite.config.ts` calls to get a standard, pre-configured Vite build with correct externals, output naming, and optional React support.
2. A **shared TypeScript base config** (`base.json`) that every plugin's `tsconfig.json` extends, so all apps compile to the same target, use the same module resolution, and share the same compiler flags.
3. An **ambient CSS type declaration** (`vite-env.d.ts`) that teaches TypeScript what `import styles from './foo.css?inline'` means — propagated to all apps automatically through the `extends` chain.

The C# analogy is a `.props` / `.targets` file in a shared NuGet package (a "build-only" NuGet package that contributes no runtime assemblies but configures the build of every project that references it). Or, more precisely, it is equivalent to a `Directory.Build.props` that every csproj inherits.

`@n3o/build` itself has **no `build` script** and produces **no output files**. Its files are read directly by the tools that consume them (Vite reads `vite-config.js`; the TypeScript compiler reads `base.json` through `extends`).

---

## Files

| File | Role |
|------|------|
| `package.json` | npm package manifest; declares the `exports` map that controls what consumers can import |
| `base.json` | Shared TypeScript compiler options; every app's `tsconfig.json` extends this |
| `vite-config.js` | The `n3oPluginConfig(options)` preset function; every app's `vite.config.ts` calls this |
| `vite-config.d.ts` | TypeScript declaration file for `vite-config.js`; defines the `N3oPluginConfigOptions` interface |
| `vite-env.d.ts` | Ambient module declaration for `*.css?inline` imports |

---

## File-by-file walkthrough

### `package.json`

```json
{
    "name": "@n3o/build",
    "private": true,
    "type": "module",
    "exports": {
        "./tsconfig": "./base.json",
        "./vite-env": "./vite-env.d.ts",
        ".": {
            "types": "./vite-config.d.ts",
            "import": "./vite-config.js"
        }
    }
}
```

This package has no `scripts` and no `dependencies`. It is pure configuration.

**The `exports` map** is the Node.js "package exports" feature (analogous to a `<PackageReference>` that exposes multiple assets via different include paths). It controls which paths consumers can import from this package:

| Consumer writes | `exports` entry | Resolves to |
|----------------|-----------------|-------------|
| `import { n3oPluginConfig } from '@n3o/build'` | `"."` | `vite-config.js` (runtime), `vite-config.d.ts` (types) |
| `"extends": "@n3o/build/tsconfig"` in tsconfig.json | `"./tsconfig"` | `base.json` |
| `/// <reference types="@n3o/build/vite-env" />` or the `files` array in base.json | `"./vite-env"` | `vite-env.d.ts` |

Without this map, Node.js would not know which file to load. Attempting `import from '@n3o/build/tsconfig'` without the `exports` entry would fail with a "package path is not exported" error — the equivalent of trying to reference an internal namespace that has no public API.

**`"type": "module"`** — both `vite-config.js` and any consumer's vite config use ES module syntax (`export function`, `import`). This field ensures Node.js treats `.js` files as ES modules.

**No `devDependencies` or `dependencies`** — Vite itself is declared as a `devDependency` at the workspace root (`src/package.json`), so it is available to all packages without being declared in each one. `@n3o/build` imports from `'vite'` in `vite-config.js`; that import resolves through the workspace's hoisted `node_modules`.

---

### `base.json` — shared TypeScript compiler options

```json
{
    "compilerOptions": {
        "target": "ES2022",
        "module": "ESNext",
        "lib": ["ES2022", "DOM", "DOM.Iterable"],
        "moduleResolution": "bundler",
        "useDefineForClassFields": false,
        "experimentalDecorators": true,
        "skipLibCheck": true,
        "isolatedModules": true,
        "moduleDetection": "force",
        "noEmit": true,
        "strict": true,
        "noUnusedLocals": true,
        "noUnusedParameters": true,
        "noFallthroughCasesInSwitch": true,
        "types": ["@umbraco-cms/backoffice/extension-types"]
    },
    "files": ["vite-env.d.ts"]
}
```

Each setting explained for a C# developer:

| Setting | Value | What it means |
|---------|-------|---------------|
| `target` | `"ES2022"` | Compile down to ES2022 syntax. All modern browsers (and the Umbraco backoffice's minimum browser bar) support this. Like setting `<LangVersion>` and `<TargetFramework>` together. |
| `module` | `"ESNext"` | Keep `import`/`export` statements in the output; do not convert them to CommonJS `require()`. Required because the browser loads these files natively as ES modules. |
| `lib` | `["ES2022", "DOM", "DOM.Iterable"]` | Type-check against the ES2022 standard library plus browser DOM APIs. Like adding `using System` — without `"DOM"` the TypeScript compiler would not know what `document`, `HTMLElement`, or `fetch` are. |
| `moduleResolution` | `"bundler"` | Tell the TypeScript compiler to resolve imports the same way a bundler (Vite/Rollup) does, not the way Node.js does. Allows `import './foo'` without an extension and supports package `exports` maps. |
| `useDefineForClassFields` | `false` | Lit web components use TypeScript decorators to declare reactive properties. Setting this to `false` ensures decorated class fields work correctly with Lit's property system. If `true`, decorated fields would be overwritten by native class field initialisation before decorators run, breaking Lit components. |
| `experimentalDecorators` | `true` | Enable TypeScript's stage-2 decorator syntax (`@customElement`, `@property`, etc.) used by Lit. |
| `skipLibCheck` | `true` | Do not type-check the contents of `.d.ts` files in `node_modules`. Equivalent to ignoring third-party assembly warnings — type errors in packages you did not write are noise. |
| `isolatedModules` | `true` | Require each file to be type-checkable in isolation. This is required when using a transpiler (esbuild, Babel) that processes files one at a time. Vite uses esbuild internally. Equivalent to ensuring every class and interface is in a file that declares its own types rather than relying on global side-effects from other files. |
| `moduleDetection` | `"force"` | Treat every `.ts` file as a module (i.e., a file that has `import`/`export`) even if it has no import statements. Without this, TypeScript would treat files with no imports as global scripts, causing type collisions between files. |
| `noEmit` | `true` | TypeScript does not produce `.js` output files; Vite (using esbuild) handles transpilation. TypeScript's role here is type-checking only — the same as running `dotnet build` with `<EmitCompilerGeneratedFiles>false</EmitCompilerGeneratedFiles>` when another tool handles the actual code generation. |
| `strict` | `true` | Enables all strict type checks: `strictNullChecks`, `strictFunctionTypes`, etc. Equivalent to `<Nullable>enable</Nullable>` and the full suite of Roslyn analysers. |
| `noUnusedLocals`, `noUnusedParameters` | `true` | Compiler errors for unused variables and parameters — equivalent to treating CA1801/CS0168 as errors. |
| `noFallthroughCasesInSwitch` | `true` | Error on switch cases that fall through without a `break` or `return`. |
| `types` | `["@umbraco-cms/backoffice/extension-types"]` | Auto-include the Umbraco backoffice's ambient type declarations. These declare the `UmbExtensionRegistry`, element tag names, manifest types, and other backoffice-specific globals — equivalent to adding a global `using` or a `<GlobalUsings>` entry. |

**`"files": ["vite-env.d.ts"]`**

This is the mechanism that propagates the ambient `*.css?inline` type to all inheriting apps. When `base.json` lists a file in `files`, every `tsconfig.json` that extends `base.json` inherits that file inclusion. The path in `files` is resolved relative to `base.json`'s own location (i.e., `src/N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/vite-env.d.ts`).

**Warning:** if an app's `tsconfig.json` declares its own `"files"` key (not `"include"`, but `"files"`), it will completely replace the inherited `files` list, silently dropping `vite-env.d.ts`. Use `"include"` (not `"files"`) in app-level tsconfigs.

---

### `vite-config.js` — the `n3oPluginConfig` preset

```javascript
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

This function is the central "house style" for every N3O plugin build. It returns a complete Vite configuration object. Think of it as a base class that provides a default implementation, which callers can extend via the `options` parameter.

**Parameters (`N3oPluginConfigOptions`):**

| Option | Type | Default | Purpose |
|--------|------|---------|---------|
| `entries` | `Record<string, string>` | required | Maps output filename → source file path. E.g. `{ 'my-plugin': 'src/index.ts' }` produces `my-plugin.js`. |
| `outDir` | `string` | required | Where to write output files, relative to the app's package root. Typically resolves to the project's `wwwroot/App_Plugins/...` folder. |
| `react` | `boolean` | `false` | When `true`: adds React packages to `external` and configures esbuild to use the automatic JSX transform. |
| `additionalExternals` | `(string \| RegExp)[]` | `[]` | Any extra packages to exclude from the bundle (e.g. a large third-party library provided separately). |
| `sourcemap` | `boolean` | `true` | Whether to emit `.js.map` source map files for browser debugging. |

**What every build gets regardless of options:**

- **`external: [/^@umbraco/]`** — all `@umbraco-cms/*` and `@umbraco/*` packages are excluded from the bundle. They are provided by Umbraco's own import map and must not be duplicated. This is the single most important external rule — inlining Umbraco code would produce multiple instances of the extension registry and break plugin registration.
- **`formats: ['es']`** — output only ES module format (no CommonJS/UMD/IIFE).
- **`emptyOutDir: false`** — do not wipe the output directory. Multiple plugins often share an `outDir` (e.g. several apps all write to `wwwroot/App_Plugins`).
- **`entryFileNames: '[name].js'`** — use the entry key name as the filename, with no content hash. This is intentional: Umbraco's package manifest references filenames by name, not by hash. Adding a hash would break the manifest.

**When `react: true`:**

```javascript
...(react ? { esbuild: { jsx: 'automatic' } } : {}),
```

This spread conditionally adds `esbuild.jsx: 'automatic'` to the Vite config. In "automatic" mode, esbuild inserts `import { jsx } from 'react/jsx-runtime'` at the top of every file that contains JSX — you do not need to write `import React from 'react'` in every component file. Combined with `external: [..., 'react/jsx-runtime']`, these imports resolve at runtime via the import map to the shared runtime.

Without `react: true`, JSX would not be transformed (Vite does not enable JSX by default for `.ts` files) and the app's `tsconfig.json` must set `"jsx": "react-jsx"` to match.

**Usage example — a Lit-only plugin (no React):**

```typescript
// src/N3O.Umbraco.Cms/Apps/N3O.Umbraco.DynamicListViews/vite.config.ts
import { n3oPluginConfig } from '@n3o/build';

export default n3oPluginConfig({
    entries: {
        'N3O.Umbraco.DynamicListViews/dynamic-list-view': 'src/dynamic-list-view.ts',
    },
    outDir: '../../wwwroot/App_Plugins',
});
```

The entry key `'N3O.Umbraco.DynamicListViews/dynamic-list-view'` uses a path separator — Rollup creates a sub-folder `N3O.Umbraco.DynamicListViews/` inside `outDir` and writes `dynamic-list-view.js` there.

**Usage example — a React plugin:**

```typescript
// src/Plugins/Cells/N3O.Umbraco.Cells.StaticAssets/Apps/vite.config.ts
import { n3oPluginConfig } from '@n3o/build';

export default n3oPluginConfig({
    entries: { 'n3o-cells': 'src/n3o-cells.ts' },
    outDir: '../wwwroot/App_Plugins/N3O.Umbraco.Cells',
    react: true,
});
```

Setting `react: true` adds `react`, `react-dom`, `react-dom/client`, and `react/jsx-runtime` to the external list and enables the automatic JSX transform. At runtime the browser resolves these specifiers to the files published by `N3O.Umbraco.ReactRuntime`.

---

### `vite-config.d.ts` — TypeScript declarations for the preset

```typescript
import type { UserConfig } from 'vite';

export interface N3oPluginConfigOptions {
    entries: Record<string, string>;
    outDir: string;
    react?: boolean;
    additionalExternals?: (string | RegExp)[];
    sourcemap?: boolean;
}

export function n3oPluginConfig(options: N3oPluginConfigOptions): UserConfig;
```

This is the hand-written type declaration file for `vite-config.js`. Because `vite-config.js` is plain JavaScript (not TypeScript), the compiler cannot infer its types automatically. This `.d.ts` file is the equivalent of an XML documentation / `extern` declaration that tells TypeScript callers what `n3oPluginConfig` accepts and returns.

`UserConfig` is Vite's own type for a configuration object. Returning it means the TypeScript compiler and editor can validate Vite config usage at the call site.

This file is referenced via the `"."` entry in `package.json`'s `exports` map under the `"types"` condition:

```json
".": {
    "types": "./vite-config.d.ts",
    "import": "./vite-config.js"
}
```

When TypeScript resolves `import { n3oPluginConfig } from '@n3o/build'`, it reads `vite-config.d.ts` for types and the runtime `import` resolves `vite-config.js` for execution.

---

### `vite-env.d.ts` — ambient CSS inline import type

```typescript
declare module '*.css?inline' {
    const css: string;
    export default css;
}
```

In Vite you can import a CSS file as a raw string using the `?inline` query suffix:

```typescript
import styles from './my-component.css?inline';
// styles is a string: ".my-class { color: red; }"
```

This is used by Lit web components that inject their styles into the shadow DOM:

```typescript
static styles = unsafeCSS(styles);
```

Without this `.d.ts` declaration, TypeScript would refuse to compile `import styles from './foo.css?inline'` with the error: "Cannot find module './foo.css?inline' or its corresponding type declarations." The `declare module '*.css?inline'` tells the compiler "any import matching this wildcard pattern returns a module whose default export is a string."

This file is included via `"files": ["vite-env.d.ts"]` in `base.json`, which means all apps that extend `base.json` inherit this ambient type declaration for free.

---

## How the workspace resolves `@n3o/build`

The npm workspace root at `src/package.json` declares workspaces:

```json
{
    "workspaces": [
        "**/Apps/**",
        "N3O.Umbraco.Cms/Build/*",
        "!**/bin/**",
        "!**/obj/**"
    ]
}
```

`N3O.Umbraco.Cms/Build/*` matches both `N3O.Umbraco.BuildConfig` and `N3O.Umbraco.ReactRuntime`. When `npm install` runs at the `src/` root it creates a symlink:

```
src/node_modules/@n3o/build -> src/N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig
```

So when any app writes `import { n3oPluginConfig } from '@n3o/build'` or `"extends": "@n3o/build/tsconfig"`, Node.js resolves it through that symlink to the local source files. No publishing or copying is involved. See [../concepts/03-node-npm-and-the-workspace.md](../concepts/03-node-npm-and-the-workspace.md).

---

## Concepts demonstrated

- **Node.js npm workspaces** — the mechanism that makes `@n3o/build` resolvable across all apps without publishing. See [../concepts/03-node-npm-and-the-workspace.md](../concepts/03-node-npm-and-the-workspace.md).
- **Vite library mode and the build pipeline** — `n3oPluginConfig` uses `build.lib` (library mode) and Rollup externals. See [../concepts/05-vite-and-the-build.md](../concepts/05-vite-and-the-build.md).

---

## Gotchas

**`@n3o/build` has no build step — do not add one.**
The package is consumed directly from source. `vite-config.js` is executed by Vite at build time; `base.json` is read by the TypeScript compiler. There is no compilation step for this package itself. Adding a `build` script that transforms or bundles these files would break consumers.

**Do not add `react` to the default `external` list.**
The unconditional external list only contains `/^@umbraco/`. React is only added when `react: true` is set. A non-React plugin must not have React in its externals — if it has no React imports, the external declaration is harmless but misleading. If a future non-React plugin accidentally imports React, the external declaration would hide that import from the bundle (silently depending on the import map) rather than bundling it or failing at build time.

**`entryFileNames: '[name].js'` — no content hash is intentional.**
Other bundler setups add a content hash to filenames (e.g. `my-plugin.abc123.js`) to bust browser caches. N3O plugins do not do this because their filenames are referenced by name in `umbraco-package.json` manifests. Umbraco reads those manifests at server startup; the filenames must be predictable. Cache busting is handled by ASP.NET Core's static file middleware (ETag / Last-Modified headers), not by filename hashing.

**`emptyOutDir: false` — adding `true` will break shared output directories.**
Many plugin apps write to a shared parent `App_Plugins` folder (e.g. `outDir: '../../wwwroot/App_Plugins'`). If any app set `emptyOutDir: true`, running its build would delete the compiled output of every other plugin.

**Adding a `"files"` key to an app tsconfig silently drops `vite-env.d.ts`.**
TypeScript's `"files"` key completely replaces the inherited list from `extends`. If an app needs to include additional specific files, use `"include"` instead, which merges with the parent.

**`vite-config.d.ts` must be kept in sync with `vite-config.js` manually.**
Because `vite-config.js` is plain JavaScript, the TypeScript compiler cannot detect a mismatch between the implementation and the declarations file. If you add a new option to `n3oPluginConfig`, you must also add it to the `N3oPluginConfigOptions` interface in `vite-config.d.ts`.
