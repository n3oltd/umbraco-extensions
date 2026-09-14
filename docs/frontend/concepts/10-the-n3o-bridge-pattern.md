# 10 — The N3O Bridge Pattern

**Prerequisites:** [06 — Web Components and Shadow DOM](./06-web-components-and-shadow-dom.md), [08 — React](./08-react.md), [09 — Umbraco Backoffice Extensions](./09-umbraco-backoffice-extensions.md).

---

## What this document explains

Every N3O backoffice plugin follows the same architectural pattern. Once you understand it, every app doc will click into place immediately. This document explains the pattern step by step, using the Block Preview plugin as the canonical example, then describes each component of the system that makes it work.

---

## Why mix web components and React?

Umbraco 17's backoffice SPA only knows how to host **custom elements** (web components). It cannot host React components directly. A plugin must give Umbraco a custom element class registered with the browser's `customElements.define` API.

However, React is a far more productive UI toolkit than raw web-component APIs for building stateful, interactive UIs — it has hooks, a component model, a rich ecosystem, and familiar patterns. N3O's solution is a **bridge**: a minimal web-component shell that handles the Umbraco contract (lifecycle, contexts, data in/out), and a React app that handles the actual rendering.

The two sides speak different "languages":
- Umbraco talks to the shell via property setters, context injection, and lifecycle callbacks.
- The shell talks to React via props (data down) and callbacks (changes up).
- React never knows it is inside a web component or a backoffice.

**C# analogy:** Think of the shell as a thin `IHttpHandler` / `IController` entry point whose only job is to parse the request and delegate to a service layer. The React app is the service layer — it knows nothing about HTTP, only about its input data and how to produce output.

---

## The canonical example: Block Preview

### Files

| File | Role |
|------|------|
| `src/Blocks/N3O.Umbraco.Blocks.StaticAssets/Apps/src/block-preview.ts` | The web-component **shell** |
| `src/Blocks/N3O.Umbraco.Blocks.StaticAssets/Apps/src/block-preview-app.tsx` | The React **app** |
| `src/Blocks/N3O.Umbraco.Blocks.StaticAssets/Apps/src/block-preview-app.css` | Styles, imported `?inline` |
| `src/Blocks/N3O.Umbraco.Blocks.StaticAssets/Apps/src/uui-react.d.ts` | JSX type declarations for `uui-*` elements |
| `src/Blocks/N3O.Umbraco.Blocks.StaticAssets/Apps/vite.config.ts` | Vite build configuration |
| `src/Blocks/N3O.Umbraco.Blocks.StaticAssets/wwwroot/App_Plugins/N3O.Umbraco.Blocks.Preview/umbraco-package.json` | Umbraco manifest |

---

## The pattern — step by step

### Step 1 — Umbraco instantiates a web-component shell

The manifest (`umbraco-package.json`) registers the extension with a JS file path:

```json
{
    "type": "blockEditorCustomView",
    "alias": "N3O.BlockCustomView.Preview",
    "element": "/App_Plugins/N3O.Umbraco.Blocks.Preview/block-preview.js",
    "forBlockEditor": "block-grid"
}
```

When the block grid editor renders a block, Umbraco loads `block-preview.js` and looks up the custom element name registered by it. The browser creates an instance of the element class and inserts it into the DOM. From this point on, the shell is a live DOM node.

The shell class is declared in `block-preview.ts`:

```ts
// src/Blocks/N3O.Umbraco.Blocks.StaticAssets/Apps/src/block-preview.ts (line 33–34)

@customElement(elementName)
export class N3oBlockPreviewElement extends UmbAuthFetchMixin(UmbElementMixin(HTMLElement))
        implements UmbBlockEditorCustomViewElement {
```

- `@customElement('n3o-block-preview')` — registers `<n3o-block-preview>` in the browser's custom element registry.
- `UmbElementMixin(HTMLElement)` — adds Umbraco's `consumeContext` and `observe` methods.
- `UmbAuthFetchMixin(...)` — wraps the mixin chain with `authFetch` support (see Step 4).
- `implements UmbBlockEditorCustomViewElement` — the TypeScript interface contract Umbraco expects: `content` and `settings` getter/setter pairs.

The shell creates a **shadow root** in its constructor to isolate its styles and markup:

```ts
// src/Blocks/.../block-preview.ts (lines 71–77)

constructor() {
    super();
    const shadow = this.attachShadow({ mode: 'open' });
    this.#mount = document.createElement('div');
    shadow.appendChild(this.#mount);
    // ... context subscriptions below
}
```

A plain `<div>` is appended to the shadow root. This `#mount` div is where React will render. React never knows it is inside a shadow root.

### Step 2 — the shell consumes Umbraco contexts

Inside the constructor, the shell subscribes to three Umbraco contexts using `consumeContext`:

```ts
// src/Blocks/.../block-preview.ts (lines 78–113)

this.consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, (context) => {
    this.observe(ctx.unique, (unique) => { this.#nodeKey = unique; }, '_observeUnique');
    this.observe(ctx.splitView.activeVariantsInfo,
        (infos) => { this.#culture = infos?.[0]?.culture ?? ''; }, '_observeCulture');
});

this.consumeContext(UMB_BLOCK_ENTRY_CONTEXT, (context) => {
    this.observe(context.contentKey, (key) => { this.#contentKey = key; }, '_observeContentKey');
    this.observe(context.contentElementTypeKey,
        (key) => { this.#documentTypeKey = key; }, '_observeContentElementTypeKey');
});

this.consumeContext(UMB_BLOCK_MANAGER_CONTEXT, (context) => {
    this.#blockManager = context;
});
```

Each context provides a different piece of Umbraco's runtime state. The shell subscribes and stores the values as private fields (`#nodeKey`, `#culture`, `#contentKey`, `#documentTypeKey`, `#blockManager`). None of this is passed to React yet — it is accumulated in the shell.

Additionally, the block editor sets `content` and `settings` properties directly on the element (JavaScript property assignment). The setters trigger a debounced reload:

```ts
// src/Blocks/.../block-preview.ts (lines 40–56)

set content(value: UmbBlockEditorCustomViewElement['content']) {
    this.#content = value;
    this.#onDataChanged();
}

set settings(value: UmbBlockEditorCustomViewElement['settings']) {
    this.#settings = value;
    this.#onDataChanged();
}
```

This is the Umbraco side of the data flow. The shell acts as a **translation layer** between Umbraco's context/property system and React's props.

### Step 3 — the shell mounts React and passes data as props

`connectedCallback` fires when the browser inserts the element into the DOM. The shell creates a React root on the mount div and renders the React app:

```ts
// src/Blocks/.../block-preview.ts (lines 121–128)

connectedCallback(): void {
    super.connectedCallback();
    this.#root ??= createRoot(this.#mount);
    this.#render();
    this.#scheduleReload(0);
}
```

The `#render` method calls React's `root.render` with props assembled from the fields the shell has accumulated:

```ts
// src/Blocks/.../block-preview.ts (lines 219–226)

#render(): void {
    this.#root?.render(
        createElement(BlockPreviewApp, {
            loaded: this.#loaded,
            markup: this.#markup,
        }),
    );
}
```

`createElement(Component, props)` is the non-JSX form of `<Component prop={value} />`. It is used here because the shell is a `.ts` file, not a `.tsx` file — Vite only enables JSX transforms for `.tsx` files.

When data changes (a new context value arrives, `content`/`settings` is set, or an API response arrives), the shell calls `#render()` again. React efficiently updates only what changed.

**Data flows in one direction:**
- **Down:** Umbraco → shell fields → React props → React component tree.
- **Up:** React `onChange` callback → shell dispatches `UmbPropertyValueChangeEvent` → Umbraco reads `element.value`.

`disconnectedCallback` tears down React:

```ts
// src/Blocks/.../block-preview.ts (lines 133–141)

disconnectedCallback(): void {
    super.disconnectedCallback();
    clearTimeout(this.#reloadHandle);
    this.#root?.unmount();
    this.#root = undefined;
}
```

`unmount()` is React's cleanup call — it frees all React state, runs `useEffect` cleanups, and removes event listeners. Equivalent to `Dispose()`.

### Step 4 — authenticated server calls via `UmbAuthFetchMixin`

The preview endpoint is protected by `[Authorize]`. Plain `fetch()` returns `401`. The shell uses `UmbAuthFetchMixin` from `@n3o/backoffice-core` to get an authenticated fetch function:

```ts
// src/Blocks/.../block-preview.ts (line 34)
extends UmbAuthFetchMixin(UmbElementMixin(HTMLElement))
```

`UmbAuthFetchMixin` (source: `src/N3O.Umbraco.Cms/Apps/N3O.Umbraco.BackofficeCore/src/auth-fetch.ts`) subscribes to `UMB_AUTH_CONTEXT` and populates `this.authFetch` — a `fetch`-compatible function that automatically injects the current bearer token.

The shell overrides `authFetchChanged` to trigger a reload when the token becomes available:

```ts
// src/Blocks/.../block-preview.ts (lines 117–119)

authFetchChanged(_authFetch: AuthFetch | null): void {
    this.#scheduleReload(0);
}
```

The actual API call:

```ts
// src/Blocks/.../block-preview.ts (lines 200–206)

const response = await this.authFetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(blockData),
});
```

`this.authFetch` looks and behaves exactly like the browser's built-in `fetch`, but adds `Authorization: Bearer <token>` on every call.

When the API returns HTML markup, the shell stores it and calls `#render()` to pass it to React as a prop:

```ts
// src/Blocks/.../block-preview.ts (lines 212–216)

this.#markup = markup;
this.#loaded = true;
this.#render();
```

### Step 5 — the React app renders

`block-preview-app.tsx` is a pure React component. It receives `loaded` and `markup` as props and renders accordingly:

```tsx
// src/Blocks/.../block-preview-app.tsx

import styles from './block-preview-app.css?inline';

export function BlockPreviewApp({ loaded, markup }: BlockPreviewAppProps) {
    return (
        <>
            {!loaded ? (
                <div className="preview-alert preview-alert-info">
                    <uui-loader style={{ color: '#fff' }}></uui-loader>
                    Loading preview...
                </div>
            ) : (
                <div className="block-preview-frame" dangerouslySetInnerHTML={{ __html: markup }} />
            )}

            <style>{styles}</style>
        </>
    );
}
```

Points of note:

- **No Umbraco imports** — the React component has zero knowledge of Umbraco. It only knows about `loaded` and `markup`.
- **`<uui-loader>`** — a web component rendered from JSX. It is safe here because it is not an interactive form control (see the gotcha in [09](./09-umbraco-backoffice-extensions.md)). It requires a declaration in `uui-react.d.ts`.
- **`<style>{styles}</style>`** — the `?inline` CSS import (see Step 7 below).
- **`dangerouslySetInnerHTML`** — the markup is server-rendered HTML from C#, the same trust level as the AngularJS version wrote to the page. It is safe here for the same reason it was safe before.

---

## Full flow diagram

```
Umbraco backoffice (web component runtime)
       │
       │  1. reads umbraco-package.json
       │     → loads block-preview.js
       │     → inserts <n3o-block-preview> into DOM
       │
       ▼
┌─────────────────────────────────────────────────────────────────┐
│  N3oBlockPreviewElement  (web-component SHELL)                  │
│                                                                 │
│  Umbraco side                         React side               │
│  ─────────────────────────────        ───────────────────────  │
│  consumeContext(AUTH_CTX)             #root = createRoot(div)  │
│    → this.authFetch          ──────►  root.render(             │
│                                         <BlockPreviewApp        │
│  consumeContext(WORKSPACE_CTX)            loaded={...}         │
│    → this.#nodeKey                        markup={...}         │
│    → this.#culture                      />                     │
│                                       )                        │
│  consumeContext(BLOCK_ENTRY_CTX)                               │
│    → this.#contentKey                                          │
│    → this.#documentTypeKey                                     │
│                                                                 │
│  consumeContext(BLOCK_MANAGER_CTX)                             │
│    → this.#blockManager                                        │
│                                                                 │
│  element.content = {...}  (Umbraco sets)                       │
│  element.settings = {...} (Umbraco sets)                       │
│                                                                 │
│  this.authFetch(url, { method: 'POST', body: blockData })      │
│    ──────────────────────────────────────────────────────────► │
│                                               C# backend       │
│                          ◄─────────────────── 200 { markup }  │
│  this.#markup = markup                                         │
│  this.#render()  ──── re-renders React ──────────────────────► │
└─────────────────────────────────────────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────────────────┐
│  BlockPreviewApp  (React component)             │
│                                                 │
│  Props in:  loaded, markup                      │
│                                                 │
│  Renders:  <uui-loader> (loading state)         │
│         or <div dangerouslySetInnerHTML> (done) │
│            <style>{inlinedCss}</style>          │
│                                                 │
│  (No onChange — this is a read-only preview)   │
└─────────────────────────────────────────────────┘
       │
       ▼
  Shadow DOM mount div  ←  React renders here (inside shadow root,
                           isolated from backoffice CSS)
```

---

## The property editor variant — N3oCellsElement

The Cells editor (`block-preview.ts` is a read-only preview; `n3o-cells.ts` is an editable property editor) shows the **two-way data flow**:

```ts
// src/Plugins/Cells/N3O.Umbraco.Cells.StaticAssets/Apps/src/n3o-cells.ts (lines 66–77)

#render(): void {
    this.#root?.render(
        createElement(N3oCellsApp, {
            value: this.#value,
            gridConfiguration: this.#gridConfiguration,
            onChange: (value: unknown[][]) => {
                this.#value = value;
                this.dispatchEvent(new UmbPropertyValueChangeEvent());
            },
        }),
    );
}
```

The `onChange` callback is a **closure** (a function that captures `this` from the enclosing shell). When the React app calls `onChange(newValue)`:

1. The shell stores the new value in `this.#value`.
2. The shell dispatches `UmbPropertyValueChangeEvent`.
3. Umbraco hears the event and reads `element.value` (the getter) to get the new value and persist it.

React never calls Umbraco directly. It calls a callback provided by the shell, and the shell handles the Umbraco side. The React app's only knowledge is its props interface:

```ts
interface N3oCellsAppProps {
    value: CellsValue;
    gridConfiguration: Record<string, unknown>;
    onChange: (value: unknown[][]) => void;
}
```

---

## The shared React instance — why it matters

Every plugin imports React:

```ts
import { createElement } from 'react';
import { createRoot } from 'react-dom/client';
```

If each plugin bundled its own copy of React, the browser would have multiple React instances. React uses global module-level state for its reconciler and context system. Two separate React copies = two separate context trees, two separate hook registries. Features like `useContext` would fail to share state across component boundaries, and you would see cryptic errors like "hooks can only be called inside a React component."

The Vite config in every plugin app solves this by marking React as **external**:

```ts
// src/Blocks/.../Apps/vite.config.ts

export default n3oPluginConfig({
    entries: { 'block-preview': 'src/block-preview.ts' },
    outDir: '../wwwroot/App_Plugins/N3O.Umbraco.Blocks.Preview',
    react: true,
    additionalExternals: ['@n3o/backoffice-core'],
});
```

Passing `react: true` to `n3oPluginConfig` expands to marking `react`, `react-dom`, `react-dom/client`, and `react/jsx-runtime` as external in the Rollup/Vite config. This means those `import` statements are **left in the output file unchanged** rather than being bundled in. At runtime, the browser's import map resolves them to `N3O.Umbraco.ReactRuntime`'s pre-built files.

The result: no matter how many plugins are loaded, there is exactly **one** `react.js` file loaded once by the browser, and all plugins share its module instance.

**C# analogy:** This is the exact equivalent of marking `CopyLocal = false` on a shared assembly reference. Every consumer project references the same assembly from the GAC/output directory rather than copying its own private version.

See [ReactRuntime](../apps/reactruntime.md) for the full walkthrough of how the shared React shims are built.

---

## `@n3o/backoffice-core` as a shared runtime

`@n3o/backoffice-core` follows the same pattern as React: compiled once, exposed via an import-map entry, never bundled into individual plugins. The `additionalExternals: ['@n3o/backoffice-core']` in the Blocks `vite.config.ts` ensures this.

```json
"importmap": {
    "imports": {
        "@n3o/backoffice-core": "/App_Plugins/N3O.Umbraco.BackofficeCore/auth-fetch.js"
    }
}
```

This gives every plugin two things via a single `import` line:

1. **`createAuthFetch`** — the factory that builds a bearer-token-injecting `fetch` wrapper.
2. **`UmbAuthFetchMixin`** — the mixin that wires an element to `UMB_AUTH_CONTEXT` and keeps `this.authFetch` current.

Plugins that need authenticated calls (Blocks, SerpEditor, Cloud Platforms) apply the mixin to their shell class. Plugins that do not need it (WelcomeDashboard, Cells) skip it.

See [BackofficeCore](../apps/backofficecore.md) for the full source walkthrough.

---

## `uui-react.d.ts` — giving TypeScript knowledge of web components

Each `Apps/src/` folder contains a file like this:

`src/Blocks/N3O.Umbraco.Blocks.StaticAssets/Apps/src/uui-react.d.ts`:
```ts
import 'react';

declare module 'react' {
    namespace JSX {
        interface IntrinsicElements {
            'uui-loader': any;
        }
    }
}
```

**What this is:** A TypeScript **declaration merging** file. It extends the `react` module's `JSX.IntrinsicElements` interface — the type that tells TypeScript which HTML/custom-element tag names are valid in JSX and what attributes they accept. Without this, TypeScript emits `Property 'uui-loader' does not exist on type 'JSX.IntrinsicElements'`.

**Why `any`:** The UUI library ships Lit types, which are incompatible with React's JSX intrinsic element system. The `any` type opts out of type-checking for these elements. This is a deliberate trade-off: we gain TSX compilation; we lose attribute-level type safety for `uui-*` elements.

**Why each app has its own:** Only the elements actually used by that app are declared. This keeps the files small and explicit. Add a new `uui-*` tag to a TSX file, add its declaration to the same app's `uui-react.d.ts`.

---

## The `?inline` CSS import — injecting styles into shadow DOM

Global CSS does not reach inside shadow roots. Each React component that needs styles imports its CSS file with the `?inline` query:

```ts
// src/Blocks/.../block-preview-app.tsx (line 1)
import styles from './block-preview-app.css?inline';
```

The `?inline` suffix is a Vite feature: instead of emitting a separate `.css` file and generating a `<link>` tag, Vite inlines the file's contents as a JavaScript string. The component then injects that string into the React tree as a `<style>` tag inside the shadow root:

```tsx
export function BlockPreviewApp({ loaded, markup }) {
    return (
        <>
            { /* ... actual content ... */ }
            <style>{styles}</style>
        </>
    );
}
```

React renders the `<style>` tag inside the shadow root's DOM tree. Because it is inside the shadow root, the styles are scoped and do not leak out to the surrounding page.

**C# analogy:** Think of it as embedding a CSS file as a `Resources` string in a class, then writing it to the `<head>` of the component's own isolated iframe. The `?inline` import is the embedding step; the `<style>` tag is the injection step.

---

## The `uui-box` + native input gotcha

As noted in [09 — Umbraco Backoffice Extensions](./09-umbraco-backoffice-extensions.md), interactive `uui-*` form controls break when rendered by React in Umbraco 17. The root cause is a conflict between React's synthetic event system and these Lit-based web components' internal shadow DOM lifecycle.

**The rule:**
- `<uui-box>`, `<umb-property-layout>`, `<uui-loader>`, `<uui-tag>`, `<uui-icon>` — safe to use from React. They are structural or read-only.
- `<uui-input>`, `<uui-textarea>`, `<uui-select>`, `<uui-checkbox>`, `<uui-button>` in interactive form use — **do not use from React**. Use native `<input>`, `<textarea>`, `<select>`, `<button>` instead.

The `serp-editor-app.tsx` demonstrates the correct pattern:

```tsx
// src/Plugins/SerpEditor/.../serp-editor-app.tsx (lines 72–96)

return (
    <uui-box headline="SEO preview">
        <div className="sv">
            <div className="sv-form">
                <input          {/* native — not <uui-input> */}
                    type="text"
                    value={title}
                    onChange={(e) => onChange({ title: e.target.value, description })}
                />
                <textarea       {/* native — not <uui-textarea> */}
                    value={description}
                    onChange={(e) => onChange({ title, description: e.target.value })}
                />
            </div>
        </div>
        <style>{styles}</style>
    </uui-box>
);
```

`<uui-box>` provides the backoffice-standard chrome (the grey bordered card with a headline). Native `<input>` and `<textarea>` provide the actual interactive inputs. They look like plain HTML but are styled via the `?inline` CSS import to match the backoffice design language.

---

## Simpler variants of the pattern

Not every plugin needs all seven steps. Here are the common variants:

### Shell-only, no auth, no contexts (Welcome Dashboard)

`src/Plugins/WelcomeDashboard/.../welcome-dashboard.ts`:
```ts
@customElement('n3o-welcome-dashboard')
export class N3oWelcomeDashboardElement extends HTMLElement {
    #root?: Root;
    #mount: HTMLDivElement;

    constructor() {
        super();
        const shadow = this.attachShadow({ mode: 'open' });
        this.#mount = document.createElement('div');
        shadow.appendChild(this.#mount);
    }

    connectedCallback(): void {
        this.#root ??= createRoot(this.#mount);
        this.#root.render(createElement(WelcomeDashboardApp));
    }

    disconnectedCallback(): void {
        this.#root?.unmount();
        this.#root = undefined;
    }
}
```

No mixin, no contexts, no props — just mount and unmount. The React app is effectively static HTML.

### Shell with property editor contract and onChange (Cells, SerpEditor)

Shell implements `UmbPropertyEditorUiElement`. The value and config are received via property setters; changes are reported via `UmbPropertyValueChangeEvent`. `authFetch` is forwarded to the React app as a prop if the app needs to call the server.

### Shell with Lit rendering instead of React (DynamicListViews)

`src/N3O.Umbraco.Cms/Apps/N3O.Umbraco.DynamicListViews/src/dynamic-list-view.ts` is the one plugin in this repo that does **not** use React. It extends `UmbElementMixin(LitElement)` and uses Lit's `html` tagged template literal for its rendering. This is appropriate because the view is a simple read-only table with no complex interactivity — the overhead of mounting React would not be justified. The shell pattern (consume contexts, render into shadow DOM) is identical; only the rendering technology differs.

---

## What to read next

The per-app docs apply this pattern to each specific plugin. Now that you understand the shell↔React↔Umbraco↔server flow, each app doc is just "here is what data this plugin's shell collects" and "here is what its React component renders."

- [BackofficeCore](../apps/backofficecore.md) — the full source walkthrough of `auth-fetch.ts` and `workspace-visibility-condition.ts`.
- [ReactRuntime](../apps/reactruntime.md) — how the shared React shims are built and why they must use explicit named exports.
- Specific plugin apps: [blocks](../apps/blocks.md), [cells](../apps/cells.md), [serpeditor](../apps/serpeditor.md), [cloud-platforms](../apps/cloud-platforms.md), [welcomedashboard](../apps/welcomedashboard.md), [dynamiclistviews](../apps/dynamiclistviews.md), [scheduler](../apps/scheduler.md).
