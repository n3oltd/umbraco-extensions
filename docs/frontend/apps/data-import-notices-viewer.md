# App — N3O.Umbraco.Data.ImportNoticesViewer

> **Concepts to read first:**
> [01 — The Big Picture](../concepts/01-the-big-picture.md) |
> [05 — Vite and the Build](../concepts/05-vite-and-the-build.md) |
> [09 — Umbraco Backoffice Extensions](../concepts/09-umbraco-backoffice-extensions.md) |
> [10 — The N3O Bridge Pattern](../concepts/10-the-n3o-bridge-pattern.md)

---

## 1. What it is

**ImportNoticesViewer** is a **read-only property editor UI** — a widget that appears inside a content property in the backoffice and displays the validation errors and warnings produced by a data import.

| Axis | Detail |
|------|--------|
| Extension type | `propertyEditorUi` |
| Custom element tag | `<n3o-import-notices-viewer>` |
| Source directory | `src/Data/N3O.Umbraco.Data.StaticAssets/Apps/N3O.Umbraco.Data.ImportNoticesViewer/` |
| Compiled output | `src/Data/N3O.Umbraco.Data.StaticAssets/wwwroot/App_Plugins/N3O.Umbraco.Data.ImportNoticesViewer/import-notices-viewer.js` |
| Manifest | `…/wwwroot/App_Plugins/N3O.Umbraco.Data.ImportNoticesViewer/umbraco-package.json` |
| Served via | ASP.NET Core static web assets (RCL pipeline) at `/App_Plugins/N3O.Umbraco.Data.ImportNoticesViewer/import-notices-viewer.js` |

### What it displays

When the Data module processes an import, it stores errors and warnings against the import record. This property editor reads those notices from the serialised property value (a list of error strings and a list of warning strings) and renders them in two sections — errors in red, warnings in amber. If there are neither, it renders "No warnings or errors". The component is entirely **display-only**; it never fires a change event.

### Comparison with ImportDataEditor — property editor UI varieties

Both this app and [ImportDataEditor](./data-import-data-editor.md) are `propertyEditorUi` extensions. The difference is in their purpose:

| Aspect | ImportDataEditor | ImportNoticesViewer |
|--------|-----------------|---------------------|
| Purpose | Edit the import data fields | Display import validation notices |
| Mutates the value? | Yes — fires `UmbChangeEvent` | No — read-only display |
| API calls | Yes — file upload (two endpoints) | No |
| Auth mixin needed? | Yes — `UmbAuthFetchMixin` | No |
| Base class | `UmbAuthFetchMixin(UmbElementMixin(HTMLElement))` | `HTMLElement` |
| React props | `value` + two callbacks | `value` only |

The read-only variant is simpler: no auth context, no change dispatch, no callbacks. The shell is a minimal wrapper that exists solely to adapt the Umbraco property-editor contract (the `value`/`config` get/set requirement) to a React render call.

---

## 2. Files

| File | Role |
|------|------|
| `package.json` | npm package manifest — declares app identity and workspace dependencies |
| `tsconfig.json` | TypeScript compiler config — extends the shared `@n3o/build/tsconfig` base |
| `vite.config.ts` | Vite build config — calls the shared `n3oPluginConfig` preset |
| `src/import-notices-viewer.ts` | Web-component shell — owns the Umbraco property-editor contract, mounts React |
| `src/import-notices-viewer-app.tsx` | React component — pure display; renders errors + warnings or placeholder |
| `src/import-notices-viewer-app.css` | Styles — injected into Shadow DOM via `?inline`; uses Umbraco design tokens |
| `src/uui-react.d.ts` | TypeScript declaration shim — lets React JSX reference `uui-*` / `umb-*` elements |
| `…/umbraco-package.json` | Umbraco manifest — registers the extension with the backoffice at startup |

---

## 3. End-to-end flow

```
dotnet build
  └── npm run build  (tsc --noEmit && vite build)
        └── Vite bundles import-notices-viewer.ts → import-notices-viewer.js
              into wwwroot/App_Plugins/N3O.Umbraco.Data.ImportNoticesViewer/

Browser boots Umbraco backoffice
  └── Scans App_Plugins/**/umbraco-package.json
        └── Finds type:"propertyEditorUi", alias:"N3O.Umbraco.Data.ImportNoticesViewer"
              └── Loads /App_Plugins/.../import-notices-viewer.js (ES module)
                    └── @customElement decorator registers <n3o-import-notices-viewer>

User opens a content node that has a property backed by this editor
  └── Umbraco sets element.value = { errors: [...], warnings: [...] }
        └── set value() stores value, calls #render()
              └── React.createElement(ImportNoticesViewerApp, { value })
                    └── createRoot(#mount).render(...)
                          └── React renders errors + warnings inside Shadow DOM

No user interaction updates the value — this is display only.
```

There is no authenticated fetch, no change dispatch, and no outbound API call. The data arrives in full via the `value` setter; the component's only job is to render it.

---

## 4. File-by-file

### `package.json`

```json
{
    "name": "n3o-umbraco-data-import-notices-viewer",
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

Structurally identical to `ImportDataEditor`'s `package.json`. Note that `@n3o/backoffice-core` is listed as a `devDependency` even though this app does not use `UmbAuthFetchMixin`. The dependency is present because the workspace resolves all `@n3o/backoffice-core` imports via its `exports` field. Having it listed does not cause it to be bundled — `vite.config.ts` also lists it as an external, so it is excluded from the compiled output regardless.

- `"build": "tsc --noEmit && vite build"` — TypeScript type-checks first, then Vite bundles. `&&` is the shell short-circuit: Vite only runs if TypeScript reports no errors.
- `"watch": "vite build --watch"` — for active development; rebuilds incrementally on file change.
- `"type": "module"` — all `.js` files in this package are treated as ES Modules.

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

Identical to `ImportDataEditor`'s `tsconfig.json`. See [ImportDataEditor § tsconfig.json](./data-import-data-editor.md#tsconfigjson) for a full explanation. Key points:

- `"extends": "@n3o/build/tsconfig"` — inherits `strict`, `experimentalDecorators`, `ES2022` target, and the `*.css?inline` module declaration from the shared base.
- `"jsx": "react-jsx"` — enables automatic JSX transform. TypeScript type-checks `.tsx` files; Vite/esbuild compiles the JSX at bundle time.

### `vite.config.ts`

```typescript
import { n3oPluginConfig } from '@n3o/build';

export default n3oPluginConfig({
    entries: {
        'N3O.Umbraco.Data.ImportNoticesViewer/import-notices-viewer': 'src/import-notices-viewer.ts',
    },
    outDir: '../../wwwroot/App_Plugins',
    react: true,
    additionalExternals: ['@n3o/backoffice-core'],
});
```

Identical structure to `ImportDataEditor`'s config; only the entry path and plugin name differ.

- `entries` — produces `wwwroot/App_Plugins/N3O.Umbraco.Data.ImportNoticesViewer/import-notices-viewer.js`. The key includes the subdirectory name so the output lands in the right plugin folder under the shared `App_Plugins/` root.
- `outDir: '../../wwwroot/App_Plugins'` — two directories up from `Apps/N3O.Umbraco.Data.ImportNoticesViewer/` reaches the `.csproj`'s `wwwroot/App_Plugins/`.
- `react: true` — marks `react`, `react-dom`, `react-dom/client`, `react/jsx-runtime` as **externals**. They are not bundled. The browser resolves them from the shared React runtime via the N3O import map at runtime.
- `additionalExternals: ['@n3o/backoffice-core']` — also external. This app does not call into `@n3o/backoffice-core`, but listing it as external costs nothing and keeps the pattern consistent.

### `src/import-notices-viewer.ts` — the web-component shell

```typescript
import { customElement } from '@umbraco-cms/backoffice/external/lit';
import {
    type UmbPropertyEditorConfigCollection,
    type UmbPropertyEditorUiElement,
} from '@umbraco-cms/backoffice/property-editor';
import { createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { ImportNoticesViewerApp, type ImportNoticesValue } from './import-notices-viewer-app';
```

Compared to `ImportDataEditor`, two imports are absent:
- No `UmbElementMixin` — not needed because `UmbAuthFetchMixin` is not used, and context consumption is not required.
- No `UmbAuthFetchMixin` — this component makes no authenticated API calls.
- No `UmbChangeEvent` — this component never changes the value.

The shell still imports `UmbPropertyEditorConfigCollection` and `UmbPropertyEditorUiElement` because Umbraco requires every property editor element to implement that interface, even read-only ones.

**Class declaration:**

```typescript
@customElement(elementName)
export class N3oImportNoticesViewerElement extends HTMLElement implements UmbPropertyEditorUiElement {
```

The base class is plain `HTMLElement` — no mixins. This is the minimal valid web component. The `@customElement(elementName)` decorator (from the Lit library, used here without LitElement) registers `<n3o-import-notices-viewer>` in the browser's custom element registry. See [06 — Web Components and Shadow DOM](../concepts/06-web-components-and-shadow-dom.md).

**Constructor:**

```typescript
constructor() {
    super();
    const shadow = this.attachShadow({ mode: 'open' });
    this.#mount = document.createElement('div');
    shadow.appendChild(this.#mount);
}
```

Same pattern as `ImportDataEditor`: a Shadow DOM is created and a `<div>` is attached as the React mount point. The `mode: 'open'` means external JavaScript can query the shadow root (useful for DevTools and testing); `'closed'` would hide it.

**`value` setter:**

```typescript
set value(v: ImportNoticesValue | undefined) {
    this.#value = v;
    this.#render();
}
```

Umbraco calls this with the deserialised property value — an object with `errors: string[]` and `warnings: string[]`. The shell stores it and re-renders React. There is no outbound data fetch; the value is complete as delivered.

**`config` getter/setter:**

```typescript
public set config(_config: UmbPropertyEditorConfigCollection | undefined) { }
public get config(): UmbPropertyEditorConfigCollection | undefined { return undefined; }
```

A no-op implementation. `UmbPropertyEditorUiElement` requires `config` to be settable (so the backoffice can push data-type configuration into the editor). This editor has no configuration options, so the setter is a deliberate empty body and the getter returns `undefined`. Note the leading underscore on the parameter name `_config` — TypeScript convention indicating "this parameter is intentionally unused".

**`connectedCallback` / `disconnectedCallback`:**

```typescript
connectedCallback(): void {
    this.#root ??= createRoot(this.#mount);
    this.#render();
}

disconnectedCallback(): void {
    this.#root?.unmount();
    this.#root = undefined;
}
```

- `??=` is the **nullish assignment** operator (C# 8+ has the equivalent `??=`). It assigns the right side only if the left side is `null` or `undefined`. The React root is created lazily on first mount and reused on subsequent re-connects.
- `disconnectedCallback` unmounts the React tree to run cleanup effects and release memory. Setting `this.#root = undefined` allows it to be re-created if the element is re-inserted.

Notably, `connectedCallback` does not call `super.connectedCallback?.()` — unlike `ImportDataEditor` which extends `UmbAuthFetchMixin` (which in turn extends `UmbElementMixin`, whose `connectedCallback` sets up context consumption). Here the base is plain `HTMLElement`, which has no `connectedCallback` to call.

**`#render()`:**

```typescript
#render(): void {
    this.#root?.render(
        createElement(ImportNoticesViewerApp, {
            value: this.#value,
        }),
    );
}
```

Simpler than `ImportDataEditor` — only the `value` prop is passed. No callbacks.

The `?.` operator is a **optional chaining** operator (C# 6+ has `?.` for the same purpose). If `#root` is `undefined` (element not yet mounted), the call is skipped silently.

### `src/import-notices-viewer-app.tsx` — the React component

```typescript
import styles from './import-notices-viewer-app.css?inline';
```

Same `?inline` pattern as `ImportDataEditor`. The CSS is imported as a raw string for Shadow DOM injection.

**Value type:**

```typescript
export interface ImportNoticesValue {
    errors: string[];
    warnings: string[];
}
```

A plain TypeScript interface — a compile-time type contract. `errors` and `warnings` are arrays of strings. This matches the shape of the data the C# Data module serialises into the property.

**The component function:**

```tsx
export function ImportNoticesViewerApp({ value }: ImportNoticesViewerAppProps) {
    const errors = value?.errors ?? null;
    const warnings = value?.warnings ?? null;

    const hasErrors = !!errors && errors.length > 0;
    const hasWarnings = !!warnings && warnings.length > 0;

    return (
        <div className="n3o-import-errors-viewer">
            {hasErrors ? (
                <>
                    <p><em className="text-error">Errors</em></p>
                    {errors!.map((error, index) => (
                        <div className="row-wrapper" key={`error-${error}-${index}`}>
                            <div className="row">{error}</div>
                        </div>
                    ))}
                </>
            ) : null}

            {hasWarnings ? (
                <>
                    <p><em className="text-warning">Warnings</em></p>
                    {warnings!.map((warning, index) => (
                        <div className="row-wrapper" key={`warning-${warning}-${index}`}>
                            <div className="row">{warning}</div>
                        </div>
                    ))}
                </>
            ) : null}

            {!hasErrors && !hasWarnings ? (
                <div className="row-wrapper">
                    <div className="row">No warnings or errors</div>
                </div>
            ) : null}

            <style>{styles}</style>
        </div>
    );
}
```

This is a stateless functional component. There is no `useState`, no `useEffect`, no `useRef` — it is a pure function from props to JSX. The same inputs always produce the same output.

Notable TypeScript idioms:

- `value?.errors ?? null` — optional chaining (`?.`) plus nullish coalescing (`??`). If `value` is `undefined`, `value?.errors` short-circuits to `undefined`; `?? null` then substitutes `null`. C# equivalent: `value?.Errors ?? null`.
- `!!errors` — double negation coerces to a boolean. `!!null` → `false`, `!![]` → `true`. This guards against the array being `null`.
- `errors!.map(...)` — the `!` is a **non-null assertion operator**. It tells TypeScript "I know this is not null here, even though the type says it might be". It is justified because the condition `hasErrors` already confirmed `errors` is non-null and non-empty. Used only as a type narrowing hint; it produces no runtime code.
- `<>...</>` — a **React Fragment** (short syntax for `<React.Fragment>`). Allows returning multiple sibling elements without adding an extra `<div>` wrapper to the DOM. C# analogy: returning multiple items from a LINQ expression without boxing them into a container.

**Key generation:** `key={\`error-${error}-${index}\`}` — combines the error text and its index. Using the index alone is a React anti-pattern when items can be reordered, but acceptable here since the array is never reordered and the items are display-only.

**Three conditional sections:**
1. Errors section — rendered only if `hasErrors`.
2. Warnings section — rendered only if `hasWarnings`.
3. "No warnings or errors" placeholder — rendered only if both are absent.

This is not mutually exclusive by design: it is valid to show both errors and warnings simultaneously.

### `src/import-notices-viewer-app.css`

```css
.n3o-import-errors-viewer .row-wrapper { margin-bottom: 40px; width: 100%; }
.n3o-import-errors-viewer .row         { display: block; width: 90%; }
.text-error   { color: var(--uui-color-danger); }
.text-warning { color: var(--uui-color-warning); }
```

Key detail: `.text-error` and `.text-warning` use Umbraco UI Library **CSS custom properties** (CSS variables — `var(--uui-color-danger)` etc.). These variables are defined by the Umbraco backoffice shell's global stylesheet and are available inside Shadow DOM because CSS custom properties pierce the shadow boundary (unlike ordinary inherited properties). This means the error colour automatically matches the backoffice's theme without hard-coding a hex value.

The class name `.n3o-import-errors-viewer` prefixes the structural selectors for scoping; `.text-error` and `.text-warning` are not prefixed because they are leaf classes applied directly to the `<em>` elements and are specific enough not to clash.

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

Identical to `ImportDataEditor`'s shim. Extends React's JSX type map with four common Umbraco/UUI web component tags. This file is not imported anywhere — TypeScript discovers `.d.ts` files in `"include": ["src"]` automatically. It adds no runtime output. See [ImportDataEditor § uui-react.d.ts](./data-import-data-editor.md#srcuui-reactdts) for full explanation.

### `umbraco-package.json` (manifest)

```json
{
    "$schema": "http://json.schemastore.org/umbraco-package.json",
    "id": "N3O.Umbraco.Data.ImportNoticesViewer",
    "name": "N3O Import Notices Viewer",
    "version": "17.0.0",
    "extensions": [
        {
            "type": "propertyEditorUi",
            "alias": "N3O.Umbraco.Data.ImportNoticesViewer",
            "name": "N3O Import Notices Viewer",
            "element": "/App_Plugins/N3O.Umbraco.Data.ImportNoticesViewer/import-notices-viewer.js",
            "meta": {
                "label": "N3O Import Notices Viewer",
                "icon": "icon-document",
                "group": "common",
                "propertyEditorSchemaAlias": "N3O.Umbraco.Data.ImportNoticesViewer"
            }
        }
    ]
}
```

Same structure as `ImportDataEditor`'s manifest; only the `id`, `name`, `alias`, `element`, and `propertyEditorSchemaAlias` differ.

| Field | Value | Meaning |
|-------|-------|---------|
| `id` | `"N3O.Umbraco.Data.ImportNoticesViewer"` | Unique package identifier |
| `extensions[].type` | `"propertyEditorUi"` | Registers this as a property editor widget |
| `extensions[].alias` | `"N3O.Umbraco.Data.ImportNoticesViewer"` | Must match the C# `[DataEditor]` alias exactly |
| `extensions[].element` | `/App_Plugins/…/import-notices-viewer.js` | Absolute URL path the backoffice loads |
| `meta.propertyEditorSchemaAlias` | `"N3O.Umbraco.Data.ImportNoticesViewer"` | Ties the UI to the backend value schema; must match C# |

Umbraco reads this file at startup (scanning all `App_Plugins/**/umbraco-package.json`). It registers the `<n3o-import-notices-viewer>` element against the alias `N3O.Umbraco.Data.ImportNoticesViewer`, so that wherever a content node has a property whose data type was configured with this alias, the backoffice knows which JS to load and which custom element to instantiate.

---

## 5. Concepts demonstrated

| Concept | Where in this app |
|---------|------------------|
| Bridge pattern (shell + React, read-only variant) | Shell owns Umbraco contract; React owns rendering; no callbacks |
| Minimal property editor UI contract | `value` get/set + no-op `config` + no `UmbChangeEvent` |
| No auth mixin needed for read-only | Base class is plain `HTMLElement`; no context consumption |
| Shadow DOM + `?inline` CSS | `attachShadow` in constructor; `<style>{styles}</style>` injected by React |
| CSS custom properties (design tokens) | `var(--uui-color-danger)` and `var(--uui-color-warning)` pierce shadow boundary |
| Stateless functional React component | `ImportNoticesViewerApp` has no `useState` or `useEffect` |
| React Fragments | `<>...</>` to group sibling elements without a wrapper `<div>` |
| Nullish coalescing and optional chaining | `value?.errors ?? null` |
| Non-null assertion | `errors!.map(...)` after `hasErrors` check |
| External modules (not bundled) | `react`, `@umbraco-cms/*`, `@n3o/backoffice-core` all external in `vite.config.ts` |

---

## 6. Gotchas

**Alias must match the C# `[DataEditor]` alias exactly.**
`"N3O.Umbraco.Data.ImportNoticesViewer"` must be identical to the alias in the C# `[DataEditor]` attribute and in `meta.propertyEditorSchemaAlias`. A mismatch is a silent failure — the data type exists in Umbraco but renders nothing. See [09 — Umbraco Backoffice Extensions](../concepts/09-umbraco-backoffice-extensions.md).

**No `super.connectedCallback?.()` — intentional.**
`HTMLElement` has no `connectedCallback` to call. If you refactor this to extend `UmbElementMixin`, remember to add `super.connectedCallback?.()`. Omitting it when extending a mixin that needs it will silently break context consumption.

**CSS custom properties work across shadow boundaries; regular styles do not.**
`var(--uui-color-danger)` works because CSS custom properties are inherited across shadow roots. If you want to use a regular Umbraco CSS class (e.g. `uui-text--danger`) directly in this component's markup, it will not be styled — the backoffice's global stylesheet does not penetrate the shadow boundary. Use `var(--uui-*)` tokens or copy the style into `import-notices-viewer-app.css`.

**The value arrives pre-populated — there is no fetch.**
Unlike a workspace view, this component never makes an API call. All data is delivered via the `value` setter. If the errors/warnings are absent or stale, the problem is in how the backend serialises the property value — not in this component.

**The compiled `.js` file is committed to git.**
`wwwroot/App_Plugins/N3O.Umbraco.Data.ImportNoticesViewer/import-notices-viewer.js` is checked in. Rebuild (`npm run build` or `dotnet build`) after any source change and commit both source and output together.

**`@n3o/backoffice-core` is listed as a dependency but not used.**
The `devDependency` entry and the `additionalExternals` entry both reference `@n3o/backoffice-core`. The runtime bundle does not include it (because it is external). There is no functional issue — listing an unused external dependency is harmless — but if you notice it during maintenance, it is safe to remove both entries from `package.json` and `vite.config.ts` without changing behaviour.
