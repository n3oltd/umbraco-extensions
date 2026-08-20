# data-export — N3O Data Export workspace view

> **Prerequisites (concepts to read first):**
> [01 — the-big-picture](../concepts/01-the-big-picture.md) ·
> [03 — node-npm-and-the-workspace](../concepts/03-node-npm-and-the-workspace.md) ·
> [04 — es-modules-and-import-maps](../concepts/04-es-modules-and-import-maps.md) ·
> [05 — vite-and-the-build](../concepts/05-vite-and-the-build.md) ·
> 06 — web-components-and-shadow-dom ·
> 08 — react ·
> 09 — umbraco-backoffice-extensions ·
> [10 — the-n3o-bridge-pattern](../concepts/10-the-n3o-bridge-pattern.md)

This app is the **exemplar well-structured React workspace view** in the codebase. Read it as a complete worked example of every pattern used across all N3O backoffice frontends: the bridge pattern, custom React hooks, props-down composition, authenticated server calls, polling, CSS-in-shadow-DOM, and the native-HTML-controls-inside-uui-box pattern.

---

## 1. What it is

The Data Export plugin adds an **Export** tab to every document workspace in the Umbraco backoffice. When a content editor opens a document, a new "Export" tab appears alongside "Content" and "Properties". Clicking it shows a React UI that lets the editor:

1. choose which **descendant content type** to export;
2. select a file **format** (Excel or CSV);
3. toggle **include unpublished** content;
4. pick **metadata fields** (e.g. name, ID) to include;
5. pick **properties** (custom fields defined on the content type) to include; and
6. click **Export** — the UI calls the server to create an export job, polls for progress, and finally triggers a file download in the browser.

**Extension type:** `workspaceView` — a tab pane inside a content-item workspace.

**Where the assets are served from:**
- Source code: `src/Data/N3O.Umbraco.Data.StaticAssets/Apps/N3O.Umbraco.Data.Export/`
- Compiled output (committed to git): `src/Data/N3O.Umbraco.Data.StaticAssets/wwwroot/App_Plugins/N3O.Umbraco.Data.Export/`
- At runtime Umbraco serves the compiled output at the URL `/App_Plugins/N3O.Umbraco.Data.Export/data-export.js` via the Razor static-web-assets pipeline (see [01 — the-big-picture](../concepts/01-the-big-picture.md)).

---

## 2. Files

### App_Plugins — the manifest (served at runtime)

| File | Purpose |
|------|---------|
| `wwwroot/App_Plugins/N3O.Umbraco.Data.Export/umbraco-package.json` | Tells Umbraco to register the `workspaceView` extension, what JS to load, under what conditions |
| `wwwroot/App_Plugins/N3O.Umbraco.Data.Export/data-export.js` | Compiled output (emitted by Vite, committed to git) |
| `wwwroot/App_Plugins/N3O.Umbraco.Data.Export/data-export.js.map` | Source map for browser DevTools debugging |

### Apps — the source (edited by developers)

| File | Kind | Purpose |
|------|------|---------|
| `package.json` | npm project manifest | Package name, build/watch scripts, dev-only dependencies |
| `tsconfig.json` | TypeScript config | Extends shared base; adds JSX mode for React |
| `vite.config.ts` | Vite build config | Entry point, output directory, externals |
| `src/data-export.ts` | Web-component shell | Registers `<n3o-data-export>`, consumes Umbraco context, mounts React |
| `src/data-export-app.tsx` | React container component | Wires hooks and sub-components; owns selection state; computes `canExport` |
| `src/use-export.ts` | Custom React hooks | Server data loading (`useExportServerData`) + export run logic (`useExportRun`) |
| `src/export-options.tsx` | Presentational component | Export Options card: content type selector, format radios, unpublished toggle |
| `src/selectable-field-list.tsx` | Generic presentational component | Reusable checkbox-grid with select-all/clear, used for both Metadata and Properties lists |
| `src/types.ts` | TypeScript interfaces | Shared data shapes for server responses and UI state |
| `src/data-export-app.css` | Stylesheet | Scoped CSS, injected at runtime via the `?inline` import pattern |
| `src/uui-react.d.ts` | TypeScript ambient declaration | Adds JSX types for `uui-box`, `uui-icon`, `uui-loader-bar`, `umb-property-layout` |

---

## 3. End-to-end flow

Understanding the flow from "Umbraco starts" to "file downloads in the browser" requires connecting several concepts.

### 3a. Manifest discovery (server startup)

At startup Umbraco 17 scans all `App_Plugins/**/umbraco-package.json` files it can find (from `wwwroot/` directories or NuGet-packaged static web assets). It reads the `extensions` array and registers each one. For Data Export:

```json
// wwwroot/App_Plugins/N3O.Umbraco.Data.Export/umbraco-package.json
{
    "$schema": "http://json.schemastore.org/umbraco-package.json",
    "id": "N3O.Umbraco.Data.Export",
    "name": "N3O Data Export",
    "version": "17.0.0",
    "extensions": [
        {
            "type": "workspaceView",
            "alias": "N3O.WorkspaceView.DataExport",
            "name": "N3O Data Export",
            "element": "/App_Plugins/N3O.Umbraco.Data.Export/data-export.js",
            "meta": {
                "label": "Export",
                "pathname": "export",
                "icon": "icon-download-alt"
            },
            "conditions": [
                {
                    "alias": "Umb.Condition.WorkspaceAlias",
                    "match": "Umb.Workspace.Document"
                },
                {
                    "alias": "N3O.Condition.WorkspaceVisibility",
                    "endpoint": "/umbraco/backoffice/api/ExportVisibility"
                }
            ]
        }
    ]
}
```

Each field explained:

| Field | Meaning |
|-------|---------|
| `"id"` | Unique package identifier; must match the `App_Plugins/<id>` folder name |
| `"version"` | Package version, informational |
| `"type": "workspaceView"` | Registers a tab inside a workspace (think: a panel that appears when you open a content item) |
| `"alias"` | Unique extension key; referenced in conditions and extension APIs — like a `[ManifestAttribute]` key in C# |
| `"element"` | URL path to the compiled JS module to load; Umbraco imports it as an ES module |
| `"meta.label"` | The tab label shown to the editor |
| `"meta.pathname"` | The URL path segment when this tab is active (`/umbraco/content/<id>/export`) |
| `"meta.icon"` | The icon shown on the tab |
| `"conditions"` | An array of conditions that must all be true for this tab to appear — see below |

**Conditions:**
- `Umb.Condition.WorkspaceAlias` — this tab only appears when the workspace is a Document workspace (not Media, not Members). `match` is the expected workspace alias.
- `N3O.Condition.WorkspaceVisibility` — a custom N3O condition (implemented in `@n3o/backoffice-core`). It calls the `endpoint` URL with the current document key and shows the tab only if the server returns `true`. This is how server-side per-node / per-user gating works, equivalent to `IContentAppFactory.GetContentAppFor` in Umbraco 13. See concept [10 — the-n3o-bridge-pattern](../concepts/10-the-n3o-bridge-pattern.md) for full details.

### 3b. Browser loads the JS module

When the user navigates to a document that satisfies both conditions, the backoffice fetches `/App_Plugins/N3O.Umbraco.Data.Export/data-export.js` as an **ES module** (see [04 — es-modules-and-import-maps](../concepts/04-es-modules-and-import-maps.md)). The module registers the `<n3o-data-export>` custom element via the `@customElement` decorator. The backoffice then stamps out `<n3o-data-export>` in the DOM. The browser calls its constructor.

### 3c. Shell → React mount

The shell (`data-export.ts`) does three things in its constructor:

1. **Creates a Shadow DOM** — an isolated subtree that prevents the parent page's CSS from leaking in (see concept 06). It creates a `<div>` inside the shadow as the React mount point.
2. **Consumes `UMB_DOCUMENT_WORKSPACE_CONTEXT`** — Umbraco's context API (think: a typed `IServiceProvider`). When the context is available it observes the `unique` observable (the document's GUID key) and stores it.
3. **Consumes `UMB_AUTH_CONTEXT` (via `UmbAuthFetchMixin`)** — the mixin (from `@n3o/backoffice-core`) wraps the constructor to consume the auth context and build an `authFetch` function that automatically attaches the OAuth bearer token to every request. See [10 — the-n3o-bridge-pattern](../concepts/10-the-n3o-bridge-pattern.md).

Whenever `contentKey` or `authFetch` changes the shell calls `#render()`, which calls `this.#root.render(createElement(DataExportApp, { contentKey, authFetch }))`. This is the React mount — it is equivalent to `ReactDOM.render(<DataExportApp … />, mountPoint)` but in the React 18+ API.

> **C# analogy:** the shell is like a thin ASP.NET controller action that reads two things from the DI container (`contentKey` from the workspace context, `authFetch` from the auth context) and passes them as constructor arguments to a service (`DataExportApp`) that does the real work.

### 3d. React renders the UI

`DataExportApp` is a React function component. It calls two custom hooks to get its data and action functions, then renders sub-components, passing data down via **props** (think: constructor parameters).

### 3e. Authenticated server calls

Whenever the component needs data from the C# backend it calls `authFetch(url, options)`. `authFetch` is the function built by `UmbAuthFetchMixin` — it is a thin wrapper around the browser's built-in `fetch` that:
1. Calls `UMB_AUTH_CONTEXT.getOpenApiConfiguration().token()` to retrieve the current OAuth bearer token.
2. Adds `Authorization: Bearer <token>` to the request headers.
3. Forwards the request to `fetch`.

Without this, any call to the N3O API controllers (which are protected with `[Authorize]`) returns HTTP 401.

### 3f. Create → poll → download

The export is a long-running server operation so it uses a create-then-poll pattern:
1. POST to `/api/Exports/export/<contentKey>/<contentTypeAlias>` → returns `{ id: "..." }`.
2. GET `/api/Exports/export/<id>/progress` every 2.5 seconds until `isComplete === true`.
3. GET `/api/Exports/export/<id>/file` → receives the file as a binary blob → triggers a browser download via a synthetic `<a download>` click.

The full logic lives in `use-export.ts` and is walked in detail in section 4 below.

---

## 4. File-by-file walkthrough

### `umbraco-package.json`

Already covered in section 3a. Key point: the `"element"` path is an absolute URL path (starts with `/`), not a relative path and not a module specifier. Umbraco fetches it directly as a script URL.

---

### `package.json`

```json
{
    "name": "n3o-umbraco-data-export",
    "version": "1.0.0",
    "private": true,
    "type": "module",
    "scripts": {
        "build": "tsc --noEmit && vite build",
        "watch": "vite build --watch"
    },
    "devDependencies": {
        "@n3o/backoffice-core": "*",
        "@n3o/build": "*"
    }
}
```

Key points:

- `"private": true` — prevents this package from being accidentally published to a registry. Always set on internal workspace packages.
- `"type": "module"` — tells Node.js to treat `.js` files in this package as ES modules (not CommonJS). Required because the source and build tooling all use `import`/`export`.
- `"build": "tsc --noEmit && vite build"` — the two-step build: first `tsc` type-checks without emitting any files (it finds errors without producing output), then `vite build` actually compiles. If `tsc` reports errors the `&&` stops the chain before Vite runs.
- `"watch": "vite build --watch"` — re-runs the Vite build whenever a source file changes. Used during active development.
- `"devDependencies"` with `"*"` — in an npm workspace the `*` version means "use whatever version is in the workspace". Because `@n3o/backoffice-core` and `@n3o/build` are local workspace packages (not published to a registry), `*` is the correct specifier. They are `devDependencies` (not `dependencies`) because they are only needed at build time — after compilation they are either bundled in or externalised.

---

### `tsconfig.json`

```json
{
    "extends": "@n3o/build/tsconfig",
    "compilerOptions": {
        "jsx": "react-jsx"
    },
    "include": ["src"]
}
```

- `"extends": "@n3o/build/tsconfig"` — pulls in all shared TypeScript settings from `src/N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/base.json` (resolved via the workspace symlink). That base sets `strict`, `noUnusedLocals`, `ES2022` target, `experimentalDecorators` (needed for the `@customElement` decorator in the shell), and `"types": ["@umbraco-cms/backoffice/extension-types"]` (ambient types for Umbraco's extension system). See [buildconfig](./buildconfig.md).
- `"jsx": "react-jsx"` — this is the only addition. It tells TypeScript how to transform JSX syntax: `"react-jsx"` means use React 17+'s automatic JSX transform, which does not require `import React from 'react'` at the top of every `.tsx` file. This is why you do not see that import anywhere in the source.
- `"include": ["src"]` — type-check only the files under `src/`. This excludes `vite.config.ts` from project type-checking (Vite config has its own types).

> **C# analogy:** `tsconfig.json` is like the `<PropertyGroup>` section of a `.csproj` — it configures the compiler. `extends` is like `<Import Project="$(SharedTargets)" />` — it inherits settings from a shared file.

---

### `vite.config.ts`

```typescript
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

The `n3oPluginConfig` helper (from `src/N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/vite-config.js`) wraps Vite's `defineConfig` with standard N3O defaults:

- `entries` — a map of `output-name → source-entry`. The key `'N3O.Umbraco.Data.Export/data-export'` tells Vite to write the output to `<outDir>/N3O.Umbraco.Data.Export/data-export.js`. Combined with `outDir: '../../wwwroot/App_Plugins'`, the output lands at `wwwroot/App_Plugins/N3O.Umbraco.Data.Export/data-export.js` — exactly where the manifest's `"element"` URL points.
- `outDir: '../../wwwroot/App_Plugins'` — relative to the `Apps/N3O.Umbraco.Data.Export/` folder, two levels up gets to `N3O.Umbraco.Data.StaticAssets/`, then `wwwroot/App_Plugins/`.
- `react: true` — marks `react`, `react-dom`, `react-dom/client`, and `react/jsx-runtime` as **external**. This means they are NOT copied into `data-export.js`. They will be resolved at runtime from the shared React runtime via the import map. This is how a single shared copy of React is maintained across all N3O plugins — if each bundled its own copy there would be multiple Reacts on the page, which breaks hooks.
- `additionalExternals: ['@n3o/backoffice-core']` — likewise externalises `@n3o/backoffice-core` (the shared auth-fetch package). At runtime the import map resolves this to the `@n3o/backoffice-core` entry provided by the BackofficeCore plugin.

> **C# analogy:** externals are like `<Reference>` entries in a `.csproj` that reference assemblies provided by the host application (e.g. ASP.NET Core's own assemblies) — they are listed as dependencies but not copied into the output.

---

### `src/data-export.ts` — the web-component shell

This is the bridge between Umbraco's web-component world and the React world. It is the most Umbraco-specific file in the app and the one you need to understand before reading any other shell file in the codebase.

```typescript
// src/Data/N3O.Umbraco.Data.StaticAssets/Apps/N3O.Umbraco.Data.Export/src/data-export.ts
import { customElement } from '@umbraco-cms/backoffice/external/lit';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import { UMB_DOCUMENT_WORKSPACE_CONTEXT } from '@umbraco-cms/backoffice/document';
import { UmbAuthFetchMixin } from '@n3o/backoffice-core';
import { createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { DataExportApp } from './data-export-app';
import type { AuthFetch } from '@n3o/backoffice-core';

const elementName = 'n3o-data-export';

@customElement(elementName)
export class N3oDataExportElement extends UmbAuthFetchMixin(UmbElementMixin(HTMLElement)) {
```

Line by line:

- `@customElement(elementName)` — a TypeScript **decorator** (like a C# `[Attribute]`). It calls `customElements.define('n3o-data-export', N3oDataExportElement)` for you, registering this class as the browser's implementation of the `<n3o-data-export>` HTML tag.
- `extends UmbAuthFetchMixin(UmbElementMixin(HTMLElement))` — **mixin chaining**, a JavaScript pattern for multiple inheritance. Read right-to-left:
  1. Start with the browser's base `HTMLElement`.
  2. Wrap with `UmbElementMixin` — adds `consumeContext`, `observe`, and the context provider infrastructure (Umbraco's DI for components).
  3. Wrap with `UmbAuthFetchMixin` — adds `this.authFetch` by consuming `UMB_AUTH_CONTEXT`. When auth changes it calls the hook `authFetchChanged`. This mixin is from `@n3o/backoffice-core/src/auth-fetch.ts`.

> **C# analogy:** mixin chaining is like having a class inherit from a generic base that itself inherits from another generic base: `class MyService : AuthMixin<UmbracoMixin<BaseService>>`. JavaScript doesn't have interfaces with default implementations, so mixins are the idiom for composable behaviour.

```typescript
    #root?: Root;
    #mount: HTMLDivElement;
    #contentKey: string | null = null;
```

The `#` prefix is JavaScript's native private field syntax (not TypeScript's `private` keyword). `#root` is the React root (the object that owns the React render tree). `#mount` is the DOM `<div>` inside the shadow that React renders into.

```typescript
    constructor() {
        super();

        const shadow = this.attachShadow({ mode: 'open' });
        this.#mount = document.createElement('div');
        shadow.appendChild(this.#mount);

        this.consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, (context) => {
            if (!context) { return; }
            this.observe(
                context.unique,
                (unique) => {
                    if (unique && unique !== this.#contentKey) {
                        this.#contentKey = unique;
                        this.#render();
                    }
                },
                '_observeUnique'
            );
        });
    }
```

- `this.attachShadow({ mode: 'open' })` — creates a Shadow DOM root. React renders inside this shadow, isolated from the Umbraco shell's CSS. `mode: 'open'` means the shadow can still be accessed from JavaScript (useful for debugging).
- `this.consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, callback)` — subscribes to the workspace context. The callback fires (possibly asynchronously) when the context becomes available. Think of this as resolving a keyed service from DI: the key is `UMB_DOCUMENT_WORKSPACE_CONTEXT` (a typed token), the value is an object that has an `observe`-able `unique` property (the document's GUID).
- `this.observe(context.unique, callback, '_observeUnique')` — subscribes to changes on the `unique` observable. Each time the GUID changes (e.g. the editor navigates to a different document) the callback fires and `#render()` is called. The string `'_observeUnique'` is a subscription key used internally for cleanup.

```typescript
    authFetchChanged(_authFetch: AuthFetch | null): void {
        this.#render();
    }

    connectedCallback(): void {
        super.connectedCallback?.();
        this.#root ??= createRoot(this.#mount);
        this.#render();
    }

    disconnectedCallback(): void {
        super.disconnectedCallback?.();
        this.#root?.unmount();
        this.#root = undefined;
    }
```

- `authFetchChanged` — called by `UmbAuthFetchMixin` whenever `authFetch` changes (e.g. after the user logs in). Triggers a re-render so React picks up the new value.
- `connectedCallback` — browser lifecycle method called when the element is inserted into the DOM (analogous to an ASP.NET middleware being added to the pipeline). `createRoot` creates the React root lazily (`??=`). Then `#render()` mounts the React tree.
- `disconnectedCallback` — called when the element is removed from the DOM. `#root.unmount()` tears down the React tree (runs cleanup effects, etc.) and frees memory.

```typescript
    #render(): void {
        this.#root?.render(
            createElement(DataExportApp, {
                contentKey: this.#contentKey,
                authFetch: this.authFetch,
            }),
        );
    }
```

`createElement(DataExportApp, { … })` is the raw-API equivalent of the JSX `<DataExportApp contentKey={…} authFetch={…} />`. The shell uses `createElement` rather than JSX because `data-export.ts` has no `.tsx` extension (it is not a React file itself — it is just the bridge). React then re-renders the component tree with the new props.

---

### `src/types.ts` — shared interfaces

```typescript
export interface ContentType {
    alias: string;
    name: string;
}

export interface ContentMetadata {
    id: string;
    name: string;
    autoSelected: boolean;
    displayOrder: number;
    selected: boolean;
}

export interface ExportableProperty {
    alias: string;
    columnTitle: string;
    selected: boolean;
}

export interface ExportProgressResponse {
    isComplete: boolean;
    text: string;
    id: string;
}

export interface CreateExportResponse {
    id: string;
}
```

This file is pure TypeScript — no runtime behaviour, only type definitions. It is the frontend equivalent of a C# DTOs file.

> **C# analogy:** every interface here corresponds to a C# class or record returned by a backend API endpoint. `ContentType`, `ContentMetadata`, `ExportableProperty` are lookup/reference types fetched once. `CreateExportResponse` and `ExportProgressResponse` are the responses of the create and poll endpoints.

`selected: boolean` on `ContentMetadata` and `ExportableProperty` is not a server-side field — it is UI state added client-side when the server data is loaded (`m.selected = m.autoSelected` in the hook). Putting it in the interface means the same object carries both the server data and the selection state, avoiding a separate parallel structure. This is a deliberate design choice: the data and the checkbox state travel together.

---

### `src/use-export.ts` — custom React hooks

This is the heart of the app. It is a great place to learn `useState`, `useEffect`, and `useRef` concretely.

#### What is a custom hook?

In React, a **hook** is any function that starts with `use` and calls other hooks. The `useState`, `useEffect`, `useRef` functions are built-in hooks provided by React. A **custom hook** is one you write yourself to extract and reuse stateful logic.

> **C# analogy:** a custom hook is like a service class injected by DI — it encapsulates logic and state that multiple components can share. The difference is that hook state is per-component-instance (each component that calls `useExportRun()` gets its own isolated state), not a singleton.

The file exports two hooks: `useExportServerData` and `useExportRun`.

#### `useExportServerData` — loading reference data

```typescript
export function useExportServerData(
    contentKey: string | null,
    authFetch: AuthFetch | null,
): ExportServerData {
    const [contentTypes, setContentTypes] = useState<ContentType[]>([]);
    const [metadatas, setMetadatas] = useState<ContentMetadata[]>([]);
```

- `useState<ContentType[]>([])` — declares a state variable `contentTypes` with initial value `[]`. `setContentTypes` is the setter. Every time `setContentTypes` is called with a new value, React re-renders the component. This is the fundamental React state model: data lives in state, state changes trigger re-renders. It is analogous to a `[ObservableProperty]` in a MVVM ViewModel, or a reactive property in a Lit element.

```typescript
    useEffect(() => {
        if (!contentKey || !authFetch) {
            return;
        }

        let active = true;

        const load = async (): Promise<void> => {
            const [typesRes, metaRes] = await Promise.all([
                authFetch(`/umbraco/backoffice/api/ContentTypes/${contentKey}/relations?type=descendant`, {
                    headers: { Accept: 'application/json' },
                }),
                authFetch('/umbraco/backoffice/api/Exports/lookups/contentMetadata', {
                    headers: { Accept: 'application/json' },
                }),
            ]);

            const types = (await typesRes.json()) as ContentType[];
            const metadata = (await metaRes.json()) as ContentMetadata[];

            for (const m of metadata) {
                m.selected = m.autoSelected;
            }
            metadata.sort((a, b) => a.displayOrder - b.displayOrder);

            if (active) {
                setContentTypes(types);
                setMetadatas(metadata);
            }
        };

        void load();

        return () => {
            active = false;
        };
    }, [contentKey, authFetch]);
```

`useEffect(fn, deps)` — registers a side effect that React runs after rendering. The `deps` array `[contentKey, authFetch]` controls when it runs: the effect fires on first render and then again whenever either `contentKey` or `authFetch` changes. If both are `null` (e.g. at initial render before the shell has consumed the Umbraco context), the effect returns early without doing anything.

Inside the effect:
- `let active = true` + `if (active) { … }` — a **stale closure guard**. The effect is asynchronous (`async`). By the time the `await Promise.all(…)` resolves, the component may have been unmounted or the effect may have re-fired (if `contentKey` changed). The `active` flag ensures we only call `setContentTypes`/`setMetadatas` if this particular invocation of the effect is still the current one. Without this guard you would see React "can't perform a state update on an unmounted component" warnings.
- `return () => { active = false; }` — the **cleanup function**. React calls this before the effect re-fires and when the component unmounts. It sets `active = false`, which causes any in-flight `load()` call to silently discard its result.
- `Promise.all([…])` — fires both HTTP requests in parallel (like `Task.WhenAll`), then waits for both to resolve. This halves the waiting time versus sequential `await`.
- The `metadata` array is mutated in place to set `selected = autoSelected` and then sorted by `displayOrder`. This happens before `setMetadatas`, so by the time React has the data it already has the right default selections.

> **Why `void load()`?** The `useEffect` callback must return either `undefined` or a cleanup function — it cannot be `async`. The pattern `void load()` calls the async function but discards the Promise (which is fine because errors are handled inside `load`). The `void` keyword explicitly acknowledges that the return value is intentionally unused, which satisfies the TypeScript linter.

#### `useExportRun` — the create → poll → download flow

```typescript
export function useExportRun(authFetch: AuthFetch | null): ExportRun {
    const [processing, setProcessing] = useState<boolean>(false);
    const [progress, setProgress] = useState<string>('');
    const [errorMessage, setErrorMessage] = useState<string | null>(null);

    const generationRef = useRef<number>(0);
    const pollTimerRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);
```

Three state variables track what the UI should show:
- `processing` — whether an export is in flight (disables all inputs, shows the progress bar and "Exporting…" button).
- `progress` — the text message from the last `/progress` poll (e.g. "Exporting page 3 of 10").
- `errorMessage` — set on any failure; renders the red error box.

Two **refs** (`useRef`):
- `generationRef` — a counter incremented each time `doExport` is called. The `poll` closure captures the generation at the moment it was started and bails out silently if a newer generation has started. This is the cancellation mechanism for the polling loop.
- `pollTimerRef` — holds the `setTimeout` handle of the pending poll tick. When a new export starts (or the component unmounts) this is cleared immediately, stopping the previous poll.

> **`useRef` vs `useState`:** `useRef` holds a value that persists across renders but does NOT trigger a re-render when changed. Use `useRef` for values that are purely internal bookkeeping (timers, cancellation flags, DOM references) and `useState` for values that should cause the UI to update. If `generationRef` were a `useState` it would cause an extra render on every export — wasteful and potentially buggy.

```typescript
    useEffect(() => {
        return () => {
            clearTimeout(pollTimerRef.current);
            generationRef.current += 1;
        };
    }, []);
```

An effect with an empty `[]` dependency array runs once on mount and its cleanup runs once on unmount. This cleanup cancels any pending poll timer and increments the generation counter so any in-flight `poll` closure discards its result.

```typescript
    const poll = (exportId: string): Promise<ExportProgressResponse> => {
        const gen = generationRef.current;

        const executePoll = async (
            resolve: (value: ExportProgressResponse) => void,
            reject: (reason?: unknown) => void
        ): Promise<void> => {
            if (generationRef.current !== gen) {
                reject(new Error('poll cancelled'));
                return;
            }

            const getProgress = await authFetch!(…);

            if (!getProgress.ok) { … }

            const progressRes = (await getProgress.json()) as ExportProgressResponse;

            if (progressRes.isComplete === true) {
                resolve(progressRes);
            } else {
                setProgress(progressRes.text);
                pollTimerRef.current = setTimeout(() => void executePoll(resolve, reject), 2500);
            }
        };

        return new Promise(executePoll);
    };
```

`poll` returns a `Promise` that resolves only when `isComplete === true`. Internally `executePoll` is a recursive async function: it calls itself again via `setTimeout` after 2500 ms if the export is not yet complete. Each scheduled call captures the `setTimeout` handle in `pollTimerRef.current`, which allows cancellation.

The stale-generation check `if (generationRef.current !== gen)` is evaluated at the start of each tick: if `doExport` was called again between ticks, the counter has incremented and this tick is discarded. This prevents a completed poll from calling `resolve` after a newer export has already started.

```typescript
    const doExport = async (…): Promise<void> => {
        clearTimeout(pollTimerRef.current);
        generationRef.current += 1;

        setProcessing(true);
        setProgress('');
        setErrorMessage(null);

        // … validation …

        const createExport = await authFetch!(
            `/umbraco/backoffice/api/Exports/export/${contentKey}/${contentTypeAlias}`,
            { method: 'POST', body: JSON.stringify(req), … }
        );

        if (!createExport.ok) { … processingError(…); return; }

        const createRes = (await createExport.json()) as CreateExportResponse;

        poll(createRes.id)
            .then(async (res) => {
                const exportFile = await authFetch!(…);
                // … blob → synthetic <a download> click → revokeObjectURL …
                setProcessing(false);
            })
            .catch(() => { /* already handled in poll */ });
    };
```

The download sequence at the end deserves explanation:
1. `exportFile.blob()` — reads the response body as a binary `Blob` (equivalent to a `byte[]`).
2. `window.URL.createObjectURL(newBlob)` — creates a temporary `blob:` URL that points to the in-memory bytes. Think of it as a handle to a memory buffer.
3. A synthetic `<a href="blob:…" download="filename">` element is created, appended to the body, clicked programmatically, and then removed. The browser's download manager intercepts the click and saves the file. There is no server-driven `Content-Disposition: attachment` response header needed for the click to work — `download="filename"` is sufficient.
4. `window.URL.revokeObjectURL(blobUrl)` — releases the in-memory buffer. Forgetting this causes a memory leak.

---

### `src/export-options.tsx` — presentational component

```typescript
interface ExportOptionsProps {
    contentTypes: ContentType[];
    contentType: ContentType | null;
    format: string;
    includeUnpublished: boolean;
    processing: boolean;
    onContentTypeChange: (event: React.ChangeEvent<HTMLSelectElement>) => void;
    onFormatChange: (value: string) => void;
    onIncludeUnpublishedChange: (checked: boolean) => void;
}
```

This is a **presentational component** — it displays data and notifies the parent of user actions, but holds no state of its own. All its inputs come in as `props` (the typed `ExportOptionsProps` parameter object). This is the React equivalent of a C# record passed to a Razor partial view.

The callback props (`onContentTypeChange`, `onFormatChange`, `onIncludeUnpublishedChange`) are functions passed from the parent. When the user changes a value, the component calls the callback, and the parent updates state. This is the **props-down, events-up** pattern.

```typescript
export function ExportOptions({ … }: ExportOptionsProps) {
    return (
        <uui-box headline="Export options">
            <umb-property-layout label="Content type" description="…" mandatory>
                <div slot="editor">
                    <select
                        className="nativeSelect"
                        value={contentType?.alias ?? ''}
                        onChange={onContentTypeChange}
                        disabled={processing || contentTypes.length === 0}>
                        …
                    </select>
                </div>
            </umb-property-layout>
            …
        </uui-box>
    );
}
```

**The native-HTML-controls-inside-uui-box pattern:**

`<uui-box>` and `<umb-property-layout>` are Umbraco's own web components (from the `@umbraco-ui` library). They provide the backoffice's standard card and property-row layout respectively. Inside them, however, the interactive controls are plain HTML: `<select>`, `<input type="radio">`, `<input type="checkbox">`. They are NOT Umbraco's `<uui-select>`, `<uui-radio>`, or `<uui-toggle>` web components.

This is deliberate. Umbraco's UUI form controls are built on Lit and use the `FormControlMixin`. When rendered by React inside a Shadow DOM they fail to mount and produce console errors (a known incompatibility in v17). Plain HTML controls rendered inside `<uui-box>` work correctly and are styled via CSS custom properties (`--uui-color-*`, `--uui-border-radius`, etc.) that inherit through the shadow boundary.

`className` (not `class`) — in JSX, `class` is a reserved JavaScript keyword, so React uses `className` instead. It maps to the HTML `class` attribute.

`contentType?.alias ?? ''` — the optional chaining (`?.`) returns `undefined` if `contentType` is `null`; the nullish coalescing (`??`) then substitutes `''`. This keeps the `<select>` in controlled mode (it always has an explicit `value`) even before the user picks a content type.

---

### `src/selectable-field-list.tsx` — generic reusable component

```typescript
interface SelectableFieldListProps<T> {
    headline: string;
    selectedCount: number;
    items: T[];
    getKey: (item: T) => string;
    getLabel: (item: T) => string;
    getChecked: (item: T) => boolean;
    processing: boolean;
    onToggle: (item: T, checked: boolean) => void;
    onSelectAll: () => void;
    onClear: () => void;
    emptyState: React.ReactNode;
}
```

`SelectableFieldList` is a **generic component** — `T` is a type parameter (the same concept as C# generics, `IEnumerable<T>`). This allows the same component to render a list of `ContentMetadata` objects or a list of `ExportableProperty` objects without being hard-coded to either.

Because the component does not know the shape of `T`, caller-supplied **accessor functions** abstract field access:
- `getKey(item)` — returns a unique string key for each item (used as React's `key` prop to track list items across re-renders — analogous to an `Id` in a list binding).
- `getLabel(item)` — returns the display string.
- `getChecked(item)` — returns the checked state.

In `data-export-app.tsx` the component is used twice with different type arguments:

```typescript
// For metadata — T = ContentMetadata
<SelectableFieldList
    getKey={(m) => m.id}
    getLabel={(m) => m.name}
    getChecked={(m) => m.selected}
    …
/>

// For properties — T = ExportableProperty
<SelectableFieldList
    getKey={(p) => p.alias}
    getLabel={(p) => p.columnTitle}
    getChecked={(p) => p.selected}
    …
/>
```

The accessor functions are **arrow functions** (equivalent to lambda expressions in C#). TypeScript infers `T` from the `items` prop, so you do not need to write `<SelectableFieldList<ContentMetadata> …>` explicitly.

`emptyState: React.ReactNode` — a prop that accepts any renderable React content (a JSX element, a string, `null`). The parent passes different messages depending on context:

```typescript
// Properties panel: distinguish "no type selected yet" from "type has no properties"
emptyState={
    !contentType ? (
        <p className="emptyState">Select a content type to see its exportable properties.</p>
    ) : (
        <p className="emptyState">This content type has no exportable properties.</p>
    )
}
```

This is JSX conditional rendering: `condition ? <TrueElement /> : <FalseElement />` is a ternary expression in JSX. React renders whichever branch evaluates to a value.

---

### `src/data-export-app.tsx` — the container component

This is the **root React component**. It does no rendering itself beyond wiring — it delegates rendering to sub-components. Its responsibilities are:

1. Call both custom hooks.
2. Hold the **selection state** for metadatas and exportable properties (these are mutable lists — the user can toggle items).
3. Load properties when the content type changes (`refreshProperties`).
4. Compute `canExport` — a derived boolean that controls whether the Export button is enabled.
5. Compose sub-components passing down state as props and registering event callbacks.

```typescript
export function DataExportApp({ contentKey, authFetch }: DataExportAppProps) {
    const { contentTypes, metadatas: initialMetadatas } = useExportServerData(contentKey, authFetch);
    const { processing, progress, errorMessage, doExport } = useExportRun(authFetch);
```

**Destructuring assignment** — `{ contentTypes, metadatas: initialMetadatas }` destructures the object returned by `useExportServerData`, renaming `metadatas` to `initialMetadatas` to avoid a name collision with the local `metadatas` state.

```typescript
    const [contentType, setContentType] = useState<ContentType | null>(null);
    const [format, setFormat] = useState<string>('excel');
    const [includeUnpublished, setIncludeUnpublished] = useState<boolean>(false);
    const [metadatas, setMetadatas] = useState<ContentMetadata[]>([]);
    const [exportableProperties, setExportableProperties] = useState<ExportableProperty[]>([]);
```

Five local state variables — the component's own "view model". `format` defaults to `'excel'`, `includeUnpublished` to `false`. `metadatas` and `exportableProperties` start empty and are populated when data loads.

```typescript
    useEffect(() => {
        setMetadatas(initialMetadatas);
    }, [initialMetadatas]);
```

This syncs `initialMetadatas` (from the hook, a fresh array reference each time the server responds) into local `metadatas` state. It must be a copy in local state because the user can toggle checkboxes — which mutates `selected` — without wanting those changes reflected back to the hook's state. The separation between `initialMetadatas` (server snapshot) and `metadatas` (interactive local state) makes the data flow explicit.

```typescript
    const canExport = !!contentType && hasSelection && !processing && !!authFetch;
```

`canExport` is a **derived value** — computed from state on each render, not stored in its own `useState`. This is a key React idiom: if a value can be calculated from existing state, do not store it separately. Keeping it derived ensures it is always consistent. The `!!` converts a possibly-null value to a `boolean` (double negation, same as `x !== null && x !== undefined`).

**The `<style>` injection pattern:**

```typescript
    return (
        <div className="n3o-data-export">
            …
            <style>{styles}</style>
        </div>
    );
```

Because React renders inside a Shadow DOM, the Umbraco shell's stylesheets do not reach it. There is no `<link>` tag; instead, the CSS is imported as a plain string (via the `?inline` Vite import — see below) and injected as an inline `<style>` element inside the component's root `<div>`. This ensures the CSS is present without requiring a separate network request.

---

### `src/data-export-app.css` — scoped stylesheet

The CSS is structured as a **flat list of class rules all scoped under `.n3o-data-export`**. This is a manual namespacing strategy (no CSS Modules, no CSS-in-JS library) that prevents conflicts with other plugins or Umbraco's own styles.

All sizing and colour values use **CSS custom properties** (CSS variables) from the Umbraco Design Tokens library:

| Variable | Meaning |
|----------|---------|
| `--uui-size-space-4` | Standard spacing unit (multiples of a base grid) |
| `--uui-color-text` | Primary text colour (adapts to light/dark mode) |
| `--uui-color-surface` | Background colour for inputs |
| `--uui-color-border` | Input border colour |
| `--uui-color-focus` | Focus ring colour |
| `--uui-color-positive` | Green (success / primary action) |
| `--uui-color-danger` | Red (error background) |
| `--uui-border-radius` | Standard corner radius |

Using these variables means the plugin's UI automatically adapts to Umbraco's light/dark mode and brand colour settings without any JavaScript involvement.

The `.btn` class definitions replicate Umbraco button appearance for the native `<button>` elements. Because UUI's `<uui-button>` cannot be used safely inside React in v17 (the same web-component-in-React incompatibility noted above), the styling is reproduced in CSS instead.

---

### `src/data-export-app.css?inline` — the Vite `?inline` import

In `data-export-app.tsx`:

```typescript
import styles from './data-export-app.css?inline';
```

The `?inline` suffix is a **Vite query parameter** — a special instruction to the bundler. Without `?inline`, Vite would try to inject a `<link rel="stylesheet">` into the document `<head>` — which does not work inside a Shadow DOM. With `?inline`, Vite instead imports the CSS file as a plain string. The variable `styles` is a string containing the entire contents of `data-export-app.css`.

This is declared as a valid import in `src/N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/vite-env.d.ts`:

```typescript
declare module '*.css?inline' {
    const css: string;
    export default css;
}
```

This tells TypeScript: "any import matching the pattern `*.css?inline` exports a default string." Without this declaration TypeScript would refuse the import as an unknown module.

---

### `src/uui-react.d.ts` — ambient JSX type declarations

```typescript
import 'react';

declare module 'react' {
    namespace JSX {
        interface IntrinsicElements {
            'uui-box': any;
            'uui-icon': any;
            'uui-loader-bar': any;
            'umb-property-layout': any;
        }
    }
}
```

TypeScript's JSX type system checks that every tag you use in JSX is either a known HTML element or a registered component. Standard HTML tags like `<div>`, `<select>`, `<input>` are already in TypeScript's `lib.dom.d.ts`. Custom elements like `<uui-box>` are not — TypeScript would error on them.

This file **augments** the `react` module's `JSX.IntrinsicElements` interface to add the four UUI tags used in this app. Each is typed as `any` because UUI's web component types are Lit-based (they use `PropertyDeclaration`, not React JSX `DetailedHTMLProps`) and are not compatible with the React JSX type system. `any` opts TypeScript out of type-checking the props of those elements, which is a pragmatic trade-off.

> **C# analogy:** this is like adding partial class entries to extend an auto-generated class, or adding entries to a `Dictionary<string, object>` type mapping. It is a module augmentation — extending a type defined elsewhere without modifying its source.

---

## 5. Concepts demonstrated

| Concept | Where demonstrated | Concepts doc |
|---------|--------------------|--------------|
| npm workspace + `"*"` version | `package.json` `devDependencies` | [03](../concepts/03-node-npm-and-the-workspace.md) |
| Vite library mode + externals | `vite.config.ts` | [05](../concepts/05-vite-and-the-build.md) |
| Vite `?inline` CSS import | `import styles from './data-export-app.css?inline'` | [05](../concepts/05-vite-and-the-build.md) |
| Shadow DOM isolation | `this.attachShadow({ mode: 'open' })` in shell | 06 |
| `@customElement` decorator | Shell class declaration | 06, [07](../concepts/07-lit.md) |
| Mixin chaining (`UmbAuthFetchMixin(UmbElementMixin(HTMLElement))`) | Shell base class | [10](../concepts/10-the-n3o-bridge-pattern.md) |
| Context consumption (`consumeContext`, `observe`) | Shell constructor | 09 |
| React `createElement` without JSX | `#render()` in shell | 08 |
| `createRoot` / `unmount` lifecycle | `connectedCallback` / `disconnectedCallback` | 08 |
| Custom hooks (`useExportServerData`, `useExportRun`) | `use-export.ts` | 08 |
| `useState` for reactive UI state | `use-export.ts`, `data-export-app.tsx` | 08 |
| `useEffect` with cleanup + stale-closure guard | `useExportServerData` | 08 |
| `useRef` for mutable non-rendering values | `generationRef`, `pollTimerRef` in `useExportRun` | 08 |
| Polling with generation-counter cancellation | `poll()` + `generationRef` | 08 |
| Props-down / events-up composition | `ExportOptions`, `SelectableFieldList` | 08 |
| Generic React component (`<T>`) | `SelectableFieldList<T>` | 08 |
| `canExport` derived value (no extra `useState`) | `data-export-app.tsx` line 73 | 08 |
| Native HTML controls inside `uui-box` / `umb-property-layout` | `export-options.tsx` | [10](../concepts/10-the-n3o-bridge-pattern.md) |
| CSS custom properties (design tokens) | `data-export-app.css` | 09 |
| Inline `<style>` injection into Shadow DOM | `<style>{styles}</style>` at bottom of `DataExportApp` | [10](../concepts/10-the-n3o-bridge-pattern.md) |
| Ambient JSX type augmentation (`uui-react.d.ts`) | `uui-react.d.ts` | [10](../concepts/10-the-n3o-bridge-pattern.md) |
| `workspaceView` extension type | `umbraco-package.json` `"type"` | 09 |
| Two-condition manifest gating (workspace alias + visibility) | `umbraco-package.json` `"conditions"` | 09, [10](../concepts/10-the-n3o-bridge-pattern.md) |
| Create → poll → blob download pattern | `doExport` + `poll` in `use-export.ts` | — |
| `authFetch` bearer-token wrapper | Used throughout hooks | [10](../concepts/10-the-n3o-bridge-pattern.md) |

---

## 6. Gotchas

**`authFetch` is `null` at first render.** The `UmbAuthFetchMixin` resolves `authFetch` asynchronously (it waits for `UMB_AUTH_CONTEXT` to be provided). On first render `authFetch` is `null`. Both hooks guard against this (`if (!authFetch) return`). `canExport` includes `!!authFetch`. Do not attempt to use `authFetch!` (TypeScript non-null assertion) at the top level of a component — only safe inside callbacks/effects that are already guarded.

**`contentKey` is also `null` at first render.** Same reason: `UMB_DOCUMENT_WORKSPACE_CONTEXT` is also consumed asynchronously. Both are `null | string` in the props type.

**UUI web components cannot be used as interactive form controls in React.** `<uui-input>`, `<uui-button>`, `<uui-toggle>` — these are form controls built on Lit's `FormControlMixin`. They fail to mount when rendered by React in a Shadow DOM (v17 known issue). The solution is to use native `<input>`, `<button>`, `<select>` inside UUI layout components (`<uui-box>`, `<umb-property-layout>`). Style them with CSS custom properties to match the Umbraco design system.

**Do not use `uui-react.d.ts` `any` types as a shortcut to skip prop validation.** The `any` is there because UUI Lit types are incompatible with React JSX types, not because we want to skip type safety on our own code. If you add a new UUI tag to the file, be aware that TypeScript will not catch typos in its attributes.

**The `?inline` import will not work in a standard Node.js or Jest environment.** It is a Vite-only feature. If unit tests import the component directly they will need to mock CSS modules or use a Vite-based test runner (e.g. Vitest).

**The generation counter prevents double-download, not network errors.** If the server-side export job is abandoned (e.g. the server restarts mid-poll), `poll` will keep retrying unless it receives a non-`ok` HTTP response. There is no timeout on the overall export operation — it relies on the server eventually either completing or returning an error status.

**`outDir: '../../wwwroot/App_Plugins'` with `emptyOutDir: false`.** The `n3oPluginConfig` helper sets `emptyOutDir: false` in the Vite config. This means Vite does not delete the entire `App_Plugins/` directory before writing output. This is necessary because multiple apps write into the same `App_Plugins/` parent folder (e.g. `data-import.js` and `data-export.js` are siblings). Each app only overwrites its own file.

**The manifest `"id"` must match the sub-folder name.** `"id": "N3O.Umbraco.Data.Export"` must equal the folder name `App_Plugins/N3O.Umbraco.Data.Export/` and the key in `vite.config.ts` entries `'N3O.Umbraco.Data.Export/data-export'`. If any of these three disagree the extension will not load.

**`selected` is added client-side, not returned by the server.** `ContentMetadata.selected` and `ExportableProperty.selected` are initialised by the frontend code after the server response is received. If you add a new interface field that mirrors a server-side property, be careful to distinguish which fields are server-provided and which are UI-only.
