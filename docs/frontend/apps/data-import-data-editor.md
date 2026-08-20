# App — N3O.Umbraco.Data.ImportDataEditor

> **Concepts to read first:**
> [01 — The Big Picture](../concepts/01-the-big-picture.md) |
> [05 — Vite and the Build](../concepts/05-vite-and-the-build.md) |
> [09 — Umbraco Backoffice Extensions](../concepts/09-umbraco-backoffice-extensions.md) |
> [10 — The N3O Bridge Pattern](../concepts/10-the-n3o-bridge-pattern.md)

---

## 1. What it is

**ImportDataEditor** is a **property editor UI** — the custom widget that appears inside a content property when an editor opens a node in the backoffice.

| Axis | Detail |
|------|--------|
| Extension type | `propertyEditorUi` |
| Custom element tag | `<n3o-import-data-editor>` |
| Source directory | `src/Data/N3O.Umbraco.Data.StaticAssets/Apps/N3O.Umbraco.Data.ImportDataEditor/` |
| Compiled output | `src/Data/N3O.Umbraco.Data.StaticAssets/wwwroot/App_Plugins/N3O.Umbraco.Data.ImportDataEditor/import-data-editor.js` |
| Manifest | `…/wwwroot/App_Plugins/N3O.Umbraco.Data.ImportDataEditor/umbraco-package.json` |
| Served via | ASP.NET Core static web assets (RCL pipeline) at `/App_Plugins/N3O.Umbraco.Data.ImportDataEditor/import-data-editor.js` |

### What it does

When an import is queued in the Data module, each import has a set of **fields** — some are plain text values, some are file references. This property editor renders those fields inside a content property: a text box per field (pre-filled with the original `sourceValue`) plus a file picker for fields whose `isFile` flag is set. Changes are written back to the property value; file uploads are sent to the backend immediately via authenticated HTTP calls.

### Property editor UI vs workspace view — key conceptual difference

A **workspace view** is like a whole page/tab on a document. It reads the document ID from context and fetches its own data.

A **property editor UI** is like a single field control on a form. Umbraco hands it a `value` (the serialised data stored for that one property on the node) and expects it to fire a change event whenever the value is modified. There is no separate data fetch — the value arrives as a property, and changes are pushed back via `UmbChangeEvent`. This is exactly analogous to how a custom `ControlValueAccessor` works in Angular, or a controlled input in React.

---

## 2. Files

| File | Role |
|------|------|
| `package.json` | npm package manifest — declares the app identity and workspace dependencies |
| `tsconfig.json` | TypeScript compiler config — extends the shared `@n3o/build/tsconfig` base |
| `vite.config.ts` | Vite build config — calls the shared `n3oPluginConfig` preset |
| `src/import-data-editor.ts` | Web-component shell — owns the Umbraco property-editor contract, manages file uploads, mounts React |
| `src/import-data-editor-app.tsx` | React component — pure UI; renders field rows; fires callbacks for text and file changes |
| `src/import-data-editor-app.css` | Styles — injected into Shadow DOM via `?inline`; scoped to `.n3o-import-fields-editor` |
| `src/uui-react.d.ts` | TypeScript declaration shim — lets React JSX reference `uui-*` / `umb-*` web components |
| `…/umbraco-package.json` | Umbraco manifest — registers the extension with the backoffice at startup |

---

## 3. End-to-end flow

```
dotnet build
  └── npm run build  (tsc --noEmit && vite build)
        └── Vite bundles import-data-editor.ts → import-data-editor.js
              into wwwroot/App_Plugins/N3O.Umbraco.Data.ImportDataEditor/

Browser boots Umbraco backoffice
  └── Scans App_Plugins/**/umbraco-package.json
        └── Finds type:"propertyEditorUi", alias:"N3O.Umbraco.Data.ImportDataEditor"
              └── Loads /App_Plugins/.../import-data-editor.js (ES module)
                    └── @customElement decorator registers <n3o-import-data-editor>

User opens a content node that has a property backed by this editor
  └── Umbraco sets element.value = { reference, fields: [...] }
        └── set value() stores value, calls #render()
              └── React.createElement(ImportDataEditorApp, { value, onTextChange, onFileSelected })
                    └── createRoot(#mount).render(...)
                          └── React renders field rows inside Shadow DOM

User edits a text field
  └── onChange → onTextChange(index, newText)
        └── Shell: mutates value.fields[index].value, re-renders, dispatchEvent(new UmbChangeEvent())
              └── Umbraco: saves updated value to the content property

User picks a file
  └── onChange → onFileSelected(index, file)
        └── Shell: #uploadResource(index, file)
              ├── POST /umbraco/api/Storage/tempUpload  (multipart form)  → storageToken
              └── POST /umbraco/backoffice/api/Imports/queued/{reference}/files  (JSON body)
                    └── On 200: mutates value.fields[index].value = file.name, re-renders, dispatches change
```

Both POST calls are made with `this.authFetch` — the authenticated fetch provided by `UmbAuthFetchMixin` (see [10 — The N3O Bridge Pattern](../concepts/10-the-n3o-bridge-pattern.md)). A plain `fetch()` would return HTTP 401 because those endpoints require a bearer token.

---

## 4. File-by-file

### `package.json`

```json
{
    "name": "n3o-umbraco-data-import-data-editor",
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

- `"name"` — internal package identifier within the npm workspace. Not published.
- `"type": "module"` — every `.js` file produced is an ES Module (`import`/`export`). Required for modern Vite/TS tooling.
- `"build": "tsc --noEmit && vite build"` — two-phase: TypeScript type-checks first (without emitting files), then Vite bundles. `&&` means Vite only runs if type-check passes — equivalent to "compile then link".
- `"watch": "vite build --watch"` — incremental rebuild on file save; for use during active development.
- `"@n3o/backoffice-core": "*"` — declares a dependency on the shared auth-fetch library. The `"*"` means "any version — resolve from the npm workspace". npm symlinks this to `src/N3O.Umbraco.Cms/Apps/N3O.Umbraco.BackofficeCore/`. This is exactly like a `<ProjectReference>` in C#.
- `"@n3o/build": "*"` — the shared Vite + TypeScript config preset (see [05 — Vite and the Build](../concepts/05-vite-and-the-build.md)).

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

- `"extends": "@n3o/build/tsconfig"` — inherits the shared base at `src/N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/base.json`. That base sets `target: "ES2022"`, `strict: true`, `experimentalDecorators: true` (needed for `@customElement`), and `types: ["@umbraco-cms/backoffice/extension-types"]`. It also includes `vite-env.d.ts` from the build package, which declares the `*.css?inline` module type. Think of it as inheriting from a shared `<PropertyGroup>` in `Directory.Build.props`.
- `"jsx": "react-jsx"` — tells TypeScript to understand JSX syntax in `.tsx` files and use the automatic React 17+ transform (no need to `import React` in every component file). Vite's esbuild does the actual JSX compilation at bundle time; this setting only covers the type-check pass (`tsc --noEmit`).
- `"include": ["src"]` — only type-check files inside the `src/` folder of this app.

### `vite.config.ts`

```typescript
import { n3oPluginConfig } from '@n3o/build';

export default n3oPluginConfig({
    entries: {
        'N3O.Umbraco.Data.ImportDataEditor/import-data-editor': 'src/import-data-editor.ts',
    },
    outDir: '../../wwwroot/App_Plugins',
    react: true,
    additionalExternals: ['@n3o/backoffice-core'],
});
```

- `entries` — one entry. The key `'N3O.Umbraco.Data.ImportDataEditor/import-data-editor'` becomes the output path relative to `outDir`, producing `wwwroot/App_Plugins/N3O.Umbraco.Data.ImportDataEditor/import-data-editor.js`. The deterministic filename (no content hash) is required because `umbraco-package.json` references the file by a fixed path.
- `outDir: '../../wwwroot/App_Plugins'` — resolves from `Apps/N3O.Umbraco.Data.ImportDataEditor/` two levels up to the `.csproj`'s `wwwroot/App_Plugins/`. The ASP.NET Core static-web-assets pipeline then serves everything under `wwwroot/` at the web root.
- `react: true` — adds `react`, `react-dom`, `react-dom/client`, and `react/jsx-runtime` to the **externals** list and enables the automatic JSX transform in esbuild. These packages are NOT bundled into the output `.js`; the browser resolves them at runtime via the N3O import map from `ReactRuntime`. See [05 — Vite and the Build](../concepts/05-vite-and-the-build.md) for why this matters (one shared React copy avoids React's hooks invariant).
- `additionalExternals: ['@n3o/backoffice-core']` — also excludes the shared auth-fetch library from the bundle. It is served as its own `.js` file and exposed via the import map so all plugins share one copy.

### `src/import-data-editor.ts` — the web-component shell

This is the most complex file. It is the boundary between the Umbraco backoffice and the React world.

```typescript
import { customElement } from '@umbraco-cms/backoffice/external/lit';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import { UmbAuthFetchMixin } from '@n3o/backoffice-core';
import {
    type UmbPropertyEditorConfigCollection,
    type UmbPropertyEditorUiElement,
} from '@umbraco-cms/backoffice/property-editor';
import { UmbChangeEvent } from '@umbraco-cms/backoffice/event';
import { createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { ImportDataEditorApp, type ImportDataValue } from './import-data-editor-app';
```

The imports span three worlds:
- `@umbraco-cms/backoffice/*` — Umbraco's backoffice SDK (externals, not bundled).
- `@n3o/backoffice-core` — N3O's shared auth-fetch helper (external, not bundled).
- `react`/`react-dom/client` — React (external, not bundled).
- `./import-data-editor-app` — the React component in the same app (bundled in).

**Class declaration:**

```typescript
@customElement(elementName)
export class N3oImportDataEditorElement
    extends UmbAuthFetchMixin(UmbElementMixin(HTMLElement))
    implements UmbPropertyEditorUiElement {
```

The class extends a chain of mixins — a JavaScript pattern for multiple inheritance (C# has no exact equivalent; think of it as implementing multiple interfaces that also contribute concrete behaviour via extension methods). Reading inside-out:

1. `HTMLElement` — the base for all custom elements. Makes this a valid web component.
2. `UmbElementMixin(HTMLElement)` — adds Umbraco's context-consumer API (the `consumeContext` method used inside `UmbAuthFetchMixin`). This is how Umbraco's dependency injection works in the frontend.
3. `UmbAuthFetchMixin(...)` — wraps the result and wires up `this.authFetch` by consuming the `UMB_AUTH_CONTEXT` context. By the time any method calls `this.authFetch`, it holds an authenticated `fetch` wrapper that injects an OAuth Bearer token header. Without this, the `[Authorize]` endpoints in the Data module return HTTP 401.

`implements UmbPropertyEditorUiElement` — declares the contract. Umbraco requires property editor UI elements to expose:
- `get value` / `set value` — the property value (get/set, like a C# property).
- `get config` / `set config` — the data-type configuration (unused here but required by the interface).

**Shadow DOM setup:**

```typescript
constructor() {
    super();
    const shadow = this.attachShadow({ mode: 'open' });
    this.#mount = document.createElement('div');
    shadow.appendChild(this.#mount);
}
```

`attachShadow` creates an isolated DOM subtree. React renders inside `#mount`, which lives inside the shadow root — styles in `import-data-editor-app.css` (injected by React as a `<style>` tag) apply only within this shadow boundary, not to the rest of the page. See [06 — Web Components and Shadow DOM](../concepts/06-web-components-and-shadow-dom.md).

**`value` setter — the entry point from Umbraco:**

```typescript
set value(value: ImportDataValue | undefined) {
    this.#value = value;
    this.#render();
}
```

Umbraco calls this setter when it loads the node. The value is whatever was previously stored for this property — an `ImportDataValue` object with a `reference` string and an array of `ImportField` objects. Each time the setter is called, React is re-rendered with the new value.

**`connectedCallback` / `disconnectedCallback`:**

These are web-component lifecycle hooks — called by the browser when the element is added to or removed from the DOM. They are equivalent to `OnNavigatedTo` / `OnNavigatedFrom` in a .NET MAUI page, or `ComponentDidMount` / `ComponentWillUnmount` in React class components.

- `connectedCallback`: lazily creates the React root (`createRoot`) the first time the element mounts, then calls `#render()`.
- `disconnectedCallback`: destroys the React root to release memory and run React cleanup effects.

**`#onTextChange` — handling text edits:**

```typescript
#onTextChange(index: number, value: string): void {
    if (!this.#value) { return; }
    this.#value.fields[index].value = value;
    this.#render();
    this.#dispatchChange();
}
```

The React component calls this callback when the user edits a text input. The shell:
1. Mutates `this.#value` directly (the source of truth lives here, not in React state).
2. Re-renders React with the updated value.
3. Dispatches `UmbChangeEvent` — the signal Umbraco listens to in order to know the property value changed and needs saving.

This is the fundamental difference from a workspace view: the shell *owns* the data and *notifies Umbraco*, whereas a workspace view reads from Umbraco's document context.

**`#uploadResource` — handling file uploads:**

```typescript
async #uploadResource(index: number, file: File): Promise<void> {
    const reference = this.#value.reference;
    const storageToken = await this.#getStorageToken(file);
    const req = { file: storageToken };

    const res = await this.authFetch(
        `/umbraco/backoffice/api/Imports/queued/${reference}/files`,
        { method: 'POST', headers: { ... }, body: JSON.stringify(req) }
    );

    if (res.status === 200) {
        this.#value.fields[index].value = file.name;
        this.#render();
        this.#dispatchChange();
    }
}
```

File upload is a two-step process:
1. `#getStorageToken` — `POST /umbraco/api/Storage/tempUpload` with a `FormData` body. Returns a storage token (an opaque reference to the temporarily-stored file).
2. The token is included in a JSON body sent to `POST /umbraco/backoffice/api/Imports/queued/{reference}/files`. On success, the field's display value is set to the file name.

Both calls use `this.authFetch` (not plain `fetch`) to include the OAuth Bearer token. A plain `fetch` would receive HTTP 401. `this.authFetch` is `null` briefly during startup before the auth context is ready — the code guards against this with an `alert`.

**`#render()` — bridging to React:**

```typescript
#render(): void {
    this.#root?.render(
        createElement(ImportDataEditorApp, {
            value: this.#value,
            onTextChange: (index, value) => this.#onTextChange(index, value),
            onFileSelected: (index, file) => void this.#uploadResource(index, file),
        }),
    );
}
```

`createElement` is `React.createElement` — the function that JSX compiles down to. The shell passes the current value and two callback functions as **props** (React's equivalent of constructor parameters / method arguments passed to a component). React re-renders efficiently, touching only the DOM nodes that changed. The `void` keyword in `void this.#uploadResource(...)` explicitly discards the returned `Promise`, preventing TypeScript's "Promise-returning function used in callback" warning.

### `src/import-data-editor-app.tsx` — the React component

```typescript
import styles from './import-data-editor-app.css?inline';
```

The `?inline` suffix tells Vite to import the CSS as a raw `string` rather than injecting it into `<head>`. The component later injects it directly: `<style>{styles}</style>`. This is required because styles in `<head>` do not pierce the Shadow DOM boundary. See [05 — Vite and the Build](../concepts/05-vite-and-the-build.md).

**Type definitions exported from this file:**

```typescript
export interface ImportField {
    name: string;
    value: string | null;
    sourceValue: string | null;
    isFile: boolean;
}

export interface ImportDataValue {
    reference: string;
    fields: ImportField[];
}
```

These are TypeScript interfaces — purely compile-time type contracts (like C# `interface` declarations). `ImportDataValue` is the shape of the property value that Umbraco stores and that the shell exposes as `element.value`. `ImportField` describes one editable field within that value.

**The component function:**

```tsx
export function ImportDataEditorApp({ value, onTextChange, onFileSelected }: ImportDataEditorAppProps) {
    const fields = value?.fields ?? [];

    return (
        <div className="n3o-import-fields-editor">
            {fields.map((field, index) => (
                <div className="row-wrapper" key={field.name}>
                    <div className="row-1">
                        <span className="text">{field.name}</span>
                    </div>
                    <div className="row-2">
                        <input
                            type="text"
                            className="custom"
                            value={field.value ?? ''}
                            placeholder={field.sourceValue ?? ''}
                            onChange={(e) => onTextChange(index, e.currentTarget.value)}
                        />
                        {field.isFile ? (
                            <input
                                type="file"
                                onChange={(e) => {
                                    const file = (e.target as HTMLInputElement).files?.[0];
                                    if (file) { onFileSelected(index, file); }
                                }}
                            />
                        ) : null}
                    </div>
                </div>
            ))}
            <style>{styles}</style>
        </div>
    );
}
```

This is a **pure functional React component** — a function that takes props (inputs) and returns JSX (the UI description). It holds no internal state; all state lives in the shell. This is the "controlled component" pattern: the text `<input>` has `value={field.value ?? ''}` — its value is always what the shell says it is. Any user keystroke fires `onChange`, which calls `onTextChange`, which causes the shell to update its state and re-call `#render()`, which passes the new value back as a prop. React then updates only the changed DOM node.

`fields.map(...)` is the JavaScript equivalent of a `foreach` that builds a list of JSX elements. The `key={field.name}` attribute is React's internal identifier for list reconciliation — it must be unique within the list (analogous to a stable record ID).

`{field.isFile ? (...) : null}` is JSX's conditional rendering — equivalent to `if (field.isFile) { ... }`. `null` renders nothing.

`<style>{styles}</style>` injects the imported CSS string directly into the React output, which lands inside the Shadow DOM and therefore scopes the styles correctly.

### `src/import-data-editor-app.css`

```css
.n3o-import-fields-editor .row-wrapper { margin-bottom: 40px; width: 100%; }
.n3o-import-fields-editor .row-1       { display: block; width: 90%; }
.n3o-import-fields-editor .row-2       { display: block; width: 90%; }
.n3o-import-fields-editor .text        { font-weight: bold; }
.n3o-import-fields-editor .custom      { width: 100%; margin-top: 10px; }
```

Standard CSS. All selectors are prefixed with `.n3o-import-fields-editor` to avoid clashing with other components — a manual scoping convention (the Shadow DOM encapsulation provides a second layer of protection, but explicit prefixes are still good practice).

### `src/uui-react.d.ts`

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

TypeScript's JSX type system only knows about standard HTML elements by default. Umbraco's UI Library (`uui-*`) and Umbraco's own elements (`umb-*`) are web components — they exist at runtime but TypeScript has no type information for them when used in JSX. This declaration file extends React's `JSX.IntrinsicElements` type map to add those tags, with `any` types to silence type errors.

This file is NOT imported anywhere — TypeScript picks up `.d.ts` files automatically from the `include` path in `tsconfig.json`. It is a compile-time-only file; it produces no runtime output.

Note that this editor's `.tsx` does not actually use any `uui-*` tags — but the declaration is present as a forward-compatible baseline across all apps that might need it.

### `umbraco-package.json` (manifest)

```json
{
    "$schema": "http://json.schemastore.org/umbraco-package.json",
    "id": "N3O.Umbraco.Data.ImportDataEditor",
    "name": "N3O Import Data Editor",
    "version": "17.0.0",
    "extensions": [
        {
            "type": "propertyEditorUi",
            "alias": "N3O.Umbraco.Data.ImportDataEditor",
            "name": "N3O Import Data Editor",
            "element": "/App_Plugins/N3O.Umbraco.Data.ImportDataEditor/import-data-editor.js",
            "meta": {
                "label": "N3O Import Data Editor",
                "icon": "icon-document",
                "group": "common",
                "propertyEditorSchemaAlias": "N3O.Umbraco.Data.ImportDataEditor"
            }
        }
    ]
}
```

This is the **registration file** Umbraco reads at startup to discover every extension. Field by field:

| Field | Meaning |
|-------|---------|
| `$schema` | Points to the JSON Schema for this file format. Enables IDE validation. |
| `id` | Unique identifier for this package. Used for deduplication and future package management. |
| `name` | Human-readable display name. |
| `version` | The plugin's version number. |
| `extensions[].type` | `"propertyEditorUi"` — tells Umbraco this is a custom property editor widget. Other types include `"dashboard"`, `"workspaceView"`, `"condition"`. |
| `extensions[].alias` | **The critical field.** This string must exactly match the `propertyEditorSchemaAlias` declared on the C# `[DataEditor]` attribute in the Data module backend. If it does not match, the data type created in the backoffice will not find its UI. See [09 — Umbraco Backoffice Extensions](../concepts/09-umbraco-backoffice-extensions.md). |
| `extensions[].element` | The absolute URL path (from the web root) to the compiled JS file. The browser fetches this when the extension is needed. |
| `meta.label` | Shown in the data-type picker UI in Umbraco. |
| `meta.icon` | Umbraco icon alias displayed in the data-type picker. |
| `meta.group` | Category grouping in the data-type picker. |
| `meta.propertyEditorSchemaAlias` | Declares which backend schema this UI implements. Umbraco uses this to pair the UI with the stored value schema. Must match the C# alias. |

Note: this file lives under `wwwroot/App_Plugins/` and is committed to git alongside the compiled JS. It is hand-authored — Vite does not generate it.

---

## 5. Concepts demonstrated

| Concept | Where in this app |
|---------|------------------|
| Bridge pattern (shell + React) | `import-data-editor.ts` — shell owns value + Umbraco contract; React owns rendering |
| Property editor UI contract | `value` get/set + `UmbPropertyEditorUiElement` interface + `UmbChangeEvent` dispatch |
| Authenticated API calls | `UmbAuthFetchMixin` → `this.authFetch` used in `#uploadResource` and `#getStorageToken` |
| Shadow DOM + `?inline` CSS | `attachShadow` in constructor; `<style>{styles}</style>` in JSX |
| Mixin chains | `UmbAuthFetchMixin(UmbElementMixin(HTMLElement))` |
| React controlled inputs | `value={field.value ?? ''}` + `onChange` callback |
| External modules (not bundled) | `react`, `@umbraco-cms/*`, `@n3o/backoffice-core` all external in `vite.config.ts` |
| Workspace dependency (`*`) | `@n3o/backoffice-core: "*"` in `package.json` resolved via npm workspace symlink |
| `uui-react.d.ts` shim | Forward-declares `uui-*`/`umb-*` tags for React JSX type-checking |
| Two-step file upload | `#getStorageToken` (temp upload) → `#uploadResource` (associate with import) |

---

## 6. Gotchas

**Alias must match the C# `[DataEditor]` alias exactly.**
The `extensions[].alias` in `umbraco-package.json` and `meta.propertyEditorSchemaAlias` both equal `"N3O.Umbraco.Data.ImportDataEditor"`. This string must be identical to the alias set in the C# `[DataEditor]` attribute. A mismatch causes the data type to exist in Umbraco but show no UI — a silent failure that can be hard to diagnose. See [09 — Umbraco Backoffice Extensions](../concepts/09-umbraco-backoffice-extensions.md).

**`this.authFetch` can be `null` on first render.**
`UmbAuthFetchMixin` resolves the auth context asynchronously. The `#render()` call in `connectedCallback` can fire before auth is ready. The code guards this with `if (!this.authFetch) { alert(...); return; }` — defensive but functional. A production improvement would disable the file input until `authFetch` is non-null.

**State lives in the shell, not React.**
The React component is fully controlled: it has no `useState` calls. All state is in `this.#value` on the shell. Every change callback mutates `#value` on the shell and calls `#render()`. If you add React state here, it will be out of sync with what Umbraco reads from the `value` getter — the shell's `#value` is the single source of truth.

**`void` in callback is intentional.**
`onFileSelected: (index, file) => void this.#uploadResource(index, file)` — the `void` discards the Promise returned by the async method. This is deliberate: the React `onChange` prop expects a synchronous callback returning `void`, not a `Promise`. The `void` operator forces the return type to `undefined` and suppresses the TypeScript warning about unhandled Promises in callbacks.

**The compiled `.js` file is committed to git.**
`wwwroot/App_Plugins/N3O.Umbraco.Data.ImportDataEditor/import-data-editor.js` is checked in alongside the TypeScript source. This is intentional — it makes the NuGet package self-contained and means downstream applications do not need Node/npm. Always rebuild (`npm run build` or `dotnet build`) after changing source, and commit both the source and the output together.

**`emptyOutDir: false` is critical.**
The shared `n3oPluginConfig` preset sets `emptyOutDir: false`. Without this, Vite would delete the entire `wwwroot/App_Plugins/` directory before each build, wiping every other plugin's compiled output. Each plugin writes only its own subdirectory.
