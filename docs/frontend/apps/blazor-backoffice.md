# N3O.Umbraco.Blazor.BackOffice

**Source (frontend):** `src/Blazor/N3O.Umbraco.Blazor.BackOffice/Apps/`
**Output:** `src/Blazor/N3O.Umbraco.Blazor.BackOffice/wwwroot/App_Plugins/N3O.Umbraco.Blazor.BackOffice/`
**Manifest:** `src/Blazor/N3O.Umbraco.Blazor.BackOffice/wwwroot/App_Plugins/N3O.Umbraco.Blazor.BackOffice/umbraco-package.json`
**Framework:** Plain TypeScript — no Lit, no React, no custom elements.

---

## What it is

This app is a **loader script**: a small piece of TypeScript that runs side effects when loaded, registers nothing in the DOM, and renders no UI of its own. Its sole job is to inject the Blazor Server JavaScript file into the page and start the Blazor SignalR circuit.

**Blazor Server** is the ASP.NET Core technology that lets you write C# components that render and respond to events over a SignalR WebSocket connection. When a Blazor Server component is in the page, the browser and server maintain a live connection; C# code running on the server drives DOM updates in the browser. The `blazor.server.js` file (served by the ASP.NET Core runtime from `/_framework/blazor.server.js`) is the client-side half of this connection.

The backoffice itself does not load `blazor.server.js` — it is a plain Vite/Lit SPA with no knowledge of Blazor. This loader bridges the gap: it is a backoffice extension that, when the backoffice loads, injects the `<script>` tag for `blazor.server.js` and then calls `Blazor.start()` to open the SignalR circuit.

This app is the **minimum viable backoffice extension**. It demonstrates that:

1. A backoffice plugin does not have to define a custom element or any UI at all.
2. A plugin can be registered as a `"bundle"` extension — a script that is loaded and run purely for its side effects.
3. Even the most trivial script still goes through the same full pipeline: TypeScript source → `tsc` type check → Vite build → `umbraco-package.json` manifest → Umbraco loads at startup.

Reading this app alongside [dynamiclistviews.md](dynamiclistviews.md) shows the full spectrum from "pure side-effect loader" to "full Lit component with data fetching."

---

## Files

| File | Role |
|------|------|
| `package.json` | npm package manifest; declares the build scripts and dev dependency on `@n3o/build` |
| `tsconfig.json` | TypeScript config; inherits shared compiler settings from `@n3o/build/tsconfig` |
| `vite.config.ts` | Vite build config; uses `n3oPluginConfig`; single entry; `outDir` points directly at the plugin folder |
| `src/N3O.Umbraco.Blazor.BackOffice.ts` | The entire frontend: ambient type declarations for the Blazor global, idempotent script injection, and `Blazor.start()` call |
| `wwwroot/.../umbraco-package.json` | Umbraco manifest: registers a `"bundle"` extension — a script loaded for its side effects, not as a custom element |

---

## End-to-end flow

```
dotnet build
  └─ MSBuild runs: npm run build  (in Blazor/N3O.Umbraco.Blazor.BackOffice/Apps/)
       └─ tsc --noEmit    (TypeScript type check)
       └─ vite build      (bundles N3O.Umbraco.Blazor.BackOffice.ts → N3O.Umbraco.Blazor.BackOffice.js)
                           All @umbraco-cms/* → kept EXTERNAL (though none are used here)
                           Output → wwwroot/App_Plugins/N3O.Umbraco.Blazor.BackOffice/
                                      N3O.Umbraco.Blazor.BackOffice.js
                                      N3O.Umbraco.Blazor.BackOffice.js.map

Umbraco starts
  └─ Scans App_Plugins/**/umbraco-package.json
  └─ Reads N3O.Umbraco.Blazor.BackOffice/umbraco-package.json
  └─ Registers one "bundle" extension with the JS file path

Browser opens the backoffice
  └─ Umbraco loads all registered "bundle" scripts unconditionally (no conditions check)
  └─ Loads N3O.Umbraco.Blazor.BackOffice.js as an ES module
  └─ The module-level code executes immediately:
       blazorIsLoaded() → false (first load)
       → creates <script src="/_framework/blazor.server.js" autostart="false">
       → appends it to document.body
       → script.onload fires when /_framework/blazor.server.js is fetched
       → startBlazor() called: Blazor.start({ configureSignalR: ... })
       → SignalR circuit opens to /_blazor
       Blazor Server components in the page can now communicate with the C# server
```

---

## File-by-file walkthrough

### `package.json`

```json
{
    "name": "n3o-umbraco-blazor-backoffice",
    "version": "17.0.0",
    "private": true,
    "type": "module",
    "scripts": {
        "build": "tsc --noEmit && vite build",
        "watch": "vite build --watch"
    },
    "devDependencies": {
        "@n3o/build": "*"
    }
}
```

Structurally identical to every other N3O backoffice plugin package. Key points that apply broadly:

- **`"version": "17.0.0"`** — this package uses CalVer aligned to the Umbraco major. (Most other apps use `"1.0.0"`; this one uses `"17.0.0"` — a minor inconsistency, but both work identically.)
- **`"type": "module"`** — required for ES module `import`/`export` in Vite config and source files.
- **`"devDependencies": { "@n3o/build": "*" }`** — the only declared dependency; everything else comes from the workspace root.
- **No `dependencies`** — this package makes no network requests, imports no external library at runtime, and ships no UI. The compiled output has no `import` statements at all (nothing is external because nothing is imported from `@umbraco-cms/*` or any other external package).

---

### `tsconfig.json`

```json
{
    "extends": "@n3o/build/tsconfig",
    "include": ["src"]
}
```

Identical structure to all other apps. Inherits shared settings from `@n3o/build/tsconfig`. Narrows type checking to `src/`. See [buildconfig.md](buildconfig.md) for the full base config.

---

### `vite.config.ts`

```typescript
import { n3oPluginConfig } from '@n3o/build';

// Builds the Blazor BackOffice loader script into the shipped App_Plugins folder.
// This is a non-Lit bundle/script entry — it is a plain loader, not a custom element.
// @umbraco-cms/* imports are kept external (resolved at runtime by Umbraco's import map);
// the loader's own code is bundled.
export default n3oPluginConfig({
    entries: { 'N3O.Umbraco.Blazor.BackOffice': 'src/N3O.Umbraco.Blazor.BackOffice.ts' },
    outDir: '../wwwroot/App_Plugins/N3O.Umbraco.Blazor.BackOffice',
});
```

- **`entries`** — a single entry. The key `'N3O.Umbraco.Blazor.BackOffice'` becomes the output filename, producing `N3O.Umbraco.Blazor.BackOffice.js` directly in `outDir`. There is no subdirectory in the key — this app owns its own `outDir` subfolder entirely.
- **`outDir: '../wwwroot/App_Plugins/N3O.Umbraco.Blazor.BackOffice'`** — one level up from `Apps/` (the `Apps` folder is directly inside the C# project root), landing in `N3O.Umbraco.Blazor.BackOffice/wwwroot/App_Plugins/N3O.Umbraco.Blazor.BackOffice/`. Unlike the apps inside `N3O.Umbraco.Cms`, this project is in its own project folder and the output goes into its own `wwwroot`.
- **No `react: true`** — no React. The preset still marks all `@umbraco-cms/*` as external, but since the source file doesn't import from `@umbraco-cms/*` either, the compiled output contains no `import` statements whatsoever.
- **No `additionalExternals`** — no additional dependencies.

The comment in the file is useful documentation of a deliberate decision: the loader is intentionally not wrapped in a React root because there is nothing to render.

---

### `src/N3O.Umbraco.Blazor.BackOffice.ts`

This single file is the entire frontend for this plugin. It is a module-level side-effect script — it runs top to bottom when the browser loads it as an ES module.

```typescript
/*
 * NOTE: React shell is overhead here (this is a non-UI JS boot loader, not a custom element) —
 * kept as the existing loader per migration decision. This bundle registers no element and renders
 * no UI; it only runs side effects at import time (injects the blazor.server.js <script> tag and
 * starts the SignalR circuit). There is nothing for React to render, so it is intentionally NOT
 * wrapped in a React root — doing so would add a runtime dependency with zero UI to mount. Left as
 * a minimal, faithful TypeScript port of the original loader.
 *
 * jQuery was removed in the v17 RCL conversion: Umbraco 17 (Bellissima) does not ship a global `$`,
 * so the "is the blazor script already injected" check now uses native DOM (querySelectorAll).
 */
```

**Lines 1-11 — block comment:**

This comment documents two migration decisions: (1) why there is no React wrapper despite other plugins using one, and (2) why jQuery was removed. The comment is part of the code — it is the canonical answer to "why is this different from the other apps?"

```typescript
interface SignalRBuilder {
    withUrl(url: string): SignalRBuilder;
    withAutomaticReconnect(reconnectDelays: number[]): SignalRBuilder;
    build(): SignalRConnection;
}

interface SignalRConnection {
    serverTimeoutInMilliseconds: number;
}

interface BlazorStartOptions {
    configureSignalR(builder: SignalRBuilder): void;
}

declare const Blazor: {
    start(options: BlazorStartOptions): void;
};
```

**Lines 13-30 — ambient type declarations:**

`blazor.server.js` is loaded at runtime by injecting a `<script>` tag. When it loads it puts a `Blazor` global variable on `window`. TypeScript does not know this global exists — there is no npm package with types for it. The `declare const Blazor: { ... }` block is an **ambient declaration**: it tells the TypeScript compiler "trust me, this global exists at runtime with this shape." C# analogy: an `extern` declaration or an assembly reference where only the public API is described.

The interfaces `SignalRBuilder`, `SignalRConnection`, and `BlazorStartOptions` describe the callback API used by `Blazor.start()`. They are minimal — only the parts actually used in `startBlazor()` are declared. This is intentional; declaring only what you use avoids maintaining stubs for the full Blazor/SignalR API surface.

These declarations produce no output in the compiled JavaScript — they are erased entirely by TypeScript. At runtime the actual `Blazor` global (with its full API) is what executes.

```typescript
const blazorJsFile = '/_framework/blazor.server.js';

function blazorIsLoaded(): boolean {
    return Array.from(document.querySelectorAll('script')).some(
        (s) => s.getAttribute('src') === blazorJsFile
    );
}
```

**Lines 32-38 — idempotency guard:**

`blazorIsLoaded()` checks whether `/_framework/blazor.server.js` is already in the page's `<script>` tags. This prevents injecting the script a second time if the module is somehow evaluated twice (possible in hot-reload scenarios or if two plugins accidentally both try to start Blazor).

`document.querySelectorAll('script')` returns a `NodeList` — a browser-native collection of DOM nodes. Unlike C#'s `IEnumerable`, `NodeList` does not natively support `Array.some()`. `Array.from(...)` converts it to a real JavaScript `Array`. The `.some()` method then returns `true` if any element matches the predicate — equivalent to C# LINQ's `.Any()`.

`s.getAttribute('src') === blazorJsFile` — `getAttribute` returns the raw attribute string, which is compared to the known path. This is a deliberate choice over `s.src` (which would give the absolute URL, e.g. `https://localhost:5778/_framework/blazor.server.js`), keeping the check path-independent.

```typescript
if (!blazorIsLoaded()) {
    const scriptElement = document.createElement('script');
    scriptElement.src = blazorJsFile;
    scriptElement.onload = startBlazor;

    scriptElement.setAttribute('autostart', 'false');

    document.body.appendChild(scriptElement);
}
```

**Lines 40-48 — script injection:**

This is the module-level top-level code — it runs immediately when the ES module is loaded by the browser. There is no wrapping function, no event listener, no class. The code executes as the module is evaluated.

`document.createElement('script')` creates a new `<script>` DOM element. Setting `scriptElement.src` is equivalent to writing `<script src="...">` in HTML. The browser fetches the URL when the element is appended to the document.

`scriptElement.onload = startBlazor` — wires the `startBlazor` function as the callback for when the script has finished loading. This is important: `Blazor.start()` can only be called after `blazor.server.js` has been parsed and its global registered. `onload` fires at that point. C# analogy: subscribing to an event that fires when an async initialisation step completes.

`scriptElement.setAttribute('autostart', 'false')` — sets the `autostart` attribute on the `<script>` tag. Blazor's script detects this attribute on itself: if `autostart` is not `'false'`, it calls `Blazor.start()` automatically with default settings. Setting it to `'false'` suppresses this so the application can call `Blazor.start()` manually with custom `configureSignalR` options (in `startBlazor()` below).

`document.body.appendChild(scriptElement)` — attaches the `<script>` element to the live DOM. The browser immediately starts fetching `/_framework/blazor.server.js`. When the fetch completes, `onload` fires and `startBlazor()` runs.

```typescript
async function startBlazor(): Promise<void> {
    Blazor.start({
        configureSignalR: function (builder: SignalRBuilder): void {
            builder.withUrl('/_blazor');
            builder.withAutomaticReconnect([0, 2000, 10000, 15000, 20000, 30000, 60000]);

            const connection = builder.build();
            connection.serverTimeoutInMilliseconds = 30_000;
        }
    });
}
```

**Lines 50-60 — `startBlazor()`:**

`Blazor.start()` accepts an options object. The `configureSignalR` callback receives a `SignalRBuilder` — a fluent builder for the underlying SignalR connection that Blazor Server uses to communicate with the C# server.

`builder.withUrl('/_blazor')` — sets the SignalR hub URL. `/_blazor` is the default Blazor Server hub endpoint registered by `app.MapBlazorHub()` in the ASP.NET Core pipeline.

`builder.withAutomaticReconnect([0, 2000, 10000, 15000, 20000, 30000, 60000])` — configures retry delays (in milliseconds) for automatic reconnection. The array `[0, 2000, 10000, 15000, 20000, 30000, 60000]` means: retry immediately on first disconnect, then after 2 s, 10 s, 15 s, 20 s, 30 s, and 60 s. After the last entry is exhausted the client stops retrying.

`connection.serverTimeoutInMilliseconds = 30_000` — sets the server timeout to 30 seconds. The `_` separators in numeric literals (`30_000`) are a TypeScript/JavaScript readability feature, equivalent to `30_000` in C# — they have no runtime effect and are stripped by the compiler.

---

### `wwwroot/App_Plugins/N3O.Umbraco.Blazor.BackOffice/umbraco-package.json`

```json
{
    "$schema": "http://json.schemastore.org/umbraco-package.json",
    "id": "N3O.Umbraco.Blazor.BackOffice",
    "name": "N3O Blazor BackOffice",
    "version": "17.0.0",
    "extensions": [
        {
            "type": "bundle",
            "alias": "N3O.Bundle.BlazorBackOffice",
            "name": "N3O Blazor BackOffice",
            "js": "/App_Plugins/N3O.Umbraco.Blazor.BackOffice/N3O.Umbraco.Blazor.BackOffice.js"
        }
    ]
}
```

**Top-level fields:**

| Field | Value | Meaning |
|-------|-------|---------|
| `"$schema"` | JSONSchema URL | IDE validation |
| `"id"` | `"N3O.Umbraco.Blazor.BackOffice"` | Unique package identifier in the backoffice extension registry |
| `"name"` | `"N3O Blazor BackOffice"` | Display name |
| `"version"` | `"17.0.0"` | Package version |

**Extension fields:**

| Field | Value | Meaning |
|-------|-------|---------|
| `"type"` | `"bundle"` | A `"bundle"` extension is a JavaScript file that Umbraco loads unconditionally for its side effects. It is not a custom element, not a workspace view, not a property editor — it is simply a script that runs. Compare to `"workspaceView"` (in DynamicListViews) or `"dashboard"` or `"propertyEditorUi"`, which all produce visible UI. |
| `"alias"` | `"N3O.Bundle.BlazorBackOffice"` | Unique identifier for this extension within the registry |
| `"name"` | `"N3O Blazor BackOffice"` | Internal name |
| `"js"` | `"/App_Plugins/.../N3O.Umbraco.Blazor.BackOffice.js"` | The URL of the JS file to load. Umbraco loads this as an ES module. The module evaluates top to bottom, executing the side effects. |

**No `"element"` field:** workspaceView and dashboard extensions use `"element"` to specify a JS file that exports a custom element class. Bundle extensions use `"js"` instead, because there is no element to instantiate — the script is loaded and run, period.

**No `"conditions"` array:** bundle extensions have no conditions. The script is always loaded when the backoffice starts. There is no per-node or per-user gating at the manifest level. If the Blazor loader needed to be conditional (e.g. only on certain pages), that logic would have to be implemented inside the script itself.

**No `"importmap"` section:** this package contributes nothing to the import map. It does not provide any shared modules. Contrast with BackofficeCore (which adds `@n3o/backoffice-core`) or ReactRuntime (which adds `react`, `react-dom`, etc.).

---

## Why no UI framework?

The `vite.config.ts` comment makes the reasoning explicit:

> This is a non-Lit bundle/script entry — it is a plain loader, not a custom element. [...] it only runs side effects at import time (injects the blazor.server.js `<script>` tag and starts the SignalR circuit). There is nothing for React to render, so it is intentionally NOT wrapped in a React root — doing so would add a runtime dependency with zero UI to mount.

A UI framework (Lit, React) is a tool for defining and updating DOM. This file creates exactly one `<script>` element, appends it to `document.body`, and calls `Blazor.start()`. That is it. No component tree, no state, no rendering. Adding React would mean loading a ~50 KB library to call one DOM API — pure overhead.

The same reasoning applies to Lit: `LitElement` is a tool for defining reusable custom elements. A script that runs once and does some DOM manipulation does not benefit from a component model.

This is a useful mental model to carry to future work: **reach for a framework when you need a component. Use plain TypeScript when you need a script.**

---

## Concepts demonstrated

- **`"bundle"` extension type** — the simplest Umbraco extension type: a script loaded for side effects. See [../concepts/09-umbraco-backoffice-extensions.md](../concepts/09-umbraco-backoffice-extensions.md).
- **Ambient type declarations** — `declare const Blazor: { ... }` tells TypeScript about a runtime global without any npm package. C# analogy: `extern`.
- **Module-level top-level code** — JavaScript ES modules execute top-to-bottom when loaded. Code outside any function or class runs immediately at import time. This is how the script injection works without an entry-point function call.
- **Vite library build** — even a trivial loader goes through `n3oPluginConfig` and produces a proper ESM bundle. See [../concepts/05-vite-and-the-build.md](../concepts/05-vite-and-the-build.md).
- **The full pipeline** — `package.json` → `tsconfig.json` → `vite.config.ts` → `umbraco-package.json` is exactly the same as every other app, regardless of how small the source is. The pipeline is the fixed structure; the source varies.

---

## Gotchas

**`blazorIsLoaded()` must use `getAttribute('src')`, not `element.src`.**
`element.src` returns the fully-qualified URL (`https://host/_framework/blazor.server.js`), which differs from the stored constant `'/_framework/blazor.server.js'`. `getAttribute('src')` returns the raw attribute value as written in the HTML — matching the constant.

**`autostart="false"` must be set before `appendChild`.**
The `<script>` tag is processed by the browser as soon as it is inserted into the DOM. Blazor's own code checks for `autostart="false"` on itself when the script executes. If `setAttribute('autostart', 'false')` were called after `appendChild`, the timing would be a race condition. The current code sets the attribute first, then appends — ensuring Blazor sees it before running.

**`Blazor.start()` is called in `onload`, not at module evaluation time.**
`blazor.server.js` has not been fetched yet when the module-level `if (!blazorIsLoaded())` block runs. Calling `Blazor.start()` at module evaluation time would throw a `ReferenceError: Blazor is not defined` because the global does not exist yet. The `onload` callback fires only after the fetch is complete and the script has been parsed — at which point the global is safe to use.

**The compiled output has no `import` statements — it is fully self-contained.**
Because `N3O.Umbraco.Blazor.BackOffice.ts` imports nothing from `@umbraco-cms/*` or any other external module, Vite bundles everything inline. The resulting `.js` file has no `import` statements. This means it does not depend on the import map at all. If you add `import` statements from external packages in future, you must also mark them as external (or add import-map entries) — otherwise Vite will try to bundle them, and `@umbraco-cms/*` packages are not designed to be bundled this way.

**The Blazor SignalR endpoint `/_blazor` must be registered in the C# pipeline.**
`builder.withUrl('/_blazor')` is only valid if `app.MapBlazorHub()` is called in `Program.cs` (or `Startup.cs`). If the C# pipeline does not register the hub, the SignalR connection will fail with a 404 and Blazor components will never initialise. The TypeScript side has no way to detect this at build time.

**`withAutomaticReconnect` delays are intentional — do not shorten them.**
The array `[0, 2000, 10000, 15000, 20000, 30000, 60000]` was chosen to balance user experience (quick first retry) against server load (exponential backoff for sustained outages). Changing these values affects how aggressively the client polls the server during an outage.
