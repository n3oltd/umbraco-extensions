# 04 — ES Modules and the Browser Import Map

> **Prerequisites:** read [03-node-npm-and-the-workspace](03-node-npm-and-the-workspace.md) first.
>
> **What this doc explains:** how JavaScript `import`/`export` works, the difference between ES Modules and the older CommonJS format, and — most critically — how the Umbraco 17 backoffice uses a browser **import map** to resolve bare package names like `@umbraco-cms/backoffice/external/lit` and `react` at runtime, and how this repo extends that map to share one copy of React and `@n3o/backoffice-core` across all plugins.

---

## 1. `export` and `import` — the ES Module syntax

### 1.1 Named exports

A file can export multiple named values. Each consumer explicitly names what it wants.

```typescript
// math.ts
export function add(a: number, b: number) { return a + b; }
export function subtract(a: number, b: number) { return a - b; }
export const PI = 3.14159;
```

```typescript
// consumer.ts
import { add, PI } from './math.js';   // only imports what it names
```

C# analogy: `export` is `public`; not exporting is `internal`. The `import { ... }` list is analogous to a `using` statement that also selects which public members you care about.

### 1.2 Default exports

A module may also have one *default* export — an unnamed main value.

```typescript
// widget.ts
export default class Widget { ... }
```

```typescript
// consumer.ts
import Widget from './widget.js';       // any name works for default imports
```

Default exports are common in React component files and in the React shim files (see section 5).

### 1.3 Namespace imports

Import everything from a module as a single object:

```typescript
import * as math from './math.js';
math.add(1, 2);
```

C# analogy: comparable to `using static` combined with a namespace alias.

### 1.4 Re-exports

A module can re-export values from another module — useful for building a public API that composes several files:

```typescript
// index.ts — barrel file
export { add, subtract } from './math.js';
export { Widget } from './widget.js';
```

C# analogy: an `index.ts` barrel file is comparable to a C# namespace that re-exposes types from sub-namespaces.

### 1.5 Type-only imports

TypeScript adds `import type`, which imports only the TypeScript type information and is erased completely at compile time — it produces no JavaScript output:

```typescript
import type { UmbConditionConfigBase } from '@umbraco-cms/backoffice/extension-api';
```

Seen throughout the repo, e.g. in `src/N3O.Umbraco.Cms/Apps/N3O.Umbraco.BackofficeCore/src/workspace-visibility-condition.ts`:

```typescript
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import type { UmbConditionConfigBase, UmbConditionControllerArguments } from '@umbraco-cms/backoffice/extension-api';
```

These two imports leave no trace in the compiled `.js` file.

---

## 2. ES Modules (ESM) vs CommonJS (CJS)

JavaScript has two module systems. Understanding why both exist explains a subtle but important bug in the React shim (section 5).

### 2.1 CommonJS (CJS) — the old system

CommonJS was invented for Node.js before browsers had a native module system. Files use `require()` and `module.exports`:

```javascript
// CJS style
const React = require('react');
module.exports = { useState: React.useState };
```

React itself (the npm package) is published as CommonJS.

### 2.2 ES Modules (ESM) — the modern browser standard

ESM is the JavaScript standard that browsers natively understand. It uses `import`/`export` statements (shown in section 1). Browsers can load ESM `<script type="module">` tags directly, and the file can even import other files over HTTP.

### 2.3 Why the distinction matters here

All Umbraco 17 backoffice plugins must be **ESM**. The browser can load ESM natively; it cannot load CommonJS directly. Vite compiles TypeScript to ESM output.

The problem: **React (and react-dom) are published as CommonJS**. When Vite's library-mode build tries to re-export a CJS package as ESM using `export * from 'react'`, the CJS named exports (`useState`, `useEffect`, etc.) do not make it through — only the `default` export survives.

This is why the React shim in this repo uses explicit named re-exports rather than `export *`. Section 5 explains the shim in full.

---

## 3. Bare specifiers and the browser's problem

When your TypeScript writes:

```typescript
import { LitElement } from '@umbraco-cms/backoffice/external/lit';
import { useState } from 'react';
```

These are called **bare specifiers** — they name a package, not a file path. Node.js can resolve them (it knows to look in `node_modules/`). The **browser cannot** — it only understands:

- Relative paths: `./foo.js`, `../bar.js`
- Absolute URLs: `https://example.com/lit.js`
- Data URLs

If you ship a `.js` file with a bare-specifier import to the browser with no further configuration, you get a runtime error:

```
Uncaught TypeError: Failed to resolve module specifier "@umbraco-cms/backoffice/external/lit".
Relative references must start with either "/", "./", or "../".
```

This is a **silent startup failure** — the web-component element simply never registers, and the backoffice shows nothing where the plugin should be, with no obvious error unless you open the browser console.

The solution is an **import map**.

---

## 4. The browser import map

An import map is a JSON dictionary, delivered in a `<script type="importmap">` tag in the page's HTML, that tells the browser how to translate bare specifiers to real URLs.

Simplified example:

```html
<script type="importmap">
{
  "imports": {
    "@umbraco-cms/backoffice/external/lit": "/umbraco/backoffice/lit.js",
    "react": "/App_Plugins/N3O.Umbraco.ReactRuntime/react.js",
    "@n3o/backoffice-core": "/App_Plugins/N3O.Umbraco.BackofficeCore/auth-fetch.js"
  }
}
</script>
```

When the browser encounters `import { useState } from 'react'` in a loaded module, it looks up `"react"` in the import map and fetches `/App_Plugins/N3O.Umbraco.ReactRuntime/react.js`.

**The import map must be present in the page before any module scripts load.** There is only one import map per page. Umbraco 17 builds the import map at startup by reading all `umbraco-package.json` files and merging their `importmap` sections.

---

## 5. How Umbraco 17 builds the import map from `umbraco-package.json`

Every backoffice plugin declares itself in an `umbraco-package.json` file. Umbraco reads all of them at startup. If a `umbraco-package.json` has an `importmap` section, those entries are merged into the page's global import map.

### 5.1 The ReactRuntime import map entries

File: `src/N3O.Umbraco.Cms/wwwroot/App_Plugins/N3O.Umbraco.ReactRuntime/umbraco-package.json`

```json
{
    "$schema": "http://json.schemastore.org/umbraco-package.json",
    "id": "N3O.Umbraco.ReactRuntime",
    "name": "N3O React Runtime",
    "version": "17.0.0",
    "importmap": {
        "imports": {
            "react":             "/App_Plugins/N3O.Umbraco.ReactRuntime/react.js",
            "react/jsx-runtime": "/App_Plugins/N3O.Umbraco.ReactRuntime/react-jsx-runtime.js",
            "react-dom":         "/App_Plugins/N3O.Umbraco.ReactRuntime/react-dom.js",
            "react-dom/client":  "/App_Plugins/N3O.Umbraco.ReactRuntime/react-dom.js"
        }
    }
}
```

These four entries ensure that any plugin that `import`s `react`, `react/jsx-runtime`, `react-dom`, or `react-dom/client` gets the **same single file** from `N3O.Umbraco.ReactRuntime`. This is critical: React uses module-level state (the current component's fiber, hooks registry, etc.). If two different files bundle React, they each have their own state and React breaks. The import map enforces **one React instance** for the entire backoffice page.

### 5.2 The BackofficeCore import map entry

File: `src/N3O.Umbraco.Cms/wwwroot/App_Plugins/N3O.Umbraco.BackofficeCore/umbraco-package.json`

```json
{
    "$schema": "http://json.schemastore.org/umbraco-package.json",
    "id": "N3O.Umbraco.BackofficeCore",
    "name": "N3O Backoffice Core",
    "version": "17.0.0",
    "extensions": [ ... ],
    "importmap": {
        "imports": {
            "@n3o/backoffice-core": "/App_Plugins/N3O.Umbraco.BackofficeCore/auth-fetch.js"
        }
    }
}
```

This means any plugin that writes:

```typescript
import { UmbAuthFetchMixin } from '@n3o/backoffice-core';
```

...gets the shared `auth-fetch.js` from `BackofficeCore`. Without this entry the browser would fail with the bare-specifier error described in section 3.

---

## 6. Why Vite builds keep these specifiers "external"

When Vite compiles a plugin (see [05-vite-and-the-build](05-vite-and-the-build.md)), it must decide what to do with each `import` statement:

- **Bundle**: copy the imported code into the output `.js` file. Good for small, private utilities.
- **External**: leave the `import` statement as-is in the output. The browser will resolve it at runtime (via the import map).

If Vite bundled React into each plugin's `.js` file, every plugin would carry its own copy of React — breaking the single-instance rule above.

The shared build config (`src/N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/vite-config.js`) marks all `@umbraco-cms/*` packages external by default, and React external when the plugin declares `react: true`:

```javascript
// src/N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/vite-config.js
const external = [/^@umbraco/];

if (react) {
    external.push('react', 'react-dom', 'react-dom/client', 'react/jsx-runtime');
}
```

The compiled output of any React plugin therefore still contains:

```javascript
import { useState } from 'react';          // bare specifier — NOT bundled
import { createRoot } from 'react-dom/client';
```

At runtime the browser resolves these via the import map to the single shared files.

---

## 7. The React shim — why `export *` is not enough

React's npm package is CommonJS. The problem was described in section 2.3: `export * from 'react'` through Vite's library-mode build drops all named exports.

The shim at `src/N3O.Umbraco.Cms/Build/N3O.Umbraco.ReactRuntime/src/react.js` solves this with explicit re-exports:

```javascript
// src/N3O.Umbraco.Cms/Build/N3O.Umbraco.ReactRuntime/src/react.js

import React from 'react';     // import the CJS default (the whole React object)

export default React;           // re-export as default

export const {                  // destructure and explicitly re-export each named export
    useState,
    useEffect,
    useContext,
    createContext,
    // ... (all public React APIs listed explicitly)
} = React;
```

The same pattern is used for `react-jsx-runtime`:

```javascript
// src/N3O.Umbraco.Cms/Build/N3O.Umbraco.ReactRuntime/src/react-jsx-runtime.js

import jsxRuntime from 'react/jsx-runtime';

export const jsx     = jsxRuntime.jsx;
export const jsxs    = jsxRuntime.jsxs;
export const Fragment = jsxRuntime.Fragment;
```

And for `react-dom`:

```javascript
// src/N3O.Umbraco.Cms/Build/N3O.Umbraco.ReactRuntime/src/react-dom.js

import ReactDOM from 'react-dom';

export default ReactDOM;
export const { createPortal, flushSync, ... } = ReactDOM;

export { createRoot, hydrateRoot } from 'react-dom/client';
```

Note that `react-dom.js` keeps `react` itself **external** (declared in `vite.config.rest.ts`). That means the built `react-dom.js` file still imports from the `react` bare specifier — which the browser resolves via the import map to the same `react.js` shim, guaranteeing the single React instance.

---

## 8. A concrete end-to-end trace

Here is the full journey for a single import in `DynamicListViews`:

1. **Source TypeScript** (`src/N3O.Umbraco.Cms/Apps/N3O.Umbraco.DynamicListViews/src/dynamic-list-view.ts`):
   ```typescript
   import { LitElement, html, css, customElement, state, nothing }
       from '@umbraco-cms/backoffice/external/lit';
   ```

2. **Vite builds** the TypeScript to `src/N3O.Umbraco.Cms/wwwroot/App_Plugins/N3O.Umbraco.DynamicListViews/dynamic-list-view.js`, keeping the import as-is (external, not bundled).

3. **Umbraco starts up** and reads all `umbraco-package.json` files. The one from `N3O.Umbraco.DynamicListViews` (`src/N3O.Umbraco.Cms/wwwroot/App_Plugins/N3O.Umbraco.DynamicListViews/umbraco-package.json`) declares the extension but has no `importmap` section. The Umbraco backoffice's own packages contribute the `@umbraco-cms/backoffice/external/lit` entry.

4. **Browser loads** the backoffice page. The import map is injected into `<head>`.

5. **Browser fetches** `/App_Plugins/N3O.Umbraco.DynamicListViews/dynamic-list-view.js`. It encounters `import ... from '@umbraco-cms/backoffice/external/lit'`, looks it up in the import map, and fetches Umbraco's served Lit bundle.

6. The custom element `<n3o-dynamic-list-view>` is registered. When Umbraco renders a document workspace with the correct condition, it inserts this element into the DOM and the component initialises.

---

## 9. The silent-failure trap

A missing import-map entry **does not produce a compile error**. TypeScript compiles successfully. Vite builds successfully. The `.js` file looks correct. The failure only occurs at runtime in the browser:

```
Uncaught TypeError: Failed to resolve module specifier "@n3o/backoffice-core".
```

The web component that tried to import it never registers. The backoffice shows a blank panel — or nothing at all. **Always check the browser console** when a plugin appears to do nothing after a build.

The checklist when a new shared package is introduced:
1. Add an `importmap` entry to the package's `umbraco-package.json` (as `BackofficeCore` does for `@n3o/backoffice-core`).
2. Mark the specifier as `external` in the consumer's Vite config (via `additionalExternals` in `n3oPluginConfig`).
3. Verify the served file actually exists in `wwwroot/App_Plugins/`.

---

## 10. Summary

| Concept | One-line description |
|---------|----------------------|
| Named export | `export const x = ...` — other files import by name |
| Default export | `export default ...` — one per module, imported without braces |
| Re-export | `export { x } from './other'` — forwarding another module's exports |
| Type-only import | `import type { T }` — TypeScript only; erased from JS output |
| ESM | The browser-native module system; uses `import`/`export` |
| CommonJS | Node.js legacy format; uses `require`/`module.exports`. React's npm package is CJS. |
| Bare specifier | `import x from 'react'` — a package name with no path prefix |
| Import map | A browser-side dictionary mapping bare specifiers to real URLs |
| `umbraco-package.json` `importmap` | The mechanism Umbraco uses to add entries to the page's import map |
| External (Vite) | Leave an `import` in the output; do not bundle the dependency |
| React shim | The `N3O.Umbraco.ReactRuntime` bundle that re-exports CJS React as ESM, served once and shared by all plugins via the import map |

Next: [05-vite-and-the-build](05-vite-and-the-build.md) — what Vite does, how library mode works, and how the shared `@n3o/build` preset wires everything together.
