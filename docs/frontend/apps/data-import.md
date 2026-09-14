# data-import — CSV/ZIP import workspace view

> **Prerequisites:** read the concepts in order, especially
> [09 — Umbraco Backoffice Extensions](../concepts/09-umbraco-backoffice-extensions.md) and
> [10 — The N3O Bridge Pattern](../concepts/10-the-n3o-bridge-pattern.md) before this doc.
> All term definitions (workspaceView, import map, authFetch, Shadow DOM, …) live in those
> concept docs; this doc references them rather than repeating them.

---

## 1. What it is

The Data Import app adds an **Import** tab to every content node in the Umbraco backoffice that
the backend has opted into. From that tab a back-office user can:

1. Choose a **content type** (the document type of child items to create/update) and a **date pattern**.
2. Browse the available properties for that content type and select which ones to include.
3. Download a blank **CSV template** with one column per selected property.
4. Fill in the template, then upload it (and optionally a ZIP archive of linked media assets).
5. Confirm — the import is queued; processing happens in the background.

The tab is an Umbraco **workspaceView** extension: Umbraco renders it as an extra tab inside the
Content workspace sidebar, gated by two conditions:

- The workspace must be `Umb.Workspace.Document` (i.e. a content node, not a media or member).
- The shared `N3O.Condition.WorkspaceVisibility` condition must pass — this calls a backend
  endpoint (`/umbraco/backoffice/api/ImportVisibility`) that decides, per-node, whether the
  current user may see the tab at all.

**Where the files are served:** The `.StaticAssets` C# project packages everything under its
`wwwroot/` as Razor Class Library static web assets. At runtime ASP.NET Core serves them from
`/App_Plugins/N3O.Umbraco.Data.Import/`. The manifest and the compiled JavaScript are both under
that path.

See [01 — The Big Picture](../concepts/01-the-big-picture.md) for the general `*.StaticAssets`
project layout.

---

## 2. Files

### Source — `src/Data/N3O.Umbraco.Data.StaticAssets/Apps/N3O.Umbraco.Data.Import/`

| File | Role | C# analogy |
|------|------|------------|
| `package.json` | Package identity, build scripts, dev-dependencies | `.csproj` |
| `tsconfig.json` | TypeScript compiler options (extends shared base, adds JSX) | `<LangVersion>` / `<Nullable>` in `.csproj` |
| `vite.config.ts` | Bundler configuration — entry, output dir, externals | MSBuild property group + ItemGroup |
| `src/data-import.ts` | Web-component shell (`<n3o-data-import>`) — Umbraco context plumbing, React mount | `IComposer` / controller entry point |
| `src/data-import-app.tsx` | Root React component — state machine, all event handlers, `doImport` | A controller class with action methods |
| `src/import-form.tsx` | Presentational component — the 4-box form UI | A view model + partial view (pure display) |
| `src/import-success.tsx` | Presentational component — success confirmation + links | Success partial view |
| `src/import-error.tsx` | Presentational component — error message list | Error partial view |
| `src/types.ts` | Shared TypeScript interfaces (`ContentType`, `DatePattern`, `ImportableProperty`) | Shared DTO / model classes |
| `src/use-import-lookups.ts` | Custom React hook — fetches content types + date patterns on mount | A service / repository initialised in `OnGet()` |
| `src/data-import-app.css` | Component styles (injected into shadow root at runtime via `?inline`) | Embedded resource CSS / scoped CSS |
| `src/uui-react.d.ts` | TypeScript ambient declaration — teaches TypeScript about `uui-*` web components in JSX | XML doc / reference assembly shim |

### Manifest and compiled output — `src/Data/N3O.Umbraco.Data.StaticAssets/wwwroot/App_Plugins/N3O.Umbraco.Data.Import/`

| File | Role |
|------|------|
| `umbraco-package.json` | Umbraco extension manifest (read at startup by the backoffice) |
| `data-import.js` | Compiled output produced by Vite from `src/data-import.ts` (committed to git) |
| `data-import.js.map` | Source map for browser DevTools debugging |

---

## 3. End-to-end flow

This section traces the full path from a user opening a content node to the import being queued.
For the underlying concepts see [10 — The N3O Bridge Pattern](../concepts/10-the-n3o-bridge-pattern.md).

### Step 1 — Umbraco reads the manifest

When Umbraco 17 starts it scans every `App_Plugins/**/umbraco-package.json` file it can find
(served via the static-web-assets pipeline from the NuGet package or directly from `wwwroot/`).
The Data Import manifest registers one extension of type `workspaceView`:

```json
// wwwroot/App_Plugins/N3O.Umbraco.Data.Import/umbraco-package.json
{
    "$schema": "http://json.schemastore.org/umbraco-package.json",
    "id": "N3O.Umbraco.Data.Import",
    "name": "N3O Data Import",
    "version": "17.0.0",
    "extensions": [
        {
            "type": "workspaceView",
            "alias": "N3O.WorkspaceView.DataImport",
            "name": "N3O Data Import",
            "element": "/App_Plugins/N3O.Umbraco.Data.Import/data-import.js",
            "meta": {
                "label": "Import",
                "pathname": "import",
                "icon": "icon-page-up"
            },
            "conditions": [
                {
                    "alias": "Umb.Condition.WorkspaceAlias",
                    "match": "Umb.Workspace.Document"
                },
                {
                    "alias": "N3O.Condition.WorkspaceVisibility",
                    "endpoint": "/umbraco/backoffice/api/ImportVisibility"
                }
            ]
        }
    ]
}
```

The manifest fields that matter:

| Field | Meaning |
|-------|---------|
| `type: "workspaceView"` | Tells Umbraco this is a tab in the content workspace sidebar. |
| `alias` | Unique name for this extension — used by the conditions system and for debugging. |
| `element` | URL path to the compiled JS file. Umbraco loads this as an ES module via a `<script type="module">` tag. |
| `meta.label` | Tab label the user sees ("Import"). |
| `meta.pathname` | URL segment appended to the workspace URL when this tab is active (`/umbraco/section/content/.../import`). |
| `meta.icon` | Icon shown on the tab. |
| `conditions` | An array of conditions that must ALL pass before the tab is shown or the JS is loaded. |

The `Umb.Condition.WorkspaceAlias` condition is built into Umbraco: it passes only when the
current workspace is the Document workspace (`Umb.Workspace.Document`), ensuring the tab never
appears on media nodes, member nodes, etc.

The `N3O.Condition.WorkspaceVisibility` condition is a custom N3O extension (defined in
`BackofficeCore`). It calls the `endpoint` URL with an authenticated GET request. The backend
controller at `/umbraco/backoffice/api/ImportVisibility` returns whether the current user and
current node should see the Import tab. This is the v17 equivalent of the v13
`IContentAppFactory.GetContentAppsFor()` node/user gating. See
[09 — Umbraco Backoffice Extensions](../concepts/09-umbraco-backoffice-extensions.md) for how
conditions work.

### Step 2 — The backoffice loads the shell element

Once both conditions pass, Umbraco loads `data-import.js` as an ES module. The file registers the
custom element `<n3o-data-import>`. Umbraco then inserts that element into the DOM as the tab
content — identical to how it would render any built-in tab.

### Step 3 — The shell acquires context and mounts React

The `N3oDataImportElement` class (in `src/data-import.ts`) is the web-component shell. It mixes in
two Umbraco/N3O helpers:

- **`UmbElementMixin`** — Umbraco's base mixin that gives the element access to Umbraco's context
  system (a dependency-injection bus for the browser).
- **`UmbAuthFetchMixin`** (from `@n3o/backoffice-core`) — wraps `UMB_AUTH_CONTEXT` to produce
  `this.authFetch`, a pre-authenticated `fetch`-compatible function. See
  [10 — The N3O Bridge Pattern](../concepts/10-the-n3o-bridge-pattern.md) for how this works.

In the constructor the shell:

1. Creates a **Shadow DOM** (`attachShadow({ mode: 'open' })`), which encapsulates styles. See
   [06 — Web Components and Shadow DOM](../concepts/06-web-components-and-shadow-dom.md).
2. Creates a plain `<div>` inside the shadow root as the React mount target (`this.#mount`).
3. Calls `consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, ...)` to subscribe to the Document
   workspace context — this is where the current content node's `unique` (its GUID/key) is
   published. The shell observes `context.unique` and stores it as `this.#contentKey`.

When `connectedCallback` fires (the element is inserted into the DOM), the shell calls
`createRoot(this.#mount)` to initialise a React root, then calls `#render()`.

`#render()` calls:
```typescript
this.#root?.render(
    createElement(DataImportApp, {
        contentKey: this.#contentKey,
        authFetch: this.authFetch,
    }),
);
```

This is the **bridge**: from here on, everything is React. `contentKey` and `authFetch` are passed
as **props** (think method parameters, or constructor dependencies). React is not bundled into
`data-import.js` — it is resolved at runtime from the shared ReactRuntime via the import map. See
[04 — ES Modules and Import Maps](../concepts/04-es-modules-and-import-maps.md) and
[10 — The N3O Bridge Pattern](../concepts/10-the-n3o-bridge-pattern.md).

The shell re-renders when either piece of data changes:
- `authFetchChanged()` is called by the mixin whenever `authFetch` is updated — the shell
  immediately re-renders.
- The `observe(context.unique, ...)` subscription re-renders whenever the node key changes (e.g.
  the user navigates to a different content node without a full page reload).

`disconnectedCallback` unmounts the React root cleanly (`this.#root?.unmount()`), releasing all
React state and effect cleanup functions — analogous to `IDisposable.Dispose()`.

### Step 4 — React loads lookups (useImportLookups)

`DataImportApp` immediately calls the `useImportLookups` custom hook, passing `contentKey` and
`authFetch`. The hook fires a `useEffect` — a side-effect block that runs after the first render
and again any time `contentKey` or `authFetch` changes. Inside the effect it makes two
authenticated GET requests in sequence:

1. `GET /umbraco/backoffice/api/ContentTypes/{contentKey}/relations?type=child` — returns the
   content types that are valid children of the current node (these become the options in the
   "Content type" dropdown).
2. `GET /umbraco/backoffice/api/Imports/lookups/datePatterns` — returns the available date format
   patterns (e.g. DD/MM/YYYY, MM/DD/YYYY).

The results are stored in React state (`setContentTypes`, `setDatePatterns`). React re-renders
`DataImportApp` with the populated lists, which it passes down as props to `ImportForm` — the
dropdowns now have options.

### Step 5 — The user fills in the form (ImportForm, show=form)

The show-state router (see section 4 below) starts in `'form'` state, so `ImportForm` is rendered.
The user:
- Picks a content type → `onContentTypeChange` fires, which fetches the list of importable
  properties for that content type from
  `GET /umbraco/backoffice/api/Imports/importableProperties/{alias}`.
- Picks a date pattern.
- Optionally ticks "Move updated content".
- Selects which properties to include (or clicks "Select all").
- Clicks "Download template" → `getTemplate()` posts to
  `POST /umbraco/backoffice/api/Imports/template/{alias}` and triggers a browser file download.
- Attaches the completed CSV file (and optionally a ZIP) via native `<input type="file">`.

### Step 6 — The user clicks Import (doImport, AbortController)

`doImport()` runs. This is the most complex part — see section 4 for a line-level walk-through.
The flow:

1. Cancel any in-flight import (previous `AbortController`).
2. Create a new `AbortController` and store it in `importAbortRef.current`.
3. Upload the CSV via `POST /umbraco/api/Storage/tempUpload` → receive a storage token.
4. Upload the ZIP (if provided) the same way → receive a second storage token.
5. Post the queue request: `POST /umbraco/backoffice/api/Imports/queue/{contentKey}/{alias}` with
   the two storage tokens, date pattern, and move flag.
6. On HTTP 200 → `setShow('success')`.
7. On any other status → parse the error response, call `processingError(messages)` → `setShow('error')`.

### Step 7 — Show success or error (ImportSuccess / ImportError)

The show-state router (section 4) switches from `'form'` to `'success'` or `'error'`. The
respective presentational component renders. Both offer a "Start over" / "Import another file"
button that calls `startOver()`, which cancels any in-flight request, resets all state, and
returns `show` to `'form'`.

---

## 4. File-by-file

### `package.json`

```json
{
    "name": "n3o-umbraco-data-import",
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

- `name: "n3o-umbraco-data-import"` — internal workspace identity (never published).
- `type: "module"` — all `.js` files in this package are ES modules.
- `build` script — `tsc --noEmit` type-checks without emitting files (fast; catches type errors
  early), then `vite build` does the actual compilation. MSBuild calls `npm run build` in this
  directory via `Directory.Build.targets`. See [03 — Node, npm, and the Workspace](../concepts/03-node-npm-and-the-workspace.md).
- `watch` script — for local development: rebuilds on every file save without the type-check step
  (faster iteration).
- `devDependencies: { "@n3o/build": "*", "@n3o/backoffice-core": "*" }` — both are workspace
  packages resolved via npm symlinks (no network fetch). `"*"` means "find the local workspace
  member". Analogous to `<ProjectReference>` in a `.csproj`. See [03 — Node, npm, and the Workspace](../concepts/03-node-npm-and-the-workspace.md).

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

- `extends: "@n3o/build/tsconfig"` — inherits strict, modern compiler settings from the shared
  base (strict null checks, `target: ES2022`, etc.). Every plugin app extends this same base to
  stay consistent. See [05 — Vite and the Build](../concepts/05-vite-and-the-build.md).
- `jsx: "react-jsx"` — tells `tsc` to validate JSX syntax and understand that JSX transforms to
  `react/jsx-runtime` calls (the "automatic" JSX transform). Without this, `tsc` would reject
  `.tsx` files. Note: Vite's esbuild (not `tsc`) actually compiles JSX at bundle time; this
  setting is only needed so `tsc --noEmit` accepts the syntax. See [05 — Vite and the Build](../concepts/05-vite-and-the-build.md).
- `include: ["src"]` — type-checks only the `src/` subfolder, not `node_modules` or other
  stray files.

### `vite.config.ts`

```typescript
import { n3oPluginConfig } from '@n3o/build';

export default n3oPluginConfig({
    entries: {
        'N3O.Umbraco.Data.Import/data-import': 'src/data-import.ts',
    },
    outDir: '../../wwwroot/App_Plugins',
    react: true,
    additionalExternals: ['@n3o/backoffice-core'],
});
```

- `entries` — one entry: `src/data-import.ts`. The key
  `'N3O.Umbraco.Data.Import/data-import'` is a path segment **relative to `outDir`**, so the
  compiled file lands at
  `wwwroot/App_Plugins/N3O.Umbraco.Data.Import/data-import.js` — exactly the path referenced
  in `umbraco-package.json`.
- `outDir: '../../wwwroot/App_Plugins'` — two levels up from the `Apps/N3O.Umbraco.Data.Import/`
  folder lands at `wwwroot/App_Plugins/`. Vite writes there without wiping the rest of the
  directory (`emptyOutDir: false` in the shared preset).
- `react: true` — marks `react`, `react-dom`, `react-dom/client`, and `react/jsx-runtime` as
  **external**: they are not bundled. The output JS contains bare `import ... from 'react'`
  statements that the browser resolves at runtime via the import map. See [04 — ES Modules and Import Maps](../concepts/04-es-modules-and-import-maps.md) and [05 — Vite and the Build](../concepts/05-vite-and-the-build.md).
- `additionalExternals: ['@n3o/backoffice-core']` — likewise excludes `@n3o/backoffice-core`
  (the auth-fetch library) from the bundle. It is served separately and registered in the import
  map, so all plugins share a single copy.

### `src/data-import.ts` — web-component shell

This is the **bridge layer** between Umbraco and React. The file is the entry point that Vite
starts from; everything it imports is bundled (or marked external). At runtime Umbraco loads this
file, which registers the `<n3o-data-import>` custom element.

**Class declaration:**
```typescript
@customElement(elementName)
export class N3oDataImportElement extends UmbAuthFetchMixin(UmbElementMixin(HTMLElement)) {
```

`UmbElementMixin(HTMLElement)` creates a class that extends `HTMLElement` (the browser base for
all custom elements) and adds Umbraco's context-consumption methods (`consumeContext`, `observe`).
`UmbAuthFetchMixin(...)` wraps that further, adding `this.authFetch` and `authFetchChanged()`.
The `@customElement(elementName)` decorator registers `'n3o-data-import'` in the browser's custom
element registry — after this line, `document.createElement('n3o-data-import')` produces this
class. See [06 — Web Components and Shadow DOM](../concepts/06-web-components-and-shadow-dom.md)
and [10 — The N3O Bridge Pattern](../concepts/10-the-n3o-bridge-pattern.md).

**Private fields:**
```typescript
#root?: Root;           // React root — created once on connectedCallback
#mount: HTMLDivElement; // The <div> inside the shadow root where React renders
#contentKey: string | null = null; // The current content node's GUID
```

The `#` prefix is JavaScript's native private field syntax (not TypeScript's `private` keyword).
Fields declared with `#` are truly inaccessible from outside the class at the JavaScript engine
level — stronger than C#'s `private`, which is only enforced by the compiler.

**`consumeContext` and `observe`:**
```typescript
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
```

`consumeContext` is like `IServiceProvider.GetService<T>()` — it subscribes to a context object
published in the DOM tree above this element (by Umbraco's workspace). The callback fires when the
context is ready.

`context.unique` is a reactive observable (think `IObservable<string>` in Rx). `observe(...)` is
like `Subscribe(...)`. Every time the value changes the callback fires, stores the new content key,
and triggers a React re-render with the new `contentKey` prop.

**`authFetchChanged`:**
```typescript
authFetchChanged(_authFetch: AuthFetch | null): void {
    this.#render();
}
```

This override is the hook the `UmbAuthFetchMixin` calls whenever the authentication token changes
(user logs in/out, token refreshes). The shell immediately re-renders the React tree with the
updated `authFetch` prop.

**`connectedCallback` / `disconnectedCallback`:**
```typescript
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

`connectedCallback` fires when the element is inserted into the DOM. `??=` is the
nullish-assignment operator: "assign only if currently null/undefined". On first connection it
creates the React root; on reconnection (the element was detached and re-attached) it reuses the
existing root.

`disconnectedCallback` fires when the element is removed from the DOM (user navigates away, tab is
hidden). `unmount()` tears down all React component state, cancels any pending effects, and removes
the rendered DOM. This prevents memory leaks — important because the backoffice is a long-running
SPA.

**`#render`:**
```typescript
#render(): void {
    this.#root?.render(
        createElement(DataImportApp, {
            contentKey: this.#contentKey,
            authFetch: this.authFetch,
        }),
    );
}
```

`createElement(Component, props)` is the non-JSX way to create a React element — equivalent to
`<DataImportApp contentKey={...} authFetch={...} />` in JSX. The shell uses `createElement`
directly rather than JSX to keep the shell file as `.ts` (no JSX extension needed). See [08 — React](../concepts/08-react.md).

---

### `src/data-import-app.tsx` — root React component + state machine

This is where all the application logic lives. The function `DataImportApp` accepts two props and
returns JSX.

#### Props interface

```typescript
interface DataImportAppProps {
    contentKey: string | null;
    authFetch: AuthFetch | null;
}
```

`AuthFetch` is the type alias defined in `@n3o/backoffice-core`:
```typescript
export type AuthFetch = (input: string, init?: RequestInit) => Promise<Response>;
```

It is a function type with the same signature as the browser's built-in `fetch(input, init)` — so
every call site looks like a normal `fetch` call, but the bearer token is injected automatically.
Think of it as an `HttpClient` instance with an `AuthenticationHeaderValue` already configured.
See [10 — The N3O Bridge Pattern](../concepts/10-the-n3o-bridge-pattern.md).

#### State declarations

```typescript
const [show, setShow] = useState<string>('form');
const [processing, setProcessing] = useState<boolean>(false);
const [contentType, setContentType] = useState<ContentType | null>(null);
const [moveUpdatedContentToCurrentLocation, setMoveUpdatedContentToCurrentLocation] = useState<boolean>(false);
const [importableProperties, setImportableProperties] = useState<ImportableProperty[]>([]);
const [errorMessages, setErrorMessages] = useState<string[] | null>(null);
```

Each `useState<T>(initialValue)` call returns a `[currentValue, setter]` pair. The setter triggers
a re-render with the new value — analogous to a property with `INotifyPropertyChanged`. See [08 — React](../concepts/08-react.md).

`show` is the **show-state router**: a string that selects which top-level view to render.
Valid values: `'form'`, `'success'`, `'error'`. The render function uses inline ternary
expressions to select the component:

```tsx
{show === 'success' ? (
    <ImportSuccess onStartOver={startOver} />
) : show === 'error' ? (
    <ImportError errorMessages={errorMessages} onStartOver={startOver} />
) : (
    <ImportForm ... />
)}
```

This is equivalent to a `switch` statement on a route. The full form tree is unmounted when
`show !== 'form'`, discarding its DOM. When the user clicks "Start over", `setShow('form')`
mounts it again — but all state has been reset, so it starts fresh.

#### Refs for file inputs

```typescript
const csvFileRef = useRef<HTMLInputElement>(null);
const zipFileRef = useRef<HTMLInputElement>(null);
```

`useRef<T>(initialValue)` returns a stable object `{ current: T }` that persists across renders
without causing re-renders when mutated — analogous to a field on a class rather than a property.
Here the refs are attached to the two `<input type="file">` elements in `ImportForm` via the
`ref={csvFileRef}` prop. After render, `csvFileRef.current` is the actual DOM `HTMLInputElement`,
giving direct access to `input.files` when the user clicks Import.

Using refs for file inputs is necessary because `<input type="file">` is an **uncontrolled**
input: its value (the selected file) is managed by the browser, not by React state. React's state
model (controlled inputs) cannot be used with file inputs for security reasons.

C# analogy: a `ref` to a file input is like holding a reference to a `FileUpload` control in
ASP.NET WebForms — you reach into the control at submit time rather than having the value stored
in a ViewModel property.

#### AbortController ref

```typescript
const importAbortRef = useRef<AbortController | null>(null);
```

**What is `AbortController`?**
`AbortController` is a browser API for cancelling in-flight `fetch` requests. You create a
controller, pass its `.signal` to `fetch({ signal })`, and if you call `controller.abort()` the
fetch is cancelled immediately — the browser drops the network request, and the `fetch` Promise
rejects with a `DOMException` whose `name` is `'AbortError'`.

C# analogy: `AbortController` is exactly equivalent to `CancellationTokenSource` / `CancellationToken` in .NET. The
controller is the source; the signal is the token passed to async operations.

Why is it needed here? Uploading a large CSV is slow. If the user clicks Import, realises they
chose the wrong file, and clicks Import again with a corrected file, without cancellation the
first upload would still be in flight — two uploads race to the queue endpoint, potentially
producing duplicates. The pattern:

```typescript
const doImport = async (): Promise<void> => {
    cancelImport();                           // abort the previous upload if any
    const controller = new AbortController();
    importAbortRef.current = controller;
    const { signal } = controller;
    // ... pass signal to every authFetch call ...
};
```

`cancelImport()` calls `importAbortRef.current?.abort()`. The `?.` is optional chaining — "call
only if not null/undefined".

`importAbortRef` is a ref (not state) because mutating it should not trigger a re-render — it is
internal plumbing, not UI data.

The `finally` block:
```typescript
finally {
    if (importAbortRef.current === controller) {
        importAbortRef.current = null;
    }
}
```
The identity check (`=== controller`) guards against the case where a second import was started
before this one finished: if `importAbortRef.current` has already been replaced by a newer
controller, this finally block should not clear the new one.

#### `getStorageToken` — temp file upload

```typescript
const getStorageToken = async (input: HTMLInputElement, signal: AbortSignal): Promise<unknown> => {
    if (!input.files || input.files.length === 0) { return null; }
    const data = new FormData();
    data.append('file', input.files[0]);
    const res = await authFetch!('/umbraco/api/Storage/tempUpload', {
        method: 'POST',
        body: data,
        signal,
    });
    return await res.json();
};
```

`FormData` is the browser equivalent of a multipart/form-data POST body — analogous to
`MultipartFormDataContent` in `HttpClient`. The file is appended with the key `'file'`, matching
what the backend controller expects.

The `!` after `authFetch` is a TypeScript non-null assertion: "I know this is not null here". The
caller (`doImport`) only reaches this point after verifying the file input, and by that time
`authFetch` is guaranteed to be set (the shell passes it from `UmbAuthFetchMixin` which only fires
`#render` once auth is available).

The `signal` is the `AbortController.signal` passed in — if the user cancels mid-upload, the
`fetch` is aborted.

#### `doImport` — the full upload + queue flow

After uploading both files, `doImport` builds the queue request:

```typescript
const req = {
    datePattern: datePattern?.id,
    moveUpdatedContentToCurrentLocation,
    csvFile: csvStorageToken,
    zipFile: zipStorageToken,
};
const result = await authFetch!(
    `/umbraco/backoffice/api/Imports/queue/${contentKey}/${contentType!.alias}`,
    { method: 'POST', headers: { Accept: '*/*', 'Content-Type': 'application/json' },
      body: JSON.stringify(req), signal }
);
```

The abort checks after each async step:
```typescript
if (signal.aborted) return;
```
These early-exit guards prevent continuing with stale data after a cancellation even if the
`AbortError` exception is somehow swallowed internally.

The catch block:
```typescript
} catch (err) {
    if (err instanceof DOMException && err.name === 'AbortError') return;
    processingError('An unexpected error occurred while queueing the import.');
}
```
`AbortError` is silently discarded — it is an intentional cancellation, not a failure. All other
errors surface to the UI via `processingError`, which calls `setShow('error')`.

#### CSS injection

```tsx
return (
    <div className="n3o-data-import">
        {/* ... show-state router ... */}
        <style>{styles}</style>
    </div>
);
```

`styles` is the raw CSS string imported with `?inline` (see `data-import-app.css` below). Because
the React tree is mounted inside a Shadow DOM (created by the shell in `data-import.ts`), styles
in the document `<head>` do not reach here. Injecting a `<style>` element directly inside the
shadow root is the only way to apply styles. See [06 — Web Components and Shadow DOM](../concepts/06-web-components-and-shadow-dom.md) and [05 — Vite and the Build](../concepts/05-vite-and-the-build.md).

---

### `src/import-form.tsx` — presentational form component

`ImportForm` receives all data and event-handler callbacks as props and renders the 4-section form.
It contains no state, no async calls, and no logic beyond what is needed to display the data it
was given. This is the "pure view" / "dumb component" pattern — analogous to an ASP.NET Razor
partial that only renders a model without touching the database.

The props interface declares every piece of data and every callback separately:

```typescript
interface ImportFormProps {
    processing: boolean;
    contentTypes: ContentType[];
    contentType: ContentType | null;
    datePatterns: DatePattern[];
    datePattern: DatePattern | null;
    moveUpdatedContentToCurrentLocation: boolean;
    importableProperties: ImportableProperty[];
    selectedPropertyCount: number;
    csvFileRef: React.RefObject<HTMLInputElement | null>;
    zipFileRef: React.RefObject<HTMLInputElement | null>;
    onContentTypeChange: (event: React.ChangeEvent<HTMLSelectElement>) => void;
    onDatePatternChange: (event: React.ChangeEvent<HTMLSelectElement>) => void;
    onMoveUpdatedChange: (checked: boolean) => void;
    onPropertyToggle: (property: ImportableProperty, checked: boolean) => void;
    onSelectAllProperties: () => void;
    onClearSelectedProperties: () => void;
    onGetTemplate: () => void;
    onImport: () => void;
}
```

`React.RefObject<HTMLInputElement | null>` — a ref created by `useRef` in the parent, passed down
so `ImportForm` can attach it to a DOM element via `ref={csvFileRef}`. This is how the parent
reads the selected file without the file input being a controlled React input.

The JSX mixes Umbraco UI Library web components (`uui-box`, `umb-property-layout`, `uui-icon`,
`uui-loader-bar`) with native HTML elements (`<select>`, `<input>`, `<button>`). The N3O pattern
intentionally uses native HTML controls inside `uui-box`/`umb-property-layout` wrappers rather
than `<uui-select>` or `<uui-checkbox>` — this avoids the known v17 bug where `uui` form controls
with the `FormControlMixin` do not mount correctly when rendered by React. See
[10 — The N3O Bridge Pattern](../concepts/10-the-n3o-bridge-pattern.md).

The `processing` boolean disables all interactive controls while an upload is in flight, preventing
double-submission.

The `checkboxGrid` div renders `importableProperties` as a multi-column checkbox grid:
```tsx
<div className="checkboxGrid">
    {importableProperties.map((property) => (
        <label key={property.alias} className="checkOption">
            <input
                type="checkbox"
                checked={!!property.selected}
                onChange={(e) => onPropertyToggle(property, e.target.checked)}
                disabled={processing}
            />
            <span>{property.columnTitle}</span>
        </label>
    ))}
</div>
```

`key={property.alias}` is required by React when rendering a list: it tells React how to
identify each item across re-renders so it can update the DOM efficiently without re-creating
every element. The key must be stable and unique within the list — `alias` satisfies both.

---

### `src/import-success.tsx` — success confirmation

```typescript
export function ImportSuccess({ onStartOver }: ImportSuccessProps) {
    return (
        <uui-box headline="Import queued">
            <div className="statusBox statusBox--positive">
                <uui-icon name="icon-check"></uui-icon>
                <span>Your CSV file has been queued and will be processed shortly.</span>
            </div>
            <div className="actions">
                <a className="btn btn--primary" href="/umbraco/section/content/dashboard/imports">
                    View import queue
                </a>
                <button type="button" className="btn btn--secondary" onClick={onStartOver}>
                    Import another file
                </button>
            </div>
        </uui-box>
    );
}
```

The "View import queue" link navigates to the Imports dashboard (a separate Umbraco UI Builder
dashboard under the Content section). The path `/umbraco/section/content/dashboard/imports` is
the v17 Bellissima URL pattern: section pathname `content` + dashboard pathname `imports`. There
is no hash-based routing in v17.

---

### `src/import-error.tsx` — error list

Renders a red status box containing either a list of server-returned error strings or a generic
fallback message. `errorMessages` is `string[] | null` — `null` means no server detail is
available.

```tsx
{errorMessages && errorMessages.length > 0 ? (
    <ul className="errorList">
        {errorMessages.map((message) => (
            <li key={message}>{message}</li>
        ))}
    </ul>
) : (
    <span>Something went wrong while queueing the import.</span>
)}
```

---

### `src/types.ts` — shared DTO interfaces

```typescript
export interface ContentType {
    alias: string;
    name: string;
}
export interface DatePattern {
    id: string;
    name: string;
}
export interface ImportableProperty {
    alias: string;
    columnTitle: string;
    selected: boolean;
}
```

These are TypeScript `interface` declarations — analogous to C# record types or POCOs. They
describe the shape of the JSON the backend returns. They carry no behaviour; TypeScript uses them
only at compile time for type checking. At runtime these types do not exist — the JavaScript
objects are plain objects.

`selected` on `ImportableProperty` is client-side state added after the API response:

```typescript
// in refreshProperties (data-import-app.tsx)
for (const property of properties) {
    property.selected = false;
}
```

The backend doesn't return `selected`; it is added here so each property object carries its own
checkbox state. The interface includes `selected` to make this explicit to the compiler.

---

### `src/use-import-lookups.ts` — custom hook

A **custom hook** is a TypeScript function whose name begins with `use` and that calls other React
hooks. The naming convention tells React's linting tools to enforce the rules of hooks on it.
Conceptually it is a service class that encapsulates reactive state + async data fetching.

C# analogy: a custom hook is like a class that implements `INotifyPropertyChanged` and loads data
in its constructor — but expressed as a function returning a plain data bag.

```typescript
export function useImportLookups(contentKey: string | null, authFetch: AuthFetch | null): ImportLookups {
    const [contentTypes, setContentTypes] = useState<ContentType[]>([]);
    const [datePatterns, setDatePatterns] = useState<DatePattern[]>([]);
    const [datePattern, setDatePattern] = useState<DatePattern | null>(null);

    useEffect(() => {
        if (!contentKey || !authFetch) { return; }

        let active = true;

        const init = async (): Promise<void> => {
            const typesRes = await authFetch(`/umbraco/backoffice/api/ContentTypes/${contentKey}/relations?type=child`, ...);
            const types = (await typesRes.json()) as ContentType[];

            const patternsRes = await authFetch('/umbraco/backoffice/api/Imports/lookups/datePatterns', ...);
            const patterns = (await patternsRes.json()) as DatePattern[];

            if (active) {
                setContentTypes(types);
                setDatePatterns(patterns);
                setDatePattern(patterns[0] ?? null);
            }
        };

        void init();

        return () => { active = false; };
    }, [contentKey, authFetch]);

    return { contentTypes, datePatterns, datePattern, setDatePattern };
}
```

**`useEffect` — the side-effect hook:**
`useEffect(fn, [dep1, dep2])` runs `fn` after the component renders, and re-runs it whenever
`dep1` or `dep2` changes. The dependency array `[contentKey, authFetch]` means: re-run whenever
the content node changes or the auth function changes (user navigates to a different node, or auth
token refreshes). See [08 — React](../concepts/08-react.md).

**The `active` flag — stale closure guard:**
`useEffect` can run multiple times. If two effects are in flight simultaneously (e.g. `contentKey`
changes before the first fetch completes), only the latest effect should update state. The `active`
flag is set to `true` at the start of each effect and `false` in the cleanup function (the
`return () => { active = false; }` — analogous to `CancellationToken`). The final `if (active)`
check prevents a completed-but-stale fetch from clobbering state updated by a newer fetch.

This pattern does not cancel the underlying HTTP request (that would need `AbortController` as
used in `doImport`); it merely suppresses stale state updates.

**`void init()`:**
`init` is an `async` function. `useEffect`'s callback must return either `undefined` or a cleanup
function — it must not return a Promise. Writing `await init()` in the effect body would return a
Promise (a rejected Promise would be an unhandled rejection). `void init()` calls the async
function, discards the Promise, and returns `undefined` — satisfying React's requirement while
still allowing `init` to be written with `async`/`await`.

**`setDatePattern(patterns[0] ?? null)`:**
The `??` operator is the **nullish-coalescing operator** (identical to C# 8's `??`): "use the
left side if it is not `null`/`undefined`; otherwise use the right side." The first date pattern
in the list becomes the default selection; if the list is empty, `null` is used.

---

### `src/data-import-app.css` (loaded with `?inline`)

The CSS uses CSS custom properties (variables) from the Umbraco UI Library:

```css
.n3o-data-import {
    display: block;
    padding: var(--uui-size-space-4);
}
```

`var(--uui-size-space-4)` reads a CSS variable named `--uui-size-space-4` that is defined on
the root element by the UUI theme. Using these variables means the plugin's spacing, colours, and
border radii automatically match the rest of the backoffice and respect theme changes.

The file is imported in `data-import-app.tsx` as:
```typescript
import styles from './data-import-app.css?inline';
```

The `?inline` suffix is a Vite-specific import modifier: instead of injecting the CSS into the
page `<head>`, Vite exports the CSS as a plain string. The string is then injected as a `<style>`
element inside the React render output, which ends up inside the Shadow DOM. This is the only way
to style content inside a Shadow DOM. See [05 — Vite and the Build](../concepts/05-vite-and-the-build.md)
and [06 — Web Components and Shadow DOM](../concepts/06-web-components-and-shadow-dom.md).

Notable patterns in the CSS:

- **`.nativeSelect`** — a class applied to `<select>` elements to give them Umbraco-themed
  appearance (border, focus ring, disabled opacity) since native selects ignore most UUI styles.
- **`.btn`, `.btn--primary`, `.btn--secondary`, `.btn--positive`, `.btn--compact`** — a
  small button component system built from CSS classes, using UUI colour variables for
  consistent theming. `<uui-button>` is avoided because of the React/FormControlMixin bug
  noted in the Gotchas section below.
- **`.checkboxGrid`** — `grid-template-columns: repeat(auto-fill, minmax(180px, 1fr))` creates a
  responsive multi-column layout: as many columns as fit, each at least 180px wide.
- **`.statusBox--positive` / `.statusBox--danger`** — success/error banners using
  `var(--uui-color-positive)` and `var(--uui-color-danger)` from the UUI theme.

---

### `src/uui-react.d.ts` — TypeScript JSX shim for UUI web components

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

TypeScript's JSX type system knows about every built-in HTML element (`<div>`, `<input>`, etc.)
via the `IntrinsicElements` interface. Custom elements such as `<uui-box>` are not built-in;
without this declaration, TypeScript would reject the JSX with "Property 'uui-box' does not exist
on type 'JSX.IntrinsicElements'".

The `declare module 'react'` block **augments** (extends) the React type definitions from the
`react` npm package — analogous to C# partial classes or extension methods on an existing type.
Each entry is typed as `any`, which means TypeScript accepts any attribute on those elements
without checking. This is intentional: the UUI library ships its own Lit-based types which are
incompatible with React's JSX system; writing out full React-compatible prop types for every UUI
element would be excessive and brittle.

This is a `.d.ts` file (a TypeScript **declaration file**): it contributes types to the
TypeScript compiler only. It contains no runtime code and produces no JavaScript output — analogous
to a C# XML doc / reference assembly that provides metadata without implementation.

---

## 5. Concepts demonstrated

| Concept | Where | Doc |
|---------|-------|-----|
| Web-component shell wrapping React (bridge pattern) | `data-import.ts` | [10 — The N3O Bridge Pattern](../concepts/10-the-n3o-bridge-pattern.md) |
| Shadow DOM + React root inside it | `data-import.ts` constructor + `data-import-app.tsx` `<style>` | [06 — Web Components and Shadow DOM](../concepts/06-web-components-and-shadow-dom.md) |
| Umbraco context consumption (`UMB_DOCUMENT_WORKSPACE_CONTEXT`) | `data-import.ts` | [09 — Umbraco Backoffice Extensions](../concepts/09-umbraco-backoffice-extensions.md) |
| `UmbAuthFetchMixin` — authenticated fetch | `data-import.ts`, `data-import-app.tsx`, `use-import-lookups.ts` | [10 — The N3O Bridge Pattern](../concepts/10-the-n3o-bridge-pattern.md) |
| Show-state router (`show` state + ternary render) | `data-import-app.tsx` | [08 — React](../concepts/08-react.md) |
| `useRef` for DOM access (uncontrolled file inputs) | `data-import-app.tsx` | [08 — React](../concepts/08-react.md) |
| `AbortController` for cancellable uploads | `data-import-app.tsx` `doImport` + `importAbortRef` | [08 — React](../concepts/08-react.md) |
| Custom React hook extracting async data-fetching | `use-import-lookups.ts` | [08 — React](../concepts/08-react.md) |
| `useEffect` with cleanup (`active` flag) | `use-import-lookups.ts` | [08 — React](../concepts/08-react.md) |
| `FormData` multipart file upload | `data-import-app.tsx` `getStorageToken` | [08 — React](../concepts/08-react.md) |
| Presentational component decomposition | `import-form.tsx`, `import-success.tsx`, `import-error.tsx` | [08 — React](../concepts/08-react.md) |
| Shared DTO types | `types.ts` | [02 — JavaScript/TypeScript for C# Devs](../concepts/02-javascript-typescript-for-csharp-devs.md) |
| `?inline` CSS import into Shadow DOM | `data-import-app.css` + import in `data-import-app.tsx` | [05 — Vite and the Build](../concepts/05-vite-and-the-build.md) |
| `uui-react.d.ts` ambient module augmentation | `uui-react.d.ts` | [10 — The N3O Bridge Pattern](../concepts/10-the-n3o-bridge-pattern.md) |
| `umbraco-package.json` manifest + conditions | `wwwroot/App_Plugins/.../umbraco-package.json` | [09 — Umbraco Backoffice Extensions](../concepts/09-umbraco-backoffice-extensions.md) |
| Externals (React + backoffice-core not bundled) | `vite.config.ts` | [05 — Vite and the Build](../concepts/05-vite-and-the-build.md) |
| npm workspace symlinked dependencies | `package.json` | [03 — Node, npm, and the Workspace](../concepts/03-node-npm-and-the-workspace.md) |

---

## 6. Gotchas

### React and `uui` form controls (`uui-button`, `uui-checkbox`, etc.)

In v17, `uui` form controls (anything that uses `FormControlMixin` or `UUIFormControlMixin`) do
not mount correctly when rendered inside a React component. They do not appear in the DOM and
produce console errors. This is a known issue in the N3O v17 migration. The workaround — used
consistently throughout `ImportForm` — is to use native HTML elements (`<button>`, `<input
type="checkbox">`, `<select>`) styled with the UUI CSS variables, and to wrap them in
`uui-box` / `umb-property-layout` for the layout chrome. Never replace these native elements
with `<uui-button>` or `<uui-select>` without verifying the bug is resolved first.

### The `active` flag is not an `AbortController`

In `useImportLookups`, the `active` flag prevents stale state updates but does NOT cancel the
in-flight HTTP requests. If `contentKey` changes rapidly, two fetch pairs may be in flight
simultaneously. The bandwidth cost is small (these are JSON endpoints, not file uploads), so
`AbortController` was not added to the lookups hook. The `doImport` flow, which uploads potentially
large files, does use `AbortController` — the distinction is intentional.

### `void init()` — not an oversight

The `void` keyword before an async function call in a `useEffect` body is intentional and
correct (see the section on `useImportLookups` above). Do not change it to `await init()` —
that would turn the effect callback into an `async` function that returns a Promise, which React
does not accept.

### CSS custom properties are inherited through Shadow DOM

`var(--uui-color-text)` and similar UUI variables work inside the Shadow DOM because CSS custom
properties **are** inherited through shadow boundaries (unlike regular CSS properties). The UUI
theme sets these on `:root`, so they cascade into any shadow root in the page. This is why the
component can use UUI design tokens without any special setup.

### The compiled `data-import.js` is committed to git

The file at `wwwroot/App_Plugins/N3O.Umbraco.Data.Import/data-import.js` is the Vite-compiled
output and is committed to the repository. This is intentional: it makes the package
self-contained for consumers of the NuGet package who do not have the npm toolchain. Do not add
`wwwroot/App_Plugins/` to `.gitignore`. After editing any source file under `Apps/`, run
`dotnet build` (or `npm run build` inside `Apps/N3O.Umbraco.Data.Import/`) to regenerate the
compiled file and commit both the source and the output together.

### Two-step upload (temp storage then queue)

The import does NOT POST the file directly to the queue endpoint. It first uploads to
`/umbraco/api/Storage/tempUpload` (a general-purpose Umbraco temp-storage endpoint) which returns
an opaque token. That token is then included in the queue POST body. This two-step pattern
decouples file transfer from job creation: the queue endpoint only validates and enqueues; it
never handles raw binary uploads. The backend's queue controller expects the storage token shape,
not `IFormFile`.

### `N3O.Condition.WorkspaceVisibility` must be registered

The manifest's second condition (`N3O.Condition.WorkspaceVisibility`) is a custom condition
extension defined in `BackofficeCore`. If the `BackofficeCore` package is not loaded (its own
`umbraco-package.json` must be present and loaded before the Data Import package), the condition
is unknown to Umbraco and the Import tab will never appear. Ensure
`N3O.Umbraco.BackofficeCore.StaticAssets` is referenced (directly or transitively) by the
consuming site's `.csproj`.
