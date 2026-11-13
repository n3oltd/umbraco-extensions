# App: Cloud Platforms Preview (`N3O.Umbraco.Cloud.Platforms.StaticAssets`)

> Concept reminders used in this doc — read these first if you are new to any term:
> - [The Big Picture](../concepts/01-the-big-picture.md)
> - [Node, npm, and the Workspace](../concepts/03-node-npm-and-the-workspace.md)
> - [Vite and the Build](../concepts/05-vite-and-the-build.md)
> - Web Components and Shadow DOM — `../concepts/06-web-components-and-shadow-dom.md`
> - Lit — `../concepts/07-lit.md`
> - React — `../concepts/08-react.md`
> - Umbraco Backoffice Extensions — `../concepts/09-umbraco-backoffice-extensions.md`
> - The N3O Bridge Pattern — `../concepts/10-the-n3o-bridge-pattern.md`

> **If you have not read `blocks.md` yet, read it first.** This doc assumes familiarity with the bridge pattern and focuses on what is different here: the polling/setTimeout approach, passing `authFetch` directly into React as a prop, the Lit-only `workspaceInfoApp`, and the two-extension manifest.

---

## 1. What it is

This app registers **two separate backoffice extensions** for the Cloud Platforms feature:

| Extension | Type | What it does |
|---|---|---|
| Platforms Preview | `workspaceView` | A new **tab** in the document editor workspace (labelled "Preview", icon `icon-eye`) that shows a live WYSIWYG preview of a Cloud Platform configuration document rendered inside an iframe. The preview polls the server every 10 seconds and skips unchanged responses using an eTag. |
| Platforms URLs | `workspaceInfoApp` | A panel in the document editor **Info tab** (right-hand sidebar) that displays the staging and production platform URLs for a document, with copy buttons. |

Both extensions are scoped to `Umb.Workspace.Document` (document workspaces only). The `workspaceView` additionally gates on a custom `N3O.Condition.WorkspaceVisibility` condition that calls a server endpoint to decide whether the current document should show the Preview tab.

**Where built output is served from:** `src/Cloud/N3O.Umbraco.Cloud.Platforms.StaticAssets/wwwroot/App_Plugins/N3O.Umbraco.Cloud.Platforms.Preview/`. The `.StaticAssets` C# project ships this as static web assets, served by ASP.NET Core at `/App_Plugins/N3O.Umbraco.Cloud.Platforms.Preview/`.

**Key architectural contrast with `blocks.md`:** This app shows two different implementation strategies side by side:

- `platforms-preview.ts` + `platforms-preview-app.tsx` — the **bridge pattern** (Lit shell + React UI), with the added twist that `authFetch` is passed directly into React as a prop rather than being used exclusively inside the shell.
- `platforms-urls-info-app.ts` — a **pure-Lit component** with no React at all, managing its own auth token manually via `UMB_AUTH_CONTEXT`.

---

## 2. Files

### Source (`Apps/` directory)

| File | Purpose |
|---|---|
| `package.json` | npm package manifest. Nearly identical to `blocks.md`'s package; notably it does **not** list `@n3o/backoffice-core` as a dev-dependency, meaning it references it only as an external (resolved by the browser at runtime). |
| `tsconfig.json` | TypeScript configuration. Identical to `blocks.md` — extends `@n3o/build/tsconfig` with `"jsx": "react-jsx"`. |
| `vite.config.ts` | Build configuration. Two entry points this time: `platforms-preview.ts` and `platforms-urls-info-app.ts`, both written to the same `App_Plugins` output directory. |
| `src/platforms-preview.ts` | **Bridge pattern shell** (custom element `n3o-platforms-preview`). Consumes `UMB_DOCUMENT_WORKSPACE_CONTEXT`, passes `unique`, `getContent`, and `authFetch` into React, and extends `LitElement` (not bare `HTMLElement`) to use Lit's `render()` lifecycle for mounting the React div. |
| `src/platforms-preview-app.tsx` | **React UI** for the preview tab. Unlike `blocks.md`, this component owns the network calls itself (using `authFetch` passed as a prop). It sets up a 10-second `setInterval` poll and builds the preview iframe imperatively using `useRef` and direct DOM manipulation. |
| `src/platforms-urls-info-app.ts` | **Pure-Lit info app** — no React. Extends `UmbLitElement`, uses Lit's `@state` decorator for reactive re-render, and manually reads the OAuth token from `UMB_AUTH_CONTEXT` to call the URLs endpoint. |
| `src/uui-react.d.ts` | TypeScript declaration shim for UUI web components in JSX. More complete than the `blocks.md` version — declares `uui-box`, `uui-label`, `uui-icon`, `uui-loader`, and `uui-load-indicator`. Includes a note that interactive controls (buttons, inputs) must not be used inside React due to a v17 bug. |

### Manifest (shipped under `wwwroot/`)

| File | Purpose |
|---|---|
| `wwwroot/App_Plugins/N3O.Umbraco.Cloud.Platforms.Preview/umbraco-package.json` | Registers both extensions (the `workspaceView` with two conditions, and the `workspaceInfoApp`). |

---

## 3. End-to-end flow

### Flow A: Platforms Preview tab (`workspaceView`)

```
Umbraco backoffice opens a document
  └─ evaluates conditions on all registered workspaceView extensions
       ├─ Umb.Condition.WorkspaceAlias = "Umb.Workspace.Document"  ✓
       └─ N3O.Condition.WorkspaceVisibility → GET /umbraco/backoffice/api/PlatformsPreview/visibility
            (server returns true/false; tab only appears if true)

User clicks the "Preview" tab
  └─ Umbraco instantiates <n3o-platforms-preview> (platforms-preview.js)
       │
       ├─ constructor(): creates mount div, consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT)
       │    └─ observe(context.unique, ...) → #unique updated → #render()
       │
       ├─ connectedCallback(): LitElement lifecycle; Lit calls render() which returns html`${this.#mount}`
       │
       ├─ updated(): once mount is in DOM, createRoot(mount), then #render()
       │    └─ root.render(<PlatformsPreviewApp unique={...} getContent={...} authFetch={...} />)
       │
       └─ authFetchChanged(): called when OAuth resolves → #render() again with non-null authFetch
            └─ PlatformsPreviewApp useEffect fires
                 ├─ authFetch GET /umbraco/backoffice/api/cloudBackOffice/subscription/code
                 ├─ authFetch POST /umbraco/backoffice/api/platformsBackOffice/previewHtml/{documentTypeUnique}
                 │    └─ compare eTag; if changed: build iframe, doc.write(html), load platforms.js CDN script
                 └─ window.setInterval(loadPreview, 10000)  ← repeats every 10s
```

### Flow B: Platforms URLs info panel (`workspaceInfoApp`)

```
Umbraco backoffice opens a document
  └─ evaluates conditions on workspaceInfoApp extensions
       └─ Umb.Condition.WorkspaceAlias = "Umb.Workspace.Document"  ✓

Info panel slot becomes visible
  └─ Umbraco instantiates <n3o-platforms-urls-info-app> (platforms-urls-info-app.js)
       │
       ├─ constructor():
       │    ├─ consumeContext(UMB_AUTH_CONTEXT, ...) → stores authConfig
       │    └─ consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, ...) → observe(unique, ...)
       │         └─ if both unique and authConfig are ready: #loadUrls(unique)
       │
       └─ #loadUrls(): manually extract Bearer token from authConfig, fetch contentUrls endpoint
            └─ if permitted: update @state _stagingUrl / _productionUrl → Lit re-renders
                 └─ render(): html`<umb-workspace-info-app-layout>...</umb-workspace-info-app-layout>`
                      └─ if both URLs null: return html`` (renders nothing, hides itself)
```

---

## 4. File-by-file walkthrough

### `package.json`

```json
{
    "name": "n3o-umbraco-cloud-platforms-preview",
    "version": "1.0.0",
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

Compared to `blocks.md`'s `package.json`, **`@n3o/backoffice-core` is absent from `devDependencies`**. It is still referenced in `vite.config.ts` as an `additionalExternal`, so the browser resolves it at runtime from the shared `N3O.Umbraco.BackofficeCore` JS file. The build tooling resolves the `@n3o/backoffice-core` import for type-checking via the npm workspace `node_modules` (the workspace root installs all workspace packages), but this package does not need to declare it explicitly because it only uses things that reach it through `@umbraco-cms/backoffice/*` or are brought in by the Vite externals configuration.

Everything else (`"private": true`, `"type": "module"`, the `build`/`watch` scripts) is identical to `blocks.md` — see that doc for the explanation.

### `tsconfig.json`

Identical to `blocks.md`. See that doc for explanation.

### `vite.config.ts`

```typescript
import { n3oPluginConfig } from '@n3o/build';

export default n3oPluginConfig({
    entries: {
        'platforms-preview': 'src/platforms-preview.ts',
        'platforms-urls-info-app': 'src/platforms-urls-info-app.ts',
    },
    outDir: '../wwwroot/App_Plugins/N3O.Umbraco.Cloud.Platforms.Preview',
    react: true,
    additionalExternals: ['@n3o/backoffice-core'],
});
```

The difference from `blocks.md` is the `entries` object: **two entry points** instead of one. Vite builds each entry independently and produces:
- `platforms-preview.js`
- `platforms-urls-info-app.js`

Both are written to the same `outDir`. This is the correct approach when you have multiple custom elements that belong to the same backoffice feature — they share a build config but produce separate JS modules (one per custom element tag), because Umbraco loads each extension's `element` file on demand.

`react: true` and `additionalExternals: ['@n3o/backoffice-core']` are the same as `blocks.md`.

Note: even though `platforms-urls-info-app.ts` does not use React at all, `react: true` still marks React as external in both entry outputs. This is harmless — a module that never imports React simply does not emit any React import statements in its output.

---

### `src/platforms-preview.ts` — the Lit shell (bridge pattern variant)

```typescript
import { LitElement, css, customElement, html, nothing } from '@umbraco-cms/backoffice/external/lit';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import { UMB_DOCUMENT_WORKSPACE_CONTEXT } from '@umbraco-cms/backoffice/document';
import type { UmbDocumentDetailModel, UmbDocumentWorkspaceContext } from '@umbraco-cms/backoffice/document';
import { UmbAuthFetchMixin } from '@n3o/backoffice-core';
import type { AuthFetch } from '@n3o/backoffice-core';
import { createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { PlatformsPreviewApp } from './platforms-preview-app';
```

Differences from `blocks.md`'s imports:
- `LitElement, css, html, nothing` are imported from `@umbraco-cms/backoffice/external/lit`. This shell extends `LitElement` (not bare `HTMLElement`). That means Lit's full reactive rendering system is available, including `render()`, `updated()`, and `static styles`.
- `UmbElementMixin` is imported from `@umbraco-cms/backoffice/element-api` rather than from `@n3o/backoffice-core`. Both re-export the same function; this is an equivalent path.

#### Class declaration (line 20)

```typescript
@customElement(elementName)
export class N3oPlatformsPreviewElement extends UmbAuthFetchMixin(UmbElementMixin(LitElement)) {
```

The base chain is `LitElement` instead of `HTMLElement`. `LitElement` adds:
- A `render()` method you override to return an `html` tagged template literal (Lit's way of describing HTML, similar to Razor's `@Html` helpers but evaluated in the browser).
- An `updated()` lifecycle hook called after each reactive update.
- `static styles = css\`...\`` for scoped CSS without needing `?inline` and `<style>` injection.

Using `LitElement` here (rather than bare `HTMLElement` as in `blocks.md`) is a design choice. The shell needs only minimal Lit features — just enough to get a mount point into the shadow DOM and then hand off to React. The choice makes the shadow DOM and lifecycle management slightly more declarative.

#### Constructor (lines 27–39)

```typescript
constructor() {
    super();
    this.#mount = document.createElement('div');

    this.consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, (context) => {
        this.#workspaceContext = context;
        this.observe(context?.unique, (unique) => {
            this.#unique = unique;
            this.#render();
        });
    });
}
```

Compared to `blocks.md`, this constructor is simpler: it only observes `context.unique` (the document GUID). The entire block manager interaction that `blocks.md` needed is absent — here the shell just passes a `getContent` getter into React and lets React make the API calls.

#### Lit `render()` override (lines 55–61)

```typescript
override render() {
    if (this.#mount.parentNode == null) {
        return html`${this.#mount}`;
    }
    return nothing;
}
```

This is the **Lit render method**, not React's render. It returns a Lit `html` tagged template literal that describes what to put in the shadow DOM.

- `html\`\`` — Lit's template literal tag. It creates an efficient template that Lit updates in-place when properties change. The `${this.#mount}` interpolation inserts the raw DOM node (the `<div>`) into the shadow root. Lit is smart enough to accept real DOM nodes in its templates.
- `nothing` — a Lit sentinel value meaning "render nothing at all." Once `this.#mount` has a `parentNode`, it is already in the shadow DOM; returning `nothing` prevents Lit from trying to re-insert it.
- `override` — TypeScript keyword confirming that this method intentionally overrides a base class method. It causes a compile error if the base class has no such method, which protects against typos.

This pattern (insert a div via Lit, then hand it to React) is necessary because `LitElement` manages the shadow root itself — you cannot call `attachShadow` manually as in `blocks.md`. Lit creates the shadow root on first render.

#### `updated()` (lines 63–69)

```typescript
override updated(): void {
    if (!this.#root && this.#mount.isConnected) {
        this.#root = createRoot(this.#mount);
        this.#render();
    }
}
```

`updated()` is called by Lit after every render completes. The first time it fires, `this.#mount` is in the DOM (`isConnected` is true) but `this.#root` does not yet exist. This is the earliest safe moment to call `createRoot`. Compare with `blocks.md` where `createRoot` is called in `connectedCallback` — here `connectedCallback` fires before Lit's first render, so the mount div is not yet connected, making `updated()` the right hook.

#### `#getContent` getter (lines 76–78)

```typescript
#getContent = (): UmbDocumentDetailModel | undefined => {
    return this.#workspaceContext?.getData();
};
```

This is a **private arrow function stored as a field** (not a regular method). The `= () =>` syntax creates a function whose `this` is permanently bound to the enclosing class instance, regardless of how it is called. This is important because this function is passed as a prop to React — React may call it from inside a `useEffect` or event handler where `this` would otherwise be lost.

In C#, the equivalent would be creating a `Func<UmbDocumentDetailModel?>` delegate that captures `this`: `Func<UmbDocumentDetailModel?> getContent = () => this._workspaceContext?.GetData();`.

#### `#render()` (lines 80–87)

```typescript
#render(): void {
    this.#root?.render(
        createElement(PlatformsPreviewApp, {
            unique: this.#unique,
            getContent: this.#getContent,
            authFetch: this.authFetch,
        }),
    );
}
```

**Key difference from `blocks.md`:** `authFetch` is passed as a **prop** to the React component. In `blocks.md`, the shell made the fetch itself; React only received the resulting HTML string. Here, the shell delegates the fetch entirely to React — the React component makes both API calls. The shell merely provides the authenticated fetch function as a prop.

This means `PlatformsPreviewApp` must handle the case where `authFetch` is `null` (not yet resolved). The shell re-renders React every time `authFetchChanged` fires (line 72), so React eventually gets a non-null `authFetch` and its `useEffect` re-runs.

#### `static styles` (lines 90–96)

```typescript
static override styles = css`
    :host {
        display: block;
        height: 100%;
        padding: var(--uui-size-layout-1);
    }
`;
```

Lit's class-level styles mechanism. The `css` tagged template literal creates a `CSSResult` that Lit injects into the shadow root as an `adoptedStyleSheets` entry (or a `<style>` tag in older browsers). `var(--uui-size-layout-1)` is a CSS custom property (CSS variable) defined by Umbraco's UI design system. No `?inline` import is needed — this is the idiomatic Lit way to scope CSS.

---

### `src/platforms-preview-app.tsx` — the React UI with polling

This is the most complex React component in the codebase. Read it as a component that owns its own data fetching and manages imperative DOM (an iframe) through a ref.

#### Imports and types (lines 1–22)

```typescript
import { useEffect, useRef } from 'react';
import type { UmbDocumentDetailModel, UmbDocumentVariantModel, UmbDocumentValueModel } from '@umbraco-cms/backoffice/document';
import type { AuthFetch } from '@n3o/backoffice-core';

export interface PreviewHtmlResponse {
    eTag: string;
    html: string;
}

interface PlatformsPreviewAppProps {
    unique: string | null | undefined;
    getContent: () => UmbDocumentDetailModel | undefined;
    authFetch: AuthFetch | null;
}
```

- `useEffect`, `useRef` — two of React's built-in **hooks** (functions that let function components tap into React's lifecycle). See the explanation below.
- `PreviewHtmlResponse` is exported (unlike the props interface in `blocks.md`) because its `eTag` field is used for the change-detection logic.
- `PlatformsPreviewAppProps.getContent: () => UmbDocumentDetailModel | undefined` — a function prop. The shell passes `#getContent` (the arrow field above) so the React component can read live document data on each poll without being re-rendered by React's normal update cycle.
- `PlatformsPreviewAppProps.authFetch: AuthFetch | null` — the authenticated fetch function, passed from the shell. This is the "prop threading" pattern: the shell gets the auth context via Umbraco's DI, then threads it down to React via props.

#### `getApiReq` helper (lines 24–37)

```typescript
function getApiReq(
    values: Array<UmbDocumentValueModel>,
    documentTypeUnique: string | undefined,
): Record<string, unknown> {
    const req: Record<string, unknown> = {};
    values.forEach((property) => {
        req[property.alias] = property.value;
    });
    req['contentTypeAlias'] = documentTypeUnique;
    return req;
}
```

A plain utility function (not a component or hook). It converts the array of `UmbDocumentValueModel` objects (each with `alias` and `value`) into a flat object that the server-side controller expects.

- `Record<string, unknown>` — TypeScript's generic dictionary type. Equivalent to `Dictionary<string, object?>` in C#.
- `.forEach((property) => { req[property.alias] = property.value; })` — iterating and building an object. The arrow function `(property) => { ... }` is an inline anonymous function — equivalent to a C# lambda `property => { req[property.alias] = property.value; }`.

#### `PlatformsPreviewApp` component (lines 44–157)

```typescript
export function PlatformsPreviewApp({ unique, getContent, authFetch }: PlatformsPreviewAppProps) {
    const containerRef = useRef<HTMLDivElement | null>(null);
    const previousETagRef = useRef<string | null>(null);

    useEffect(() => {
        // ...
    }, [unique, getContent, authFetch]);

    return <div ref={containerRef} id="platformsPreviewContainer" style={{ display: 'none' }} />;
}
```

The component's body runs on **every render**, but the `useEffect` callback runs only when its **dependency array** changes.

#### `useRef` (lines 45–46)

```typescript
const containerRef = useRef<HTMLDivElement | null>(null);
const previousETagRef = useRef<string | null>(null);
```

`useRef` returns a mutable box (`{ current: T }`) whose value persists across renders and whose mutations do **not** trigger a re-render. There are two separate uses here:

- `containerRef` — holds a reference to the actual DOM element (`<div id="platformsPreviewContainer">`). Set up by the `ref={containerRef}` attribute on the JSX return. Equivalent to declaring a `private HtmlElement _container;` field in Blazor code-behind — you need the actual DOM node to append the iframe to it imperatively.
- `previousETagRef` — holds the last-seen eTag between poll invocations. Stored in a ref (not `useState`) because changing it should not re-render the component. Equivalent to a private field on a class — mutable state that does not drive the UI.

#### `useEffect` (lines 48–154)

```typescript
useEffect(() => {
    let active = true;

    const loadPreview = async (): Promise<void> => { ... };

    void loadPreview();
    const intervalId = window.setInterval(() => { void loadPreview(); }, 10000);

    return () => {
        active = false;
        window.clearInterval(intervalId);
    };
}, [unique, getContent, authFetch]);
```

`useEffect` is the React hook for side effects that happen outside the render cycle — network requests, timers, manual DOM manipulation. Think of it as a combination of `OnAfterRenderAsync` (runs after render) and `IDisposable.Dispose` (cleanup via the returned function).

**How it works:**

1. The callback runs after the first render and again whenever any value in the dependency array (`[unique, getContent, authFetch]`) changes.
2. Before the next run (or on component unmount), React calls the returned **cleanup function** (`() => { active = false; clearInterval(intervalId); }`). This is like `IDisposable.Dispose()` — it cancels the timer and sets `active = false` so any in-flight `loadPreview` call knows not to modify the DOM.
3. `void loadPreview()` runs the first poll immediately. `setInterval(..., 10000)` then repeats it every 10 seconds.

The `active` boolean is a closure variable (captured by both `loadPreview` and the cleanup function). It guards against the scenario where the effect re-runs (cleanup fires, `active = false`) before an async `loadPreview` in progress has finished. This is the JavaScript equivalent of a `CancellationToken`.

#### Inside `loadPreview` (lines 51–142)

```typescript
const subscriptionCodeRes = await authFetch('/umbraco/backoffice/api/cloudBackOffice/subscription/code');
const subscriptionCode = (await subscriptionCodeRes.json()) as string;

const apiRes = await authFetch(`/umbraco/backoffice/api/platformsBackOffice/previewHtml/${documentTypeUnique}`, {
    method: 'POST',
    headers: { accept: 'application/json', 'Content-Type': 'application/json' },
    body: JSON.stringify(apiReq),
});
const res = (await apiRes.json()) as PreviewHtmlResponse;

if (res.eTag === previousETagRef.current) { return; }
previousETagRef.current = res.eTag;
```

Two sequential API calls:
1. GET the subscription code (a short string like `"abc123"`) — used to build the CDN URL for `platforms.js`.
2. POST the current document's property values to `previewHtml/{documentTypeUnique}` — returns `{ eTag, html }`.

The **eTag comparison** (`if (res.eTag === previousETagRef.current) return`) is a cache-busting optimisation: if the server-rendered HTML has not changed since the last poll (same eTag), skip the expensive DOM rebuild. Equivalent to `If-None-Match` HTTP header caching, but implemented in JS.

#### Imperative iframe construction (lines 109–141)

```typescript
container.innerHTML = '';
const iframe = document.createElement('iframe');
iframe.style.width = '100%';
// ...
iframe.style.display = 'none';
container.appendChild(iframe);

const doc = iframe.contentWindow!.document;
doc.open();
doc.write(res.html);
doc.close();

const script = doc.createElement('script');
script.src = `https://cdn.n3o.cloud/connect-${subscriptionCode}/platforms-js/platforms.js`;
script.type = 'module';
doc.body.appendChild(script);

window.setTimeout(() => {
    if (!active) { return; }
    iframe.style.display = 'block';
    container.style.display = 'block';
}, 2000);
```

This section bypasses React's virtual DOM and manipulates the DOM directly. This is acceptable inside a `useEffect` when the DOM node is managed by `containerRef` and the manipulation is intentionally imperative (building an iframe with `doc.write` is inherently not a declarative operation).

- `container.innerHTML = ''` — clears the previous preview before rebuilding. Simple and direct.
- `iframe.contentWindow!.document` — accesses the iframe's document object. The `!` is TypeScript's **non-null assertion operator** — it tells the compiler "I know this is not null." The comment in the source (`// iframe.contentWindow is non-null immediately after appending a same-origin iframe`) explains why this is safe.
- `doc.open(); doc.write(res.html); doc.close()` — the old-style way to write HTML into a same-origin iframe. Equivalent to writing `Response.Write(html)` before the streaming response era. Preserved from the original Lit/AngularJS implementation for fidelity.
- `doc.createElement('script')` / `doc.body.appendChild(script)` — dynamically loads the tenant's `platforms.js` CDN module into the iframe. The script is appended after `doc.write` because the iframe's DOM must exist first.
- `window.setTimeout(() => { iframe.style.display = 'block'; }, 2000)` — a 2-second delay before showing the iframe. Gives `platforms.js` time to initialise and render inside the iframe before the user sees it. The `active` guard inside the timeout prevents showing a stale iframe if the effect has already been torn down.

#### Return value (line 156)

```typescript
return <div ref={containerRef} id="platformsPreviewContainer" style={{ display: 'none' }} />;
```

The component renders a single empty `<div>`. All the interesting content is injected imperatively into this div by `useEffect`. The div starts hidden (`display: 'none'`); the `setTimeout` in `loadPreview` reveals it after the iframe loads.

- `ref={containerRef}` — this JSX attribute tells React to store the real DOM node in `containerRef.current` after the first render. From that point on, `containerRef.current` is the live `<div>` element.

---

### `src/platforms-urls-info-app.ts` — the pure-Lit info panel

This file is a deliberate contrast: **no React at all**. It uses Lit's built-in reactive system end-to-end.

```typescript
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css, customElement, state } from '@umbraco-cms/backoffice/external/lit';
import { UMB_DOCUMENT_WORKSPACE_CONTEXT } from '@umbraco-cms/backoffice/document';
import { UMB_AUTH_CONTEXT, type UmbOpenApiConfiguration } from '@umbraco-cms/backoffice/auth';
```

- `UmbLitElement` — Umbraco's own base class (extends `LitElement` and adds `consumeContext` / `observe` via `UmbElementMixin`). Used instead of composing the mixin manually.
- `UMB_AUTH_CONTEXT` — the auth context token. Used here directly (not via `UmbAuthFetchMixin`) because this component builds its own Bearer-token header rather than using the shared `AuthFetch` wrapper.

#### `@state` decorator (lines 20–21)

```typescript
@state() private _stagingUrl: string | null = null;
@state() private _productionUrl: string | null = null;
```

`@state()` is Lit's equivalent of React's `useState` — it declares a reactive property. When its value changes, Lit automatically re-runs `render()`. The `private` and `_` prefix are conventions for internal state (not public attributes).

In React you would write:
```tsx
const [stagingUrl, setStagingUrl] = useState<string | null>(null);
const [productionUrl, setProductionUrl] = useState<string | null>(null);
```

In Lit, the assignment `this._stagingUrl = data.stagingUrl` triggers the re-render automatically (Lit's property change detection), whereas in React you must call `setStagingUrl(data.stagingUrl)` explicitly.

#### Manual auth token handling (lines 48–61)

```typescript
async #loadUrls(unique: string): Promise<void> {
    if (!this.#authConfig) { return; }

    const rawToken = this.#authConfig.token;
    const token = typeof rawToken === 'function' ? await rawToken() : rawToken;

    const headers: Record<string, string> = { Accept: 'application/json' };
    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }

    const res = await fetch(`/umbraco/backoffice/api/PlatformsBackOffice/contentUrls/${unique}`, { headers });
    const data = (await res.json()) as ContentUrlsRes;
    // ...
}
```

This is the **manual auth pattern** — the alternative to `UmbAuthFetchMixin`. It uses `UMB_AUTH_CONTEXT` directly:
1. `this.#authConfig = authContext.getOpenApiConfiguration()` (in the constructor) stores the config.
2. `config.token` may be a string or a function that returns a string asynchronously. The ternary `typeof rawToken === 'function' ? await rawToken() : rawToken` handles both cases.
3. The token is then attached manually as `Authorization: Bearer <token>`.

Why not use `UmbAuthFetchMixin` here? This component predates the mixin, and since it is a `workspaceInfoApp` with minimal requirements, the manual pattern was retained. The mixin is the preferred approach for new code — it is cleaner and handles token refresh automatically.

#### Self-hiding behaviour (lines 78–80)

```typescript
override render() {
    if (!this._stagingUrl && !this._productionUrl) {
        return html``;
    }
    // ...
}
```

If neither URL is set (e.g., the backend returned `permitted: false`), the component returns an empty template. Lit renders nothing, so the element is invisible. **No custom visibility condition is needed** — the component hides itself from within. This is simpler than registering a server-side condition, and works for a `workspaceInfoApp` (which has no routing or tab visibility concerns). Compare with the `workspaceView` which does need a condition because the tab label itself must be hidden.

#### Lit `html` tagged templates (lines 82–97)

```typescript
return html`
    <umb-workspace-info-app-layout headline="Platform URLs">
        ${this._stagingUrl ? this.#renderRow('Staging', this._stagingUrl) : ''}
        ${this._productionUrl ? this.#renderRow('Production', this._productionUrl) : ''}
    </umb-workspace-info-app-layout>
`;
```

- `html\`...\`` — Lit's template literal tag. Everything inside is processed as HTML, with `${...}` interpolations evaluated as JavaScript. Lit tracks which parts of the template are dynamic and updates only those DOM nodes on re-render — similar to how Blazor's `@` syntax marks dynamic content.
- `<umb-workspace-info-app-layout headline="Platform URLs">` — a custom element from Umbraco's UI library that provides the standard "Info tab panel" chrome (header, separator, etc.).
- `${this._stagingUrl ? this.#renderRow(...) : ''}` — conditional rendering. If `_stagingUrl` is non-null, call `#renderRow` which returns another `html` template; otherwise render nothing (empty string).

#### Event handling (line 95 and `#copy`)

```typescript
<button class="copy" @click=${() => this.#copy(url)} title="Copy">Copy</button>
```

`@click=${...}` is Lit's event binding syntax. The `@` prefix means "add an event listener for this event." The value is any JavaScript expression that evaluates to a function. Equivalent to `@onclick="() => Copy(url)"` in Blazor's Razor syntax.

```typescript
#copy(url: string): void {
    void navigator.clipboard.writeText(url);
}
```

`navigator.clipboard.writeText` is the modern browser API for writing to the clipboard. It returns a `Promise<void>`; `void` discards it (errors are silently ignored here — no toast feedback is shown on failure).

#### `static override styles` (lines 101–143)

Lit's CSS-in-class pattern — `css\`...\`` creates scoped styles injected into the shadow DOM. Uses Umbraco's CSS custom properties (`--uui-size-space-3`, `--uui-color-text-alt`, `--uui-color-interactive`, etc.) so the component adapts to theme changes. See `../concepts/06-web-components-and-shadow-dom.md` for why shadow DOM scoping matters here.

---

### `src/uui-react.d.ts`

```typescript
// Interactive uui controls (uui-button, uui-input, etc.) are intentionally omitted:
// they break when rendered by React in v17 — use native HTML controls inside uui-box/umb-property-layout instead.
import 'react';

declare module 'react' {
    namespace JSX {
        interface IntrinsicElements {
            'uui-box': any;
            'uui-label': any;
            'uui-icon': any;
            'uui-loader': any;
            'uui-load-indicator': any;
        }
    }
}
```

More complete than `blocks.md`'s version — adds `uui-box`, `uui-label`, `uui-icon`, and `uui-load-indicator` for use inside `.tsx` files. The comment explicitly calls out that interactive UUI controls (`uui-button`, `uui-input`, etc.) are **not** listed here and must not be used inside React components in Umbraco 17 — they fail to mount and produce console errors. See the `uui + React FormControlMixin bug` memory note for details. Use native HTML elements (`<button>`, `<input>`) inside `<uui-box>` wrappers as the workaround.

---

### `wwwroot/App_Plugins/N3O.Umbraco.Cloud.Platforms.Preview/umbraco-package.json`

```json
{
    "$schema": "http://json.schemastore.org/umbraco-package.json",
    "id": "N3O.Umbraco.Cloud.Platforms.Preview",
    "name": "N3O Platforms Preview",
    "version": "17.0.0",
    "extensions": [
        {
            "type": "workspaceView",
            "alias": "N3O.WorkspaceView.PlatformsPreview",
            "name": "N3O Platforms Preview",
            "element": "/App_Plugins/N3O.Umbraco.Cloud.Platforms.Preview/platforms-preview.js",
            "meta": {
                "label": "Preview",
                "pathname": "platforms-preview",
                "icon": "icon-eye"
            },
            "conditions": [
                {
                    "alias": "Umb.Condition.WorkspaceAlias",
                    "match": "Umb.Workspace.Document"
                },
                {
                    "alias": "N3O.Condition.WorkspaceVisibility",
                    "endpoint": "/umbraco/backoffice/api/PlatformsPreview/visibility"
                }
            ]
        },
        {
            "type": "workspaceInfoApp",
            "alias": "N3O.WorkspaceInfoApp.PlatformsUrls",
            "name": "N3O Platforms URLs",
            "element": "/App_Plugins/N3O.Umbraco.Cloud.Platforms.Preview/platforms-urls-info-app.js",
            "weight": 50,
            "meta": {},
            "conditions": [
                {
                    "alias": "Umb.Condition.WorkspaceAlias",
                    "match": "Umb.Workspace.Document"
                }
            ]
        }
    ]
}
```

Two extensions in one manifest, each explained:

#### `workspaceView` extension

- `"type": "workspaceView"` — adds a **tab** to the document workspace. Unlike `blockEditorCustomView` (which is scoped to an individual property editor), a `workspaceView` is visible at the workspace level (the whole document editor).
- `"meta": { "label": "Preview", "pathname": "platforms-preview", "icon": "icon-eye" }` — defines the tab label ("Preview"), its URL path segment, and the icon shown in the tab bar.
- **Two conditions** must both be satisfied for the tab to appear:
  - `Umb.Condition.WorkspaceAlias` — a built-in Umbraco condition. The `"match": "Umb.Workspace.Document"` value restricts this view to document workspaces only (not media, members, etc.).
  - `N3O.Condition.WorkspaceVisibility` — a custom condition implemented in `N3O.Umbraco.BackofficeCore` (see `src/N3O.Umbraco.Cms/Apps/N3O.Umbraco.BackofficeCore/src/workspace-visibility-condition.ts`). It calls the given `endpoint` URL; the server responds with `true` or `false`. If false, the tab is hidden. This replaces the v13 `IContentAppFactory` per-node gating mechanism. The `endpoint` for this extension is `/umbraco/backoffice/api/PlatformsPreview/visibility`.

#### `workspaceInfoApp` extension

- `"type": "workspaceInfoApp"` — adds a **panel to the Info tab** (the right-hand sidebar of the document editor). Unlike a `workspaceView`, this does not create a new top-level tab — it contributes content to the built-in "Info" section.
- `"weight": 50` — controls ordering relative to other `workspaceInfoApp` extensions. Lower weight = higher up.
- **One condition** — only `Umb.Condition.WorkspaceAlias`. There is no `N3O.Condition.WorkspaceVisibility` here because the component hides itself (returns `html\`\``) when the server says `permitted: false`. Two approaches to the same problem; the self-hiding approach is simpler when the component's own data fetch already knows whether to show.

---

## 5. Concepts demonstrated

| Concept | Where demonstrated |
|---|---|
| [The Big Picture](../concepts/01-the-big-picture.md) | Two extensions registered from one manifest; conditional tab visibility |
| [Node, npm, and the Workspace](../concepts/03-node-npm-and-the-workspace.md) | `package.json` with `@n3o/build` only; externals resolved via workspace |
| [Vite and the Build](../concepts/05-vite-and-the-build.md) | Two entry points in one `vite.config.ts`; `react: true` marking React external |
| ES Modules and Import Maps — `../concepts/04-es-modules-and-import-maps.md` | React resolved at runtime by `N3O.Umbraco.ReactRuntime` import map |
| Web Components and Shadow DOM — `../concepts/06-web-components-and-shadow-dom.md` | `LitElement` shadow root; `static styles = css\`\`` scoped CSS; `UmbLitElement` |
| Lit — `../concepts/07-lit.md` | `html\`\`` templates; `@state`; `@click`; `render()`/`updated()` lifecycle; pure-Lit info app |
| React — `../concepts/08-react.md` | `useEffect`; `useRef`; `setInterval`/`clearInterval` pattern; `active` closure guard; imperative DOM via ref |
| Umbraco Backoffice Extensions — `../concepts/09-umbraco-backoffice-extensions.md` | `workspaceView` + `workspaceInfoApp` types; `Umb.Condition.WorkspaceAlias`; `N3O.Condition.WorkspaceVisibility`; `weight` ordering |
| The N3O Bridge Pattern — `../concepts/10-the-n3o-bridge-pattern.md` | `authFetch` passed as React prop; `getContent` getter as prop; `LitElement`-based shell variant |

---

## 6. Comparing the two approaches in this app

| Aspect | Bridge pattern (`platforms-preview.ts` + `.tsx`) | Pure Lit (`platforms-urls-info-app.ts`) |
|---|---|---|
| Base class | `UmbAuthFetchMixin(UmbElementMixin(LitElement))` | `UmbLitElement` (extends `LitElement` via Umbraco) |
| React involved | Yes — `createRoot`, JSX, hooks | No — all Lit |
| Auth | `authFetch` from mixin, passed as prop to React | `UMB_AUTH_CONTEXT` consumed directly; token extracted manually |
| Reactivity / state | React `useRef` + `useEffect` for side effects | Lit `@state` decorators; property assignment triggers re-render |
| UI templates | JSX (`.tsx`) | `html` tagged template literals (`.ts`) |
| Imperative DOM | Yes — iframe built directly in `useEffect` | Minimal — only a clipboard write |
| When to use | Complex UI with React ecosystem (hooks, third-party React libs) | Simple, server-data-driven UI with no React dependency |
| Self-hiding | N/A (tab hidden by `N3O.Condition.WorkspaceVisibility` condition) | `render()` returns `html\`\`` if URLs are null |

The general rule: use the bridge pattern when you want React for the UI. Use pure Lit when the UI is simple and driven directly by Umbraco context data.

---

## 7. Gotchas

**1. `authFetch` is null until OAuth resolves**

`PlatformsPreviewApp` guards `if (!authFetch) { return; }` at the top of `loadPreview`. The shell calls `this.#render()` in `authFetchChanged`, which re-renders the React component with the new (non-null) `authFetch`. React's `useEffect` re-runs because `authFetch` is in the dependency array. Do not add side effects that depend on `authFetch` outside `useEffect` — they will run before auth is ready.

**2. `useEffect` cleanup prevents stale iframe injection**

The `active` flag is essential. Without it, a slow network response could arrive after the effect has been torn down (e.g., the user navigated away), calling `container.innerHTML = ''` and creating a new iframe on a node that is no longer in the DOM. Always mirror this pattern when combining `useEffect` with async operations.

**3. `getContent` reads in-memory state, not saved state**

`this.#workspaceContext?.getData()` returns the current unsaved draft, not the published version. The preview therefore always reflects what the editor has typed, including changes that have not been saved. This is intentional — it mirrors the v13 behaviour.

**4. eTag change-detection is server-driven**

The client does not compute the eTag — the server does. If the server changes its eTag generation logic (e.g., changes the hashing algorithm), the client will rebuild the iframe on every poll even if the HTML is identical. The current optimisation assumes the server produces a stable eTag for unchanged data.

**5. `doc.write` and same-origin iframes**

`doc.write` only works for same-origin iframes in modern browsers. If the iframe `src` were set to an external URL, `contentWindow.document` would throw a cross-origin security error. Here no `src` is set, so the iframe is same-origin by default.

**6. Interactive UUI controls break inside React in Umbraco 17**

As noted in `uui-react.d.ts`, do not use `uui-button`, `uui-input`, or other form controls from Umbraco UI inside React components targeting Umbraco 17. They use `FormControlMixin` in a way that conflicts with React's DOM ownership. Use plain `<button>` and `<input>` elements instead, styled to match if needed.

**7. The `workspaceInfoApp` uses `weight: 50` — ordering is not guaranteed without it**

Without a `weight`, Umbraco orders info app panels by registration order (which depends on package load order). Setting `weight: 50` gives explicit control. Lower numbers appear higher. If a future panel needs to appear above this one, give it `weight < 50`; below: `weight > 50`.
