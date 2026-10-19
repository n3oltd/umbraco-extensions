# N3O.Umbraco.SerpEditor

**Package name:** `n3o-umbraco-serpeditor`
**Source (Apps folder):** `src/Plugins/SerpEditor/N3O.Umbraco.SerpEditor.StaticAssets/Apps/`
**Build output:** `src/Plugins/SerpEditor/N3O.Umbraco.SerpEditor.StaticAssets/wwwroot/App_Plugins/N3O.Umbraco.SerpEditor/`
**Manifest:** `wwwroot/App_Plugins/N3O.Umbraco.SerpEditor/umbraco-package.json`

---

## What it is

The SERP Editor is a **property editor UI** for Umbraco content types. It lets editors enter an SEO title and meta description for a page, and shows them a live preview of how the result will look on a Google search results page (a SERP — Search Engine Results Page).

In Umbraco terms, a **property editor UI** is a custom input control that replaces the default text box or date picker for a content type field. Umbraco renders it inside the backoffice document editor wherever a data type backed by the editor's alias appears. Think of it as a custom Blazor component or a custom WinForms control for a specific data type.

Extension type: `propertyEditorUi`
Served from: `/App_Plugins/N3O.Umbraco.SerpEditor/` (static web assets via ASP.NET Core RCL)

The editor also calls a backend API endpoint (`/umbraco/backoffice/api/serpEditor/templateOptions`) to retrieve the site's configured title suffix (e.g. "| My Site Name") so the preview can append it to the title. That endpoint is decorated with `[Authorize]`, so the frontend must send an OAuth bearer token — this is why the editor uses the shared `authFetch` helper from `@n3o/backoffice-core`.

---

## Files

| File | Role |
|------|------|
| `package.json` | npm package manifest for this app |
| `tsconfig.json` | TypeScript compiler config; extends shared base from `@n3o/build` |
| `vite.config.ts` | Vite build config; uses the shared `n3oPluginConfig` preset |
| `src/serp-editor.ts` | **The web-component shell** — the entry point Umbraco loads |
| `src/serp-editor-app.tsx` | **The React app** — all UI and the `templateOptions` fetch |
| `src/serp-editor-app.css` | CSS for the editor layout and the SERP preview styling |
| `src/uui-react.d.ts` | TypeScript ambient declarations allowing `<uui-box>` etc. inside JSX |
| `wwwroot/.../umbraco-package.json` | Umbraco manifest — tells Umbraco this file is a `propertyEditorUi` |

---

## End-to-end flow

This section traces the full lifecycle from Umbraco startup to the editor appearing on screen and saving a value. For the general bridge-pattern architecture, see [../concepts/10-the-n3o-bridge-pattern.md](../concepts/10-the-n3o-bridge-pattern.md).

### 1. Umbraco discovers the manifest

At startup Umbraco scans every `App_Plugins/*/umbraco-package.json` and registers extensions. It reads:

```json
{
    "type": "propertyEditorUi",
    "alias": "N3O.Umbraco.SerpEditor",
    "element": "/App_Plugins/N3O.Umbraco.SerpEditor/serp-editor.js"
}
```

Two things happen:
- The alias `N3O.Umbraco.SerpEditor` is registered as a known property editor UI. Any data type configured with `propertyEditorSchemaAlias: "N3O.Umbraco.SerpEditor"` (set in the C# `[DataEditor]` attribute on the backend) will use this UI.
- The file path `/App_Plugins/N3O.Umbraco.SerpEditor/serp-editor.js` is added to the backoffice's module graph so the browser loads it when the editor is needed.

For the alias rule — why the manifest alias must match the C# `[DataEditor]` alias — see [../concepts/09-umbraco-backoffice-extensions.md](../concepts/09-umbraco-backoffice-extensions.md).

### 2. Browser loads `serp-editor.js` and registers the custom element

The file is a standard ES module. When it is first imported, the top-level `@customElement('n3o-serp-editor')` decorator (executed as module-level side-effect code) registers the `<n3o-serp-editor>` tag with the browser's custom element registry.

Think of this like a static constructor in C# that runs once when the class is first referenced.

### 3. Umbraco instantiates the element for a property

When an editor opens a document that has a SERP property, Umbraco creates an instance of `<n3o-serp-editor>` in the DOM. The element's constructor runs:

```typescript
constructor() {
    super();
    const shadow = this.attachShadow({ mode: 'open' });
    this.#mount = document.createElement('div');
    shadow.appendChild(this.#mount);
}
```

A **shadow root** is attached (isolated DOM subtree — see [../concepts/06-web-components-and-shadow-dom.md](../concepts/06-web-components-and-shadow-dom.md)) and a `<div>` mount point is placed inside it. No React tree exists yet.

### 4. Umbraco sets `value` and `config`

Umbraco calls the element's `set value(...)` setter with the persisted JSON value `{ title: "...", description: "..." }` and the `set config(...)` setter with the data type's prevalues (the configured `maxCharsTitle` and `maxCharsDescription` limits).

Both setters call `#render()`, which calls `this.#root?.render(...)`. However, at this point `this.#root` is still `undefined` (React has not been mounted yet), so `?.render` short-circuits and nothing happens.

### 5. `connectedCallback` fires, React mounts

When the element is inserted into the document's live DOM, the browser calls `connectedCallback`:

```typescript
connectedCallback(): void {
    super.connectedCallback?.();
    this.#root ??= createRoot(this.#mount);
    this.#render();
}
```

`??=` is the **nullish assignment** operator — equivalent to `if (this.#root == null) { this.#root = createRoot(this.#mount); }`. `createRoot` is React 18/19's API that attaches a React reconciler to a DOM node. From this point forward, the `<div>` inside the shadow root is managed by React.

`#render()` is then called for real:

```typescript
#render(): void {
    this.#root?.render(
        createElement(SerpEditorApp, {
            value: this.#value,
            maxCharsTitle: this.#maxCharsTitle,
            maxCharsDescription: this.#maxCharsDescription,
            authFetch: this.authFetch,
            onChange: (value: SerpValue) => {
                this.#value = value;
                this.dispatchEvent(new UmbPropertyValueChangeEvent());
            },
        }),
    );
}
```

`createElement` is the un-JSX form of `<SerpEditorApp ... />`. The shell does not use JSX in `.ts` files (only `.tsx` files support JSX syntax by convention); this explicit call is equivalent and identical in behaviour.

The `onChange` callback is a closure that writes the new value back to `#value` and dispatches `UmbPropertyValueChangeEvent` — the Umbraco signal that "this property's value has changed, please save it to the model."

### 6. `UmbAuthFetchMixin` delivers the bearer token

`N3oSerpEditorElement` extends `UmbAuthFetchMixin(UmbElementMixin(HTMLElement))`.

`UmbAuthFetchMixin` (from `src/N3O.Umbraco.Cms/Apps/N3O.Umbraco.BackofficeCore/src/auth-fetch.ts`) calls `this.consumeContext(UMB_AUTH_CONTEXT, ...)` in its constructor. The Umbraco context system (a dependency injection mechanism built into the backoffice) will call the callback as soon as an `UMB_AUTH_CONTEXT` provider is found in the element's ancestor chain. When it fires:

```typescript
this.authFetch = authContext
    ? createAuthFetch(authContext.getOpenApiConfiguration())
    : null;
this.authFetchChanged?.(this.authFetch);
```

`authFetchChanged` is overridden on the shell:

```typescript
authFetchChanged(_authFetch: AuthFetch | null): void {
    this.#render();
}
```

This triggers another `#render()`, now passing a non-null `authFetch` prop to `SerpEditorApp`. React performs a reconcile — it re-renders the component tree with the updated prop. This is equivalent to setting a bound property in a ViewModel and the UI databinding updating itself.

`authFetch` is a function with the signature `(input: string, init?: RequestInit) => Promise<Response>`. Internally it obtains the current OAuth bearer token from the auth context and adds `Authorization: Bearer <token>` to every request's headers.

### 7. React renders the UI and fetches `templateOptions`

The `SerpEditorApp` component renders a form (title input + description textarea) and a live preview panel. On first render with a valid `authFetch`, the `useEffect` hook fires and calls the backend:

```typescript
authFetch('/umbraco/backoffice/api/serpEditor/templateOptions')
    .then(response => response.json())
    .then(data => {
        cachedTitleSuffix = data.titleSuffix ?? '';
        if (active) {
            setTitleSuffix(cachedTitleSuffix);
        }
    });
```

When `setTitleSuffix` is called, React re-renders `SerpEditorApp` with the new `titleSuffix` value and the preview updates.

### 8. Editor changes a value

When the user types in the title `<input>`:

```tsx
onChange={(e) => onChange({ title: e.target.value, description })}
```

This calls the `onChange` prop, which is the callback defined in step 5. The callback updates `#value` on the shell element and dispatches `UmbPropertyValueChangeEvent`. Umbraco listens for this event and updates its internal content model. React's `onChange` here is `onInput` in raw HTML terms — it fires on every keystroke, not just on blur.

### 9. `disconnectedCallback` — cleanup

When the document editor is closed, Umbraco removes the element from the DOM. The browser calls `disconnectedCallback`:

```typescript
disconnectedCallback(): void {
    super.disconnectedCallback?.();
    this.#root?.unmount();
    this.#root = undefined;
}
```

`unmount()` tells React to run all `useEffect` cleanup functions and discard the component tree. This is analogous to `IDisposable.Dispose()`.

---

## File-by-file walkthrough

### `package.json`

```json
{
    "name": "n3o-umbraco-serpeditor",
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

- **`"name": "n3o-umbraco-serpeditor"`** — the workspace package name. Not referenced by any other package at build time; it exists so npm's workspace resolution can find this folder and install its `node_modules` symlinks.
- **`"private": true`** — prevents accidental publication to a registry. The .NET equivalent is `<IsPackable>false</IsPackable>`.
- **`"type": "module"`** — declares that `.js` files in this package use ES module syntax. Required because the vite config and source files use `import`/`export`. See [../concepts/04-es-modules-and-import-maps.md](../concepts/04-es-modules-and-import-maps.md).
- **`"build": "tsc --noEmit && vite build"`** — runs the TypeScript compiler first as a type-check-only pass (`--noEmit` means: check types but do not emit files), then runs Vite to actually produce the `.js` output. The `&&` means vite only runs if tsc exits with code 0 (no type errors). Think of tsc here as running `dotnet build` just to validate; Vite is the actual emit step.
- **`"watch": "vite build --watch"`** — rebuilds automatically when source files change; used during local development.
- **`"@n3o/build": "*"`** — the only dependency is the shared build preset. `"*"` resolves to the local workspace package via the npm workspace symlink. It is a `devDependency` because it is only needed at build time, not at runtime in the browser.

For more on `package.json` and the workspace, see [../concepts/03-node-npm-and-the-workspace.md](../concepts/03-node-npm-and-the-workspace.md).

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

- **`"extends": "@n3o/build/tsconfig"`** — inherits the shared TypeScript configuration from `src/N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/base.json` (exposed by the workspace under the name `@n3o/build/tsconfig`). Inherited settings include `target: "ES2022"`, `strict: true`, `experimentalDecorators: true`, and the ambient type reference `@umbraco-cms/backoffice/extension-types`. This is the TypeScript equivalent of a `<Import Project="..." />` in MSBuild.
- **`"jsx": "react-jsx"`** — selects the modern "automatic" JSX transform. When the TypeScript compiler sees `<SomeComponent />` syntax in a `.tsx` file, it rewrites it to a call to `jsx(...)` imported from `react/jsx-runtime` rather than to `React.createElement(...)`. This means `.tsx` files do not need `import React from 'react'` at the top. See [../concepts/08-react.md](../concepts/08-react.md).
- **`"include": ["src"]`** — only type-check files under `src/`. The vite config file at the root is not included; it is covered by the inherited `"files": ["vite-env.d.ts"]` from the base.

---

### `vite.config.ts`

```typescript
import { n3oPluginConfig } from '@n3o/build';

export default n3oPluginConfig({
    entries: { 'serp-editor': 'src/serp-editor.ts' },
    outDir: '../wwwroot/App_Plugins/N3O.Umbraco.SerpEditor',
    react: true,
    additionalExternals: ['@n3o/backoffice-core'],
});
```

`n3oPluginConfig` is a factory function defined in `src/N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/vite-config.js`. It returns a standard Vite `defineConfig(...)` object. Under the hood it sets:

- **Library mode** (`build.lib`) — Vite builds a library (a reusable module), not an application (a full HTML page with code-splitting). The output is a single `.js` file per entry rather than an `index.html` with asset hashes.
- **Entry point** — `'serp-editor': 'src/serp-editor.ts'` maps the entry name `serp-editor` to the source file `src/serp-editor.ts`. The output file will be `serp-editor.js` (controlled by `entryFileNames: '[name].js'`).
- **`outDir: '../wwwroot/App_Plugins/N3O.Umbraco.SerpEditor'`** — relative to the `Apps/` folder, this resolves to `wwwroot/App_Plugins/N3O.Umbraco.SerpEditor/`. ASP.NET Core's static web asset pipeline serves files under `wwwroot/` at the corresponding URL path, so the file becomes available at `/App_Plugins/N3O.Umbraco.SerpEditor/serp-editor.js`.
- **`react: true`** — adds `react`, `react-dom`, `react-dom/client`, and `react/jsx-runtime` to the externals list. These are not bundled into `serp-editor.js`; instead the `import 'react'` statements are left as-is in the output and the browser resolves them via the import map at runtime. This ensures a single shared React instance.
- **`additionalExternals: ['@n3o/backoffice-core']`** — also excludes `@n3o/backoffice-core` from the bundle. This package is provided at runtime by a separate import-map entry (from `BackofficeCore`'s own `umbraco-package.json`). Externalising it means the `authFetch` helper is shared code rather than duplicated in every plugin. The TextResourceEditor does not use `@n3o/backoffice-core` and therefore has no `additionalExternals`.

For more on Vite builds, library mode, and externals, see [../concepts/05-vite-and-the-build.md](../concepts/05-vite-and-the-build.md).

---

### `src/serp-editor.ts` — the web-component shell

This is the file that Umbraco loads. Its job is to implement the Umbraco property editor contract (`UmbPropertyEditorUiElement`) using a web component, then mount the React app inside it.

**Imports**

```typescript
import { customElement } from '@umbraco-cms/backoffice/external/lit';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import {
    UmbPropertyValueChangeEvent,
    type UmbPropertyEditorConfigCollection,
    type UmbPropertyEditorUiElement,
} from '@umbraco-cms/backoffice/property-editor';
import { UmbAuthFetchMixin } from '@n3o/backoffice-core';
import type { AuthFetch } from '@n3o/backoffice-core';
import { createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { SerpEditorApp, type SerpValue } from './serp-editor-app';
```

All `@umbraco-cms/*` imports are external (left as bare specifiers in the output); they resolve via Umbraco's own import map at runtime. `@n3o/backoffice-core` is also external (via `additionalExternals`). `react` and `react-dom/client` are external via the `react: true` flag. Only `./serp-editor-app` is bundled — it is a local file.

**Class declaration**

```typescript
@customElement(elementName)
export class N3oSerpEditorElement
    extends UmbAuthFetchMixin(UmbElementMixin(HTMLElement))
    implements UmbPropertyEditorUiElement
```

The mixin chain reads right-to-left:

1. `HTMLElement` — the base browser class for all elements. Every DOM element (div, span, etc.) ultimately inherits from this.
2. `UmbElementMixin(HTMLElement)` — wraps `HTMLElement` adding Umbraco's context consumption system (`consumeContext`, `provideContext`). Think of this as Umbraco's DI container integration for web components.
3. `UmbAuthFetchMixin(...)` — wraps the result of step 2, adding the `authFetch` property and automatically calling `consumeContext(UMB_AUTH_CONTEXT, ...)` to populate it. Defined in `src/N3O.Umbraco.Cms/Apps/N3O.Umbraco.BackofficeCore/src/auth-fetch.ts`.

In C# terms, each mixin is like a partial class or a decorator that adds state and behaviour without using multiple inheritance.

`implements UmbPropertyEditorUiElement` declares that the class satisfies the interface Umbraco expects: it must have `value` getter/setter and `config` setter.

**Private fields**

```typescript
#root?: Root;
#mount: HTMLDivElement;
#value: SerpValue = { title: '', description: '' };
#maxCharsTitle = 60;
#maxCharsDescription = 160;
```

The `#` prefix is native JavaScript **private class fields** — unlike TypeScript's `private` keyword (which is compile-time only), `#` fields are enforced by the JavaScript runtime and are completely inaccessible outside the class. The equivalence in C# is a `private` field, but with runtime enforcement.

`#root` is the React root — the reconciler handle returned by `createRoot`. It is `undefined` until `connectedCallback` creates it.

**`value` getter and setter**

```typescript
get value(): SerpValue { return this.#value; }
set value(value: SerpValue | undefined) {
    this.#value = value ?? { title: '', description: '' };
    this.#render();
}
```

Umbraco sets this property after the element is created but before it is connected to the DOM. `??` is the **nullish coalescing** operator — it returns the right-hand side only if the left-hand side is `null` or `undefined`. The call to `#render()` here will be a no-op if `#root` is undefined (see the `?.` safe-call in `#render`).

**`set config`**

```typescript
public set config(config: UmbPropertyEditorConfigCollection | undefined) {
    const maxCharsTitle = Number.parseInt(config?.getValueByAlias('maxCharsTitle') ?? '', 10);
    ...
}
```

`config` contains the data type's **prevalues** — configuration set by an administrator in the Umbraco backoffice data types section. These are analogous to constructor parameters in C#, set in the admin UI rather than code. `getValueByAlias` retrieves a named prevalue. `Number.parseInt(..., 10)` parses a string to an integer in base 10; the `?? ''` before it ensures `parseInt` never receives `undefined`.

**`#render()`**

```typescript
#render(): void {
    this.#root?.render(
        createElement(SerpEditorApp, { ... }),
    );
}
```

`?.` is the **optional chaining** operator. If `#root` is `undefined` or `null`, the expression short-circuits and returns `undefined` without calling `.render(...)`. In C# this is equivalent to `this._root?.Render(...)`. Every time this method is called — whether from `set value`, `set config`, `authFetchChanged`, or `connectedCallback` — React receives a fresh props snapshot and performs a reconcile (updating only the DOM nodes that changed).

---

### `src/serp-editor-app.tsx` — the React app

This file contains all the UI logic. The `.tsx` extension (vs `.ts`) tells the TypeScript compiler that this file may contain JSX syntax.

**Interfaces**

```typescript
export interface SerpValue {
    title: string;
    description: string;
}

interface TemplateOptionsResponse {
    titleSuffix?: string;
}

interface SerpEditorAppProps {
    value: SerpValue;
    maxCharsTitle: number;
    maxCharsDescription: number;
    authFetch: AuthFetch | null;
    onChange: (value: SerpValue) => void;
}
```

`SerpValue` is exported because the shell needs to re-use the type. `SerpEditorAppProps` defines the React component's props — analogous to a C# record or class with properties that the caller must supply. The `onChange: (value: SerpValue) => void` prop is a **callback** — a function the parent (the shell) passes in so the child (the React component) can report changes upward without knowing anything about Umbraco's event system.

**Module-level cache**

```typescript
let cachedTitleSuffix: string | undefined;
```

This is a module-level variable. In ES module semantics a module is loaded once per JavaScript runtime and its top-level scope persists for the lifetime of the page — the equivalent of a C# `static` field. This means `cachedTitleSuffix` is shared across all instances of `SerpEditorApp` mounted anywhere on the page in the current browser session.

The comment in the source explains the design rationale: the `templateOptions` response is site-wide and never changes within a session. If a content type has multiple SERP properties (two different fields both using this editor), each would mount its own `SerpEditorApp` instance. Without the cache, each instance would make its own HTTP request to the same endpoint. With the module-level cache, only the first instance to resolve the fetch writes to `cachedTitleSuffix`; every subsequent instance reads it immediately.

**Trade-offs of the module-level cache (vs per-component state)**

| Aspect | Module-level variable | `useState` / component state |
|---|---|---|
| Lifetime | Entire page session (until hard reload) | Component lifetime (unmount clears it) |
| Sharing | All instances see the same value | Each component instance is independent |
| Memory of stale data | Possible if the value can change server-side | Never stale — each mount re-fetches |
| Network requests | At most one per session | One per component mount |
| Reset on hot-reload | Yes (module re-executes) | N/A |

For data like `titleSuffix` — which is a backend configuration value set by an administrator and unlikely to change mid-session — the module-level cache is the right trade-off.

**`SerpEditorApp` component**

```typescript
export function SerpEditorApp({ value, maxCharsTitle, maxCharsDescription, authFetch, onChange }: SerpEditorAppProps) {
    const [titleSuffix, setTitleSuffix] = useState(cachedTitleSuffix ?? '');
```

The initial state value is `cachedTitleSuffix ?? ''`. If a prior instance already fetched and populated the cache, this component starts with the correct value immediately and never needs to fetch. The `??` guard means the cache hit path also handles the case where `cachedTitleSuffix` is `undefined` (first mount, before any fetch completes).

**`useEffect` for the fetch**

```typescript
useEffect(() => {
    if (!authFetch) { return; }
    if (cachedTitleSuffix !== undefined) { return; }

    let active = true;

    authFetch('/umbraco/backoffice/api/serpEditor/templateOptions')
        .then(...)
        .then((data) => {
            cachedTitleSuffix = data.titleSuffix ?? '';
            if (active) {
                setTitleSuffix(cachedTitleSuffix);
            }
        })
        .catch(...);

    return () => { active = false; };
}, [authFetch]);
```

Key points:

- **`[authFetch]` dependency array** — `useEffect` re-runs whenever `authFetch` changes. The first render has `authFetch = null` (the auth context has not arrived yet), so the guard `if (!authFetch) { return; }` exits immediately. When the shell calls `#render()` again after `authFetchChanged` fires, React reconciles with the new non-null `authFetch` prop and the effect re-runs. See [../concepts/08-react.md](../concepts/08-react.md) for `useEffect` in detail.
- **`let active = true`** — a local boolean that is set to `false` by the cleanup function. This guards against a **stale closure race**: if the component unmounts between when the fetch starts and when it resolves, calling `setTitleSuffix` on an unmounted component would log a React warning and potentially trigger a re-render on a dead tree. The `if (active)` check prevents that. The cleanup function (`return () => { active = false; }`) is the React equivalent of `CancellationToken.Cancel()`.
- **Writing to the module-level variable inside the effect** — `cachedTitleSuffix = data.titleSuffix ?? ''` assigns to the module variable, not a React state variable. This happens synchronously inside the `.then` callback and is visible immediately to any other component instance that reads `cachedTitleSuffix` after this point.

**JSX — the preview panel**

```tsx
<uui-box headline="SEO preview">
    <div className="sv">
        <div className="sv-form">
            <input
                type="text"
                value={title}
                placeholder="Enter a short but descriptive title"
                onChange={(e) => onChange({ title: e.target.value, description })}
            />
```

`<uui-box>` is an Umbraco UI Library web component — it renders a standard backoffice panel with a headline. It is a web component tag (kebab-case, contains a hyphen), not a React component (which would be PascalCase). React does not know about these custom elements natively, which is why `src/uui-react.d.ts` provides the type declarations. See [../concepts/09-umbraco-backoffice-extensions.md](../concepts/09-umbraco-backoffice-extensions.md) for the `uui` library.

`className` is used instead of `class` because React follows the DOM property name (`element.className`) rather than the HTML attribute name (`class`). This is a React-specific quirk with no C# parallel.

The `onChange` handler in React's controlled input model fires on every keystroke and calls the prop callback. `e.target.value` is the new string content of the input. The handler reconstructs the full `SerpValue` object (copying the unchanged `description` from the closure variable) and passes it to `onChange`. This is a **controlled component** — React owns the displayed value, which always reflects the `value` prop. If you remove the `value={title}` binding, the input becomes uncontrolled and React will no longer synchronise it with the property data.

**CSS injection via `?inline`**

```tsx
import styles from './serp-editor-app.css?inline';
...
<style>{styles}</style>
```

The `?inline` suffix is a Vite feature that imports the CSS file's contents as a string rather than injecting it as a `<link>` tag. The string is then rendered as a `<style>` element inside the shadow root (via `<style>{styles}</style>` at the bottom of the JSX). This is necessary because shadow DOM isolates the component from the page's global CSS — a `<link>` tag in the main document would not penetrate the shadow boundary. Injecting a `<style>` element inside the shadow ensures the rules apply to the component's content. See [../concepts/06-web-components-and-shadow-dom.md](../concepts/06-web-components-and-shadow-dom.md) for shadow DOM isolation.

---

### `src/serp-editor-app.css`

```css
.sv { display: flex; gap: 40px; }
.sv-form { flex: 0 0 30%; }
.sv-demo { flex: 1 1 600px; max-width: 600px; }
.sv-form input, .sv-form textarea { width: 100%; box-sizing: border-box; }
.sv-form textarea { height: 100px; margin-top: 12px; }
.sv-form p.sv-error { color: var(--uui-color-danger, red); margin-top: 3px; }
.sv-demo h6, .sv-demo p { font-family: Arial, Helvetica, sans-serif; padding: 0; margin: 0; }
.sv-demo h6 { font-size: 20px; line-height: 1.3; margin-bottom: 3px; color: #1a0dab; text-decoration: underline; }
.sv-demo p { font-size: 14px; margin-bottom: 3px; line-height: 1.57; word-wrap: break-word; }
.sv-demo p.sv-url { color: #006621; }
```

The layout is a flex row: the form (`.sv-form`) takes a fixed 30% of the width; the preview (`.sv-demo`) takes the remaining space up to 600px.

The preview styling intentionally mimics Google's SERP appearance:
- `.sv-demo h6` — blue underlined title (`#1a0dab`), imitating Google's title link colour.
- `.sv-demo p.sv-url` — green URL text (`#006621`), imitating Google's displayed URL.
- `font-family: Arial` — Google's SERP uses Arial/Helvetica.

`var(--uui-color-danger, red)` is a CSS custom property (CSS variable). It reads the value of `--uui-color-danger` from the backoffice theme if available, falling back to `red` if not. This is how UUI exposes its design tokens — analogous to accessing a theme constant in a C# UI framework.

---

### `src/uui-react.d.ts`

```typescript
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

TypeScript's JSX type system requires that every element name used in JSX be declared in `JSX.IntrinsicElements`. For standard HTML elements (`div`, `input`, etc.) React's type definitions cover this. For custom web component tags (`uui-box` etc.) there are no React types — the Umbraco UI Library ships Lit-based types, not React JSX types.

This file **augments** the React module's type declarations (using TypeScript's **declaration merging** feature) to add the five `uui-*` tags used in this app, typed as `any` to avoid typing out the full Lit component API in React form. Think of it as adding extension methods to a third-party class in C# — you can't change the original type, but you can extend what the compiler sees.

The file header also states an important constraint: **only display-only elements are declared here**. Interactive `uui-*` controls (buttons, inputs, toggles) are intentionally omitted because they break when rendered by React in Umbraco 17. See the Gotchas section below and [../concepts/10-the-n3o-bridge-pattern.md](../concepts/10-the-n3o-bridge-pattern.md).

---

### `wwwroot/App_Plugins/N3O.Umbraco.SerpEditor/umbraco-package.json`

```json
{
    "$schema": "http://json.schemastore.org/umbraco-package.json",
    "id": "N3O.Umbraco.SerpEditor",
    "name": "N3O Serp Editor",
    "version": "17.0.0",
    "extensions": [
        {
            "type": "propertyEditorUi",
            "alias": "N3O.Umbraco.SerpEditor",
            "name": "N3O Serp Editor",
            "element": "/App_Plugins/N3O.Umbraco.SerpEditor/serp-editor.js",
            "meta": {
                "label": "N3O Serp Editor",
                "icon": "icon-search",
                "group": "common",
                "propertyEditorSchemaAlias": "N3O.Umbraco.SerpEditor"
            }
        }
    ]
}
```

This is the file Umbraco reads at startup to discover the extension. Key fields:

| Field | Value | Explanation |
|-------|-------|-------------|
| `id` | `"N3O.Umbraco.SerpEditor"` | Unique package identifier. Must be unique across all installed packages. |
| `type` | `"propertyEditorUi"` | The extension type. Tells Umbraco "this is a property editor UI". |
| `alias` | `"N3O.Umbraco.SerpEditor"` | **Must match** the `[DataEditor]` alias on the C# backend class. See the alias rule below. |
| `element` | `"/App_Plugins/.../serp-editor.js"` | Absolute URL path to the JavaScript file. Umbraco adds a `<script type="module">` import for this when the editor is needed. |
| `propertyEditorSchemaAlias` | `"N3O.Umbraco.SerpEditor"` | Reinforces the link to the backend schema. |
| `icon` | `"icon-search"` | Icon shown in the Umbraco data types list. |
| `group` | `"common"` | Groups this editor in the property editor picker. |

**The alias rule.** The `alias` in `umbraco-package.json` must equal the alias declared in the C# `[DataEditor(...)]` attribute on the corresponding backend class. Umbraco uses this alias as the key to look up which UI to render for a given data type. If the two aliases differ, Umbraco will fall back to a default text box for properties using that data type — the custom UI will silently not load. For a full explanation, see [../concepts/09-umbraco-backoffice-extensions.md](../concepts/09-umbraco-backoffice-extensions.md).

---

## Concepts demonstrated

| Concept | Where demonstrated |
|---------|--------------------|
| The N3O bridge pattern (shell + React) | The entire shell/app split | Link: [../concepts/10-the-n3o-bridge-pattern.md](../concepts/10-the-n3o-bridge-pattern.md) |
| Umbraco property editor UI extension | `umbraco-package.json` + `implements UmbPropertyEditorUiElement` | Link: [../concepts/09-umbraco-backoffice-extensions.md](../concepts/09-umbraco-backoffice-extensions.md) |
| Authenticated API calls from the backoffice | `UmbAuthFetchMixin` + `authFetch` prop | Link: [../concepts/10-the-n3o-bridge-pattern.md](../concepts/10-the-n3o-bridge-pattern.md) |
| Module-level cache for session-scoped data | `let cachedTitleSuffix` in `serp-editor-app.tsx` | Explained in this doc |
| React controlled components | `<input value={title} onChange={...} />` | Link: [../concepts/08-react.md](../concepts/08-react.md) |
| `useEffect` with a dependency array and cleanup | The `authFetch` effect in `SerpEditorApp` | Link: [../concepts/08-react.md](../concepts/08-react.md) |
| Shadow DOM + CSS `?inline` injection | `attachShadow` + `import styles from '...css?inline'` | Link: [../concepts/06-web-components-and-shadow-dom.md](../concepts/06-web-components-and-shadow-dom.md) |
| TypeScript declaration merging for JSX | `src/uui-react.d.ts` | Explained in this doc |
| Vite library mode + externals | `vite.config.ts` via `n3oPluginConfig` | Link: [../concepts/05-vite-and-the-build.md](../concepts/05-vite-and-the-build.md) |
| React mixin chain | `UmbAuthFetchMixin(UmbElementMixin(HTMLElement))` | Explained in this doc |

---

## Gotchas

**Do not use interactive `uui-*` controls inside React.**
`uui-input`, `uui-textarea`, `uui-button`, `uui-toggle`, `uui-select`, `uui-form-layout-item`, and `uui-button-group` fail to mount (they do not render and the console shows an error) when React renders them in Umbraco 17. This is documented in the memory note `uui + React FormControlMixin bug`. The root cause is a conflict between UUI's `FormControlMixin` (a Lit mixin that hooks into the browser form API) and React's DOM reconciliation. Use native HTML elements — `<input>`, `<textarea>`, `<button>`, `<select>` — inside React. `uui-box` and display-only elements (`uui-icon`, `uui-label`, `uui-loader`) work correctly. See [../concepts/10-the-n3o-bridge-pattern.md](../concepts/10-the-n3o-bridge-pattern.md).

**The module-level cache is never invalidated.**
`cachedTitleSuffix` is written once and never cleared. If the site's title suffix changes while a browser tab is open with the backoffice loaded, the editor will continue to show the old suffix until the user does a hard reload. For a value that requires freshness guarantees, use component-level `useState` instead and remove the module-level variable.

**`authFetch` starts as `null`.**
The first call to `#render()` from `connectedCallback` will pass `authFetch: null` to the React component. The `useEffect` guard `if (!authFetch) { return; }` handles this. Any code path that needs `authFetch` must check for `null` before using it; failure to do so will result in a runtime `TypeError: null is not a function`.

**The `alias` in `umbraco-package.json` is a string constant — it is not validated at compile time.**
If the alias does not match the C# `[DataEditor]` alias exactly (including casing), the editor silently falls back to the default text box. There is no build-time check; verify by opening a content item that uses the data type in the backoffice.

**`#render()` is called before `connectedCallback`.**
Both `set value` and `set config` call `#render()`. Because `#root` is `undefined` at that point, `this.#root?.render(...)` is a safe no-op. The value/config state is stored in `#value`, `#maxCharsTitle`, and `#maxCharsDescription`, ready for when `connectedCallback` creates the root and calls `#render()` for the first effective render.

**`onChange` in React fires on every keystroke, not on blur.**
React maps `onChange` on `<input>` elements to the DOM's `input` event, not `change`. Every keystroke raises `UmbPropertyValueChangeEvent` via the shell's callback. Umbraco is designed to handle this — it debounces saves — but do not be surprised if you see many events when tracing the flow.
