# App: Blocks Preview (`N3O.Umbraco.Blocks.StaticAssets`)

> Concept reminders used in this doc — read these first if you are new to any term:
> - [The Big Picture](../concepts/01-the-big-picture.md)
> - [Node, npm, and the Workspace](../concepts/03-node-npm-and-the-workspace.md)
> - [Vite and the Build](../concepts/05-vite-and-the-build.md)
> - Web Components and Shadow DOM — `../concepts/06-web-components-and-shadow-dom.md`
> - Lit — `../concepts/07-lit.md`
> - React — `../concepts/08-react.md`
> - Umbraco Backoffice Extensions — `../concepts/09-umbraco-backoffice-extensions.md`
> - **The N3O Bridge Pattern — `../concepts/10-the-n3o-bridge-pattern.md`** (this app is the canonical example)

---

## 1. What it is

This app registers a **Block Grid custom view** (`blockEditorCustomView` extension type) for the Umbraco backoffice. When an editor opens a document that contains a Block Grid property editor, Umbraco renders this custom view instead of its built-in block tile UI.

What the custom view does:

1. Reads the current Block Grid value from Umbraco's in-memory block contexts (the data the editor has typed but not yet saved).
2. POSTs that data to a server-side C# controller (`blockPreviewBackoffice/previewGridBlock`), which renders the block as real HTML using the website's Razor templates.
3. Displays that server-rendered HTML inside a lightweight React component, giving editors a live WYSIWYG preview without leaving the backoffice.

**Extension type registered:** `blockEditorCustomView`

**Where built output is served from:** The Vite build writes JavaScript to `src/Blocks/N3O.Umbraco.Blocks.StaticAssets/wwwroot/App_Plugins/N3O.Umbraco.Blocks.Preview/`. The `N3O.Umbraco.Blocks.StaticAssets` C# project ships that folder as static web assets, so ASP.NET Core serves the file at `/App_Plugins/N3O.Umbraco.Blocks.Preview/block-preview.js` — exactly the URL recorded in `umbraco-package.json`.

**This is the canonical bridge pattern example** — the clearest demonstration of how every N3O backoffice plugin that needs React works: a thin Lit/web-component shell owns the Umbraco API contract while React renders the UI. See [The N3O Bridge Pattern](../concepts/10-the-n3o-bridge-pattern.md) for the general explanation; this doc makes it concrete with real code.

---

## 2. Files

### Source (`Apps/` directory)

| File | Purpose |
|---|---|
| `package.json` | npm package manifest: declares the package name, marks it private (never published), lists build scripts, and declares dev-dependencies on the shared `@n3o/backoffice-core` and `@n3o/build` workspace packages. |
| `tsconfig.json` | TypeScript compiler configuration. Extends the shared base (`@n3o/build/tsconfig`) and adds `"jsx": "react-jsx"` so `.tsx` files compile correctly. |
| `vite.config.ts` | Build configuration. Delegates to `n3oPluginConfig()` from `@n3o/build` — sets the entry point, output directory, and which modules to leave external (not bundled). |
| `src/block-preview.ts` | **The Lit shell** (custom element `n3o-block-preview`). Owns the Umbraco block-editor contract, handles authentication, fetches the preview from the server, and mounts React into its shadow DOM. This is the heart of the bridge pattern. |
| `src/block-preview-app.tsx` | **The React component** (`BlockPreviewApp`). Pure presentational: given a `loaded` flag and an `markup` string from the shell, it renders either a loading indicator or the server-rendered HTML. |
| `src/block-preview-app.css` | Scoped CSS for the preview frame and the loading/error alert banners. Imported by the `.tsx` file as an inline string and injected via a `<style>` tag inside the shadow DOM. |
| `src/uui-react.d.ts` | TypeScript declaration shim. Tells the TypeScript compiler that `<uui-loader>` is a valid JSX element, so using Umbraco UI web components inside React TSX does not produce a type error. |

### Manifest (shipped under `wwwroot/`)

| File | Purpose |
|---|---|
| `wwwroot/App_Plugins/N3O.Umbraco.Blocks.Preview/umbraco-package.json` | Tells Umbraco what to register: one `blockEditorCustomView` extension pointing at the built JS file. |

---

## 3. End-to-end flow

```
Umbraco backoffice boots
  └─ reads umbraco-package.json
       └─ registers blockEditorCustomView extension
            └─ Umbraco instantiates <n3o-block-preview> (block-preview.js)
                 │
                 ├─ constructor(): attachShadow, consumeContext (workspace + block contexts)
                 │    └─ UmbAuthFetchMixin wires up authenticated fetch
                 │
                 ├─ connectedCallback(): createRoot(mount), scheduleReload(0)
                 │
                 ├─ #loadPreview(): authFetch POST → /umbraco/backoffice/api/blockPreviewBackoffice/previewGridBlock
                 │    └─ server renders Razor → returns HTML string as JSON
                 │
                 └─ #render(): root.render(<BlockPreviewApp loaded={...} markup={...} />)
                      └─ React paints either <uui-loader> or <div dangerouslySetInnerHTML>
```

**Key relationships:**

- **Manifest → shell element:** `umbraco-package.json` records `"element": "/App_Plugins/N3O.Umbraco.Blocks.Preview/block-preview.js"`. Umbraco imports that module; the module's side effect (`@customElement('n3o-block-preview')`) registers the custom element in the browser.
- **Shell → React mount:** The shell creates a `<div>` inside its shadow root and calls `createRoot(div)` from `react-dom/client`. React then owns that subtree. The shell never writes HTML directly; it calls `root.render(...)` passing a `createElement(BlockPreviewApp, props)` call.
- **Shell → server:** The shell calls `this.authFetch(url, { method: 'POST', ... })`. `authFetch` is provided by `UmbAuthFetchMixin` and automatically attaches the Umbraco OAuth bearer token — without this the endpoint returns HTTP 401.
- **React → shadow DOM CSS:** The `.tsx` imports the CSS file with the `?inline` suffix (a Vite feature), which gives it the CSS as a plain string. React injects `<style>{styles}</style>` inside the shadow root so the styles are scoped and do not leak into the page. See [Vite and the Build](../concepts/05-vite-and-the-build.md) and `../concepts/06-web-components-and-shadow-dom.md` for why scoping matters.

---

## 4. File-by-file walkthrough

### `package.json`

```json
{
    "name": "n3o-umbraco-blocks-preview",
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

- `"private": true` — prevents accidental `npm publish`. Think of it like `<IsPackable>false</IsPackable>` in a `.csproj`.
- `"type": "module"` — tells Node.js to treat `.js` files as ES modules (not CommonJS). Required for modern tooling. Think of it as always using `using` rather than `require`.
- `"build": "tsc --noEmit && vite build"` — runs the TypeScript compiler first to type-check (but produce no output files), then Vite to actually bundle. The `&&` means Vite only runs if TypeScript finds no errors.
- `"watch": "vite build --watch"` — rebuild on every save; used during development.
- `"@n3o/backoffice-core": "*"` and `"@n3o/build": "*"` — workspace package references. The `*` version means "whatever version is in this npm workspace". See [Node, npm, and the Workspace](../concepts/03-node-npm-and-the-workspace.md).
- Both are `devDependencies` (not `dependencies`) because they are only needed at build time; they are either bundled into the output or left external.

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

- `"extends": "@n3o/build/tsconfig"` — inherits the shared base configuration from the `@n3o/build` workspace package (`src/N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/base.json`). The base sets strict mode, target ESNext, module resolution, and so on. This app overrides only what it needs to change.
- `"jsx": "react-jsx"` — tells TypeScript how to transform JSX syntax. `"react-jsx"` maps to React 17+'s automatic JSX transform: the compiler inserts `import { jsx as _jsx } from 'react/jsx-runtime'` automatically, so `.tsx` files do not need `import React from 'react'` at the top. If you used `"react"` (the old mode) you would need that manual import.
- `"include": ["src"]` — only type-check and compile files inside `src/`. Config files like `vite.config.ts` are excluded from strict type-checking; Vite handles them separately.

See [Vite and the Build](../concepts/05-vite-and-the-build.md) for more on how TypeScript and Vite interact.

### `vite.config.ts`

```typescript
import { n3oPluginConfig } from '@n3o/build';

export default n3oPluginConfig({
    entries: { 'block-preview': 'src/block-preview.ts' },
    outDir: '../wwwroot/App_Plugins/N3O.Umbraco.Blocks.Preview',
    react: true,
    additionalExternals: ['@n3o/backoffice-core'],
});
```

`n3oPluginConfig` is a factory function defined in `src/N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/vite-config.js`. It calls Vite's `defineConfig()` with a standardised configuration. Understanding each option:

- `entries: { 'block-preview': 'src/block-preview.ts' }` — the entry point. Vite starts here and follows imports. The key (`'block-preview'`) becomes the output filename (`block-preview.js`). Think of it as the `<Compile Include="...">` root of a C# project.
- `outDir: '../wwwroot/App_Plugins/N3O.Umbraco.Blocks.Preview'` — where to write the built files. The path is relative to the `vite.config.ts` file, so it resolves to the `wwwroot/App_Plugins/` folder that ASP.NET Core serves as static files.
- `react: true` — instructs `n3oPluginConfig` to mark `react`, `react-dom`, `react-dom/client`, and `react/jsx-runtime` as **external**. "External" means Vite does not bundle these packages into the output file; instead it emits `import 'react'` statements that the browser must resolve at runtime via the import map. See [Vite and the Build](../concepts/05-vite-and-the-build.md) and `../concepts/04-es-modules-and-import-maps.md`.
- `additionalExternals: ['@n3o/backoffice-core']` — also leaves `@n3o/backoffice-core` external. That package is published as its own JS file by its own build; it is not duplicated inside this bundle.

The resulting `n3oPluginConfig` call (from `vite-config.js` lines 4–29) builds a single ES-module library with no chunking, outputs `block-preview.js`, and sets `emptyOutDir: false` so successive builds do not wipe other files in the `App_Plugins` folder.

### `src/block-preview.ts` — the Lit shell

This is the most complex file in the app. It is a TypeScript class that extends the browser's `HTMLElement`, enhanced by two mixins. Read it as a class with a lifecycle — very similar to an ASP.NET Core middleware or a Blazor component.

#### Imports (lines 1–14)

```typescript
import { customElement } from '@umbraco-cms/backoffice/external/lit';
import { UMB_BLOCK_ENTRY_CONTEXT, UMB_BLOCK_MANAGER_CONTEXT } from '@umbraco-cms/backoffice/block';
import type { UmbBlockManagerContext, UmbBlockLayoutBaseModel, ... } from '@umbraco-cms/backoffice/block';
import type { UmbBlockEditorCustomViewElement } from '@umbraco-cms/backoffice/block-custom-view';
import { UMB_DOCUMENT_WORKSPACE_CONTEXT } from '@umbraco-cms/backoffice/document';
import type { UmbActiveVariant } from '@umbraco-cms/backoffice/workspace';
import { UmbAuthFetchMixin, UmbElementMixin } from '@n3o/backoffice-core';
import type { AuthFetch } from '@n3o/backoffice-core';
import { createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { BlockPreviewApp } from './block-preview-app';
```

- `@umbraco-cms/backoffice/*` imports — these are Umbraco's own backoffice TypeScript packages. All start with `@umbraco-cms/backoffice/` and are **external** in the build (Umbraco provides them via its own script loading; you never bundle them). Think of them as NuGet packages that are already on the server — you reference their types but do not ship the binary.
- `UMB_BLOCK_ENTRY_CONTEXT`, `UMB_BLOCK_MANAGER_CONTEXT`, `UMB_DOCUMENT_WORKSPACE_CONTEXT` — these are **context tokens** (typed symbols). In Umbraco's backoffice, parent elements publish data under a token and child elements consume it by presenting the same token. It is the frontend equivalent of .NET's dependency injection: you register a service under a key and resolve it by the same key. The "context" here is an observable stream of the document workspace data and the block editor's in-memory state.
- `UmbAuthFetchMixin`, `UmbElementMixin` — **mixins**. A mixin is a TypeScript pattern for adding behaviour to a class without inheritance. Think of them as C# extension methods, but they add instance methods and lifecycle hooks instead of just static helpers. `UmbElementMixin` adds the `consumeContext` and `observe` methods. `UmbAuthFetchMixin` (from `src/N3O.Umbraco.Cms/Apps/N3O.Umbraco.BackofficeCore/src/auth-fetch.ts`) adds the `authFetch` property and resolves the OAuth token automatically.
- `createElement`, `createRoot` — React's API for creating a component tree and mounting it into the DOM. Both are **external** — they come from the `N3O.Umbraco.ReactRuntime` import map at runtime.
- `BlockPreviewApp` — the React component defined in the sibling file.

#### Class declaration (lines 33–34)

```typescript
@customElement(elementName)
export class N3oBlockPreviewElement
    extends UmbAuthFetchMixin(UmbElementMixin(HTMLElement))
    implements UmbBlockEditorCustomViewElement {
```

- `@customElement('n3o-block-preview')` — a decorator (syntactic sugar for calling `customElements.define('n3o-block-preview', N3oBlockPreviewElement)`). This registers the class as the implementation of the `<n3o-block-preview>` HTML tag. When Umbraco's browser runtime encounters that tag, it creates an instance of this class. Think of it like `[ApiController]` in ASP.NET — declarative registration.
- `UmbAuthFetchMixin(UmbElementMixin(HTMLElement))` — mixin chaining. Reading from inside out: `HTMLElement` is the base browser class; `UmbElementMixin` wraps it and adds Umbraco context API; `UmbAuthFetchMixin` wraps that and adds the authenticated fetch. The result is a single class with all three sets of behaviour. In C# you would express this as multiple interface implementations plus a base class — TypeScript uses composition.
- `implements UmbBlockEditorCustomViewElement` — an interface contract from Umbraco saying this element must expose `content` and `settings` property setters. Umbraco's block editor calls `element.content = <data>` when the block data changes; the setters below respond.

#### Private fields (lines 37–69)

```typescript
#content?: UmbBlockEditorCustomViewElement['content'];
#settings?: UmbBlockEditorCustomViewElement['settings'];
#root?: Root;
#mount: HTMLDivElement;
#loaded = false;
#markup = '';
#nodeKey: string | undefined;
// ...
```

The `#` prefix denotes **private class fields** (a JavaScript language feature, not TypeScript-only). They are truly private — inaccessible outside the class even at runtime — unlike TypeScript's `private` keyword, which only enforces privacy at compile time. Think of them as the private backing fields you would declare in a C# class.

- `#root` — the React root. Analogous to holding a reference to a Blazor `RenderFragment` or a hosted service instance: you need it to later call `render()` and `unmount()`.
- `#mount` — a plain `<div>` created in the constructor and placed inside the shadow DOM. React renders into this div.
- `#loaded` and `#markup` — simple state tracking. `#loaded` starts `false` so React shows the loading UI immediately.
- `#reloadHandle` — the return value of `setTimeout`, stored so the pending callback can be cancelled with `clearTimeout`. This is the JS equivalent of a `CancellationTokenSource`.
- `#blockManager` — a reference to Umbraco's `UmbBlockManagerContext`, which exposes the current in-memory block grid value (layouts, content data, settings data, expose records).

#### Constructor (lines 71–114)

The constructor runs once when the element is created (before it is attached to the DOM).

```typescript
constructor() {
    super();

    const shadow = this.attachShadow({ mode: 'open' });
    this.#mount = document.createElement('div');
    shadow.appendChild(this.#mount);

    this.consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, (context) => { ... });
    this.consumeContext(UMB_BLOCK_ENTRY_CONTEXT, (context) => { ... });
    this.consumeContext(UMB_BLOCK_MANAGER_CONTEXT, (context) => { this.#blockManager = context; });
}
```

- `this.attachShadow({ mode: 'open' })` — creates an **encapsulated subtree** (the shadow DOM) for this element. CSS and JavaScript outside the element cannot directly reach elements inside the shadow root. `mode: 'open'` means the shadow root is accessible via `element.shadowRoot` (useful for dev tools). See `../concepts/06-web-components-and-shadow-dom.md`.
- `document.createElement('div')` — creates the React mount point in memory and immediately attaches it to the shadow root. React will own everything inside this div.
- `this.consumeContext(TOKEN, callback)` — subscribes to a context provided by a parent element. The callback fires when the context value first resolves and again each time it changes. This is Umbraco's dependency injection mechanism for the backoffice: parent components publish services (workspace, block manager, auth) and children consume them by token.

Inside the workspace context callback (lines 78–98), `this.observe(ctx.unique, ...)` and `this.observe(ctx.splitView.activeVariantsInfo, ...)` subscribe to **observable streams** (RxJS `Observable`). An observable is like an async `IEnumerable<T>` — it emits values over time. The `'_observeUnique'` string is a subscription key so the mixin can automatically unsubscribe when the element disconnects. Each emission updates a private field and the preview is automatically refreshed.

#### Property setters (lines 40–56)

```typescript
set content(value: ...) {
    this.#content = value;
    this.#onDataChanged();
}
```

Umbraco calls these setters whenever the block data or settings change. The setter stores the new value and calls `#onDataChanged()`, which schedules a debounced reload with a 500 ms delay. This is the same pattern as C#'s `OnPropertyChanged` in MVVM — the setter triggers a side effect.

#### `authFetchChanged` (line 117)

```typescript
authFetchChanged(_authFetch: AuthFetch | null): void {
    this.#scheduleReload(0);
}
```

`UmbAuthFetchMixin` calls this hook whenever the `authFetch` property changes (i.e., when the OAuth context resolves on startup). Without this, the first preview load would fail because `authFetch` would still be `null` when `connectedCallback` fires. The `0` delay means "run on the next event loop tick, after the current call stack clears."

#### `connectedCallback` (lines 121–129)

```typescript
connectedCallback(): void {
    super.connectedCallback();
    this.#root ??= createRoot(this.#mount);
    this.#render();
    this.#scheduleReload(0);
}
```

Called by the browser when the element is **inserted into the live DOM** — equivalent to `OnAfterRender` in Blazor or `OnConnected` in SignalR. The `??=` operator assigns only if the left side is null or undefined (short for `if (this.#root == null) this.#root = createRoot(...)`).

- `createRoot(this.#mount)` — creates the React root. After this call, React owns the `#mount` div. You must call `root.render()` to paint anything, and `root.unmount()` to clean up.
- `this.#render()` — paints React immediately with `loaded: false`, showing the loading spinner before any network request.
- `this.#scheduleReload(0)` — queues the actual API fetch. Deferring to the next tick (delay 0) gives the context subscriptions time to fire and populate `#nodeKey`, `#documentTypeKey`, and so on.

#### `disconnectedCallback` (lines 131–141)

```typescript
disconnectedCallback(): void {
    super.disconnectedCallback();
    clearTimeout(this.#reloadHandle);
    this.#root?.unmount();
    this.#root = undefined;
}
```

Called when the element is **removed from the DOM** — equivalent to `IDisposable.Dispose()` in C#. Critical to call `root.unmount()` to let React clean up event listeners and internal state; otherwise memory leaks occur. Any pending `setTimeout` is also cancelled.

#### `#loadPreview` (lines 186–216)

```typescript
async #loadPreview(): Promise<void> {
    const blockData = this.#buildBlockData();
    if (!blockData || !this.#documentTypeKey || !this.authFetch) { return; }

    const url = `/umbraco/backoffice/api/blockPreviewBackoffice/previewGridBlock/?nodeKey=...`;

    const response = await this.authFetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(blockData),
    });

    if (!response.ok) { return; }

    const markup = (await response.json()) as string;
    this.#markup = markup;
    this.#loaded = true;
    this.#render();
}
```

- `async #loadPreview(): Promise<void>` — a private async method. In JavaScript/TypeScript, `async` functions always return a `Promise`. The `void` return type (at the call sites `void this.#loadPreview()`) is a deliberate choice to suppress unhandled-promise-rejection warnings while still running the async work — the equivalent of `_ = Task.Run(...)` in C#.
- `this.authFetch(url, {...})` — calls the authenticated fetch provided by `UmbAuthFetchMixin`. Internally (`auth-fetch.ts` line 9–17) it calls `config.token()` to get the current OAuth bearer token and sets the `Authorization: Bearer <token>` header. Using plain `fetch()` here instead would return HTTP 401 because the C# endpoint is decorated with `[Authorize]`.
- `JSON.stringify(blockData)` — serialises the `BlockGridValue` object to a JSON string for the request body. Equivalent to `JsonSerializer.Serialize(blockData)` in C#.
- `(await response.json()) as string` — deserialises the JSON response. The C# endpoint returns the server-rendered HTML markup as a JSON-encoded string (i.e., a JSON string value, not an object). The `as string` is a TypeScript type assertion — it does not perform a runtime cast (unlike C#'s `(string)` cast); it just tells the compiler "trust me, this is a string."

#### `#render` (lines 219–226)

```typescript
#render(): void {
    this.#root?.render(
        createElement(BlockPreviewApp, {
            loaded: this.#loaded,
            markup: this.#markup,
        }),
    );
}
```

- `this.#root?.render(...)` — the optional-chaining operator `?.` means "call `render` only if `#root` is not null/undefined." Safe to call before the root is created.
- `createElement(BlockPreviewApp, { loaded, markup })` — creates a React element (a virtual DOM node describing what to render). This is equivalent to writing `<BlockPreviewApp loaded={...} markup={...} />` in JSX — in fact, JSX compiles down to exactly this `createElement` call. The shell uses `createElement` directly (no JSX) because it is a `.ts` file, not a `.tsx` file.

---

### `src/block-preview-app.tsx` — the React component

```tsx
import styles from './block-preview-app.css?inline';

interface BlockPreviewAppProps {
    loaded: boolean;
    markup: string;
}

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

#### Line-by-line

- `import styles from './block-preview-app.css?inline'` — the `?inline` suffix is a Vite-specific import modifier. It tells Vite to return the CSS file's contents as a plain string (rather than injecting it into the page `<head>` as a `<link>` tag). This is necessary because the component lives inside a shadow DOM — a `<link>` in the page head would not penetrate the shadow boundary. See `../concepts/06-web-components-and-shadow-dom.md`.

- `interface BlockPreviewAppProps` — a TypeScript **interface** (think C# `interface`, but purely structural — any object with a `loaded: boolean` and `markup: string` satisfies it, without needing to explicitly implement it). Props are how React components receive data from their caller — analogous to constructor parameters or public properties on a Blazor component.

- `export function BlockPreviewApp({ loaded, markup }: BlockPreviewAppProps)` — a React **function component**. It is just a regular TypeScript function that returns JSX. The `{ loaded, markup }` syntax is **destructuring** — it unpacks the props object into local variables. Equivalent in C# would be: `public RenderFragment Render(BlockPreviewAppProps props) { var loaded = props.loaded; var markup = props.markup; ... }`.

- `<> ... </>` — a React **fragment**. JSX requires a single root element, but you may not always want to introduce a wrapping `<div>`. A fragment groups children without adding a real DOM node. Think of it as a C# tuple that disappears at runtime.

- `{!loaded ? (...) : (...)}` — JSX conditional rendering. The curly braces `{}` switch from JSX mode (HTML-like) into JavaScript expression mode. The ternary operator works exactly as in C#. When `loaded` is false, the loading banner is shown; otherwise the preview frame is shown.

- `<uui-loader style={{ color: '#fff' }}>` — using an Umbraco UI web component inside JSX. The `uui-react.d.ts` file is what allows this without a TypeScript error. Note the double curly braces: the outer `{}` enters JS expression mode in JSX; the inner `{}` is an object literal for the style prop. (In HTML you write `style="color: #fff"` as a string; in React's JSX you always write it as an object.)

- `dangerouslySetInnerHTML={{ __html: markup }}` — sets the element's inner HTML to the server-rendered markup string. The name is intentionally alarming: React normally prevents direct HTML injection to defend against XSS attacks. Here it is safe because the markup comes from our own trusted C# Razor templates, not from user input. In .NET you would use `@Html.Raw(markup)` in a Razor view — same concept, same trust requirement.

- `<style>{styles}</style>` — injects the CSS string (loaded via `?inline`) into the shadow DOM as a `<style>` element. Because this renders inside the shadow root, the styles are scoped to this component and do not affect the rest of the page.

---

### `src/block-preview-app.css`

```css
:host {
    display: block;
}
```

- `:host` — a CSS pseudo-selector that targets the custom element itself (the `<n3o-block-preview>` tag in the parent DOM). Custom elements default to `display: inline`; setting `display: block` makes the element behave like a `<div>`. This is a web-component-specific selector that only works inside a shadow DOM stylesheet.

The remaining rules (`.block-preview-frame`, `.preview-alert-*`) use standard CSS class selectors. Because the stylesheet lives inside the shadow root, there is no risk of these class names clashing with any page-level CSS. The `transform: scale(0.9)` shrinks the preview to 90% to fit the narrow backoffice column.

---

### `src/uui-react.d.ts`

```typescript
import 'react';

declare module 'react' {
    namespace JSX {
        interface IntrinsicElements {
            'uui-loader': any;
        }
    }
}
```

TypeScript needs to know the valid attributes and children of every JSX element. For standard HTML elements (`<div>`, `<span>`, etc.) React ships this information in `@types/react`. For **custom elements** (`<uui-loader>`, etc.) there is no built-in type definition, so we declare them manually here.

- `declare module 'react'` — opens the `react` module's declaration and merges our additions into it. This is TypeScript's **declaration merging** — equivalent to writing a `partial class` in C#.
- `namespace JSX { interface IntrinsicElements { 'uui-loader': any; } }` — adds `'uui-loader'` to the set of known JSX element names. Using `any` as the type means no validation of attributes — add more specific types if you need attribute checking. See the comment in the file: interactive UUI controls are deliberately omitted here (they work differently in the Cloud Platforms app's version — see that doc for the fuller comment).

---

### `wwwroot/App_Plugins/N3O.Umbraco.Blocks.Preview/umbraco-package.json`

```json
{
    "$schema": "http://json.schemastore.org/umbraco-package.json",
    "id": "N3O.Umbraco.Blocks.Preview",
    "name": "N3O Block Preview",
    "version": "17.0.0",
    "extensions": [
        {
            "type": "blockEditorCustomView",
            "alias": "N3O.BlockCustomView.Preview",
            "name": "N3O Block Preview",
            "element": "/App_Plugins/N3O.Umbraco.Blocks.Preview/block-preview.js",
            "forBlockEditor": "block-grid"
        }
    ]
}
```

This file is the **Umbraco extension manifest**. It is read by the Umbraco backoffice on startup. Think of it as the `IComposer`/`[BackOfficeManifest]` mechanism from earlier Umbraco versions, now expressed as JSON.

- `"id"` — unique package identifier. Must be unique across all installed packages.
- `"extensions"` — array of extension registrations. Each entry is one contribution to the backoffice.
- `"type": "blockEditorCustomView"` — tells Umbraco this extension provides a custom rendering component for a block editor. Umbraco will use it in place of the default block tile UI.
- `"alias": "N3O.BlockCustomView.Preview"` — unique identifier for this specific extension within the package. Used by Umbraco to reference it (e.g., for overriding or extending it in downstream packages).
- `"element": "/App_Plugins/.../block-preview.js"` — the JavaScript module to load when this extension activates. Umbraco performs a dynamic `import('/App_Plugins/.../block-preview.js')`, which executes the module and runs the `@customElement(...)` decorator, registering the custom element.
- `"forBlockEditor": "block-grid"` — scopes this custom view to block grid editors only (not block list editors). Without this, Umbraco might try to use it for all block editor types.

---

## 5. Concepts demonstrated

| Concept | Where demonstrated |
|---|---|
| [The Big Picture](../concepts/01-the-big-picture.md) | The full flow from umbraco-package.json to browser |
| [Node, npm, and the Workspace](../concepts/03-node-npm-and-the-workspace.md) | `package.json` workspace package references (`*` versions) |
| [Vite and the Build](../concepts/05-vite-and-the-build.md) | `vite.config.ts` with `n3oPluginConfig`, `?inline` CSS import, external modules |
| ES Modules and Import Maps — `../concepts/04-es-modules-and-import-maps.md` | React and `@umbraco-cms/*` left external; resolved at runtime via `N3O.Umbraco.ReactRuntime` import map |
| Web Components and Shadow DOM — `../concepts/06-web-components-and-shadow-dom.md` | `attachShadow`, `:host` CSS selector, scoped `<style>` injection |
| Lit — `../concepts/07-lit.md` | `@customElement`, `UmbElementMixin`, `consumeContext`, `observe` |
| React — `../concepts/08-react.md` | `createRoot`, function component, props interface, JSX, fragments, `dangerouslySetInnerHTML` |
| Umbraco Backoffice Extensions — `../concepts/09-umbraco-backoffice-extensions.md` | `blockEditorCustomView` type, `umbraco-package.json`, context tokens |
| **[The N3O Bridge Pattern](../concepts/10-the-n3o-bridge-pattern.md)** | The entire shell/React split; `UmbAuthFetchMixin`; `createRoot` in shadow DOM |

---

## 6. Gotchas

**1. React is not bundled — it must be in the import map**

If `N3O.Umbraco.ReactRuntime` is not loaded (i.e., its `umbraco-package.json` is not deployed), any import of `react` or `react-dom` will fail at runtime with a module-not-found error. The `react: true` flag in `vite.config.ts` only omits React from the bundle; the import map in `N3O.Umbraco.Cms/wwwroot/App_Plugins/N3O.Umbraco.ReactRuntime/umbraco-package.json` is what actually resolves the runtime imports.

**2. Plain `fetch()` returns 401**

The preview endpoint (`blockPreviewBackoffice/previewGridBlock`) is protected by `[Authorize]`. Calling `fetch(url)` without an `Authorization` header gets a 401 response. Always use `this.authFetch` (from `UmbAuthFetchMixin`) for any backoffice API call. `authFetch` is `null` until `UMB_AUTH_CONTEXT` resolves — the guard `if (!this.authFetch)` at the top of `#loadPreview` handles this.

**3. Shadow DOM isolates CSS — for better and worse**

Page-level CSS and Umbraco's design system CSS do not apply inside the shadow root. The `:host { display: block; }` rule and all `.preview-alert-*` rules must be declared inside the shadow root (via the `?inline` import pattern). Conversely, the preview HTML rendered by the server (via `dangerouslySetInnerHTML`) is injected into a `<div>` that lives inside the shadow root — if that HTML relies on page-level CSS classes, it will not see them. The preview endpoint must serve self-contained HTML.

**4. `#onDataChanged` debounce only fires after first load**

The guard `if (this.#loaded)` in `#onDataChanged` means that if the `content` or `settings` setters fire before the first preview loads (e.g., during initial render), they do not schedule an extra reload. The initial load is always triggered by `#scheduleReload(0)` in `connectedCallback` and `authFetchChanged`.

**5. `createRoot` must only be called once**

The `??=` operator in `connectedCallback` (`this.#root ??= createRoot(this.#mount)`) ensures `createRoot` is called at most once. Calling it a second time on the same DOM node throws a React warning. The root is cleared in `disconnectedCallback` (`this.#root = undefined`) so it can be recreated if the element is re-attached.

**6. TypeScript `as` is not a runtime cast**

`(await response.json()) as string` does not throw if the value is not a string — it only suppresses the TypeScript error. If the server returns something unexpected, `this.#markup` will silently hold the wrong type and the preview will break without a clear error. Add runtime validation (`typeof markup === 'string'`) if you need defensive handling.
