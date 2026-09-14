# Welcome Dashboard

**Source app directory:**
`src/Plugins/WelcomeDashboard/N3O.Umbraco.WelcomeDashboard.StaticAssets/Apps/`

**Manifest (built output):**
`src/Plugins/WelcomeDashboard/N3O.Umbraco.WelcomeDashboard.StaticAssets/wwwroot/App_Plugins/N3O.Umbraco.WelcomeDashboard/umbraco-package.json`

---

## 1. What it is

The Welcome Dashboard is the **simplest possible React plugin** in the N3O backoffice and is the best starting point for understanding the pattern. It is also explicitly acknowledged in the source as being over-engineered for its content — a near-static help panel with a link to the N3O Support Centre — but it follows the standard React-shell pattern for consistency.

| Attribute | Value |
|-----------|-------|
| Extension type | `dashboard` |
| Umbraco alias | `N3O.Dashboard.Welcome` |
| Section it appears in | Content section (`Umb.Section.Content`) |
| Tab label | "Welcome" |
| URL pathname within the section | `welcome` |
| Custom element tag | `n3o-welcome-dashboard` |
| Built output file | `App_Plugins/N3O.Umbraco.WelcomeDashboard/welcome-dashboard.js` |

**How it is served:** The `N3O.Umbraco.WelcomeDashboard.StaticAssets` project is a Razor Class Library (RCL). The `wwwroot/App_Plugins/` folder inside it is published as static web assets. Umbraco discovers `umbraco-package.json` from any `App_Plugins/*/` folder and registers the extensions it declares. See [../concepts/09-umbraco-backoffice-extensions.md](../concepts/09-umbraco-backoffice-extensions.md) for the discovery mechanism.

---

## 2. Files

| File | Role |
|------|------|
| `package.json` | npm package metadata; declares the `@n3o/build` dev-dependency and build scripts |
| `tsconfig.json` | TypeScript compiler config; extends the shared `@n3o/build/tsconfig` base |
| `vite.config.ts` | Vite build config; calls `n3oPluginConfig` from `@n3o/build` |
| `src/welcome-dashboard.ts` | **Web-component shell** — the custom element Umbraco instantiates |
| `src/welcome-dashboard-app.tsx` | **React component** — the actual UI rendered inside the shell |
| `src/welcome-dashboard-app.css` | CSS scoped to the component; injected as a `<style>` tag inside JSX |
| `src/uui-react.d.ts` | TypeScript ambient declarations so `uui-*` tags compile inside TSX |
| `wwwroot/App_Plugins/N3O.Umbraco.WelcomeDashboard/umbraco-package.json` | Umbraco extension manifest |
| `wwwroot/App_Plugins/N3O.Umbraco.WelcomeDashboard/welcome-dashboard.js` | **Built output** (not in source control — produced by Vite) |

---

## 3. End-to-end flow

The pattern used here — web-component shell plus React subtree — is called the **N3O bridge pattern**. It is described in detail in [../concepts/10-the-n3o-bridge-pattern.md](../concepts/10-the-n3o-bridge-pattern.md). The summary for this app:

1. Umbraco reads `umbraco-package.json` at startup and registers the `dashboard` extension. The `element` field points to the built JS file.
2. When the user navigates to the Content section and clicks the Welcome tab, Umbraco dynamically imports `welcome-dashboard.js` via its import map. This executes the module, which calls `customElements.define('n3o-welcome-dashboard', N3oWelcomeDashboardElement)` (via the `@customElement` decorator).
3. Umbraco inserts `<n3o-welcome-dashboard>` into the DOM.
4. The element's `constructor` creates a shadow root and a `<div>` mount point inside it.
5. `connectedCallback` runs: `createRoot(this.#mount)` creates a React root; `.render(createElement(WelcomeDashboardApp))` renders the React component tree into the shadow DOM.
6. The React component returns JSX containing a `<uui-box>` (a UUI web component) with static markup.
7. When the user navigates away, Umbraco removes the element from the DOM; `disconnectedCallback` calls `this.#root.unmount()`, which tears down the React tree and cleans up.

React itself is **not bundled** in `welcome-dashboard.js`. It is resolved at runtime from the shared ReactRuntime — see [../apps/reactruntime.md](../apps/reactruntime.md) and [../concepts/04-es-modules-and-import-maps.md](../concepts/04-es-modules-and-import-maps.md).

---

## 4. File-by-file

### `package.json`

```json
{
    "name": "n3o-umbraco-welcomedashboard",
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

- `"private": true` — this package is never published to any npm registry. C# analogy: think of it as an internal project reference.
- `"type": "module"` — every `.js` file in this package is treated as an ES module (uses `import`/`export` instead of `require()`). See [../concepts/04-es-modules-and-import-maps.md](../concepts/04-es-modules-and-import-maps.md).
- `"scripts"` — these are shortcuts invoked with `npm run build` or `npm run watch`. The build script runs the TypeScript type-checker first (`tsc --noEmit` — checks types, emits nothing), then Vite for the actual compilation. C# analogy: like running `dotnet build` which invokes Roslyn and then MSBuild tasks.
- `"@n3o/build": "*"` — resolved from the npm workspace (the `*` means "whatever version is in the workspace"). `@n3o/build` is the shared Vite configuration package located at `src/N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/`. No `react` or `@umbraco-cms/backoffice` dependencies are listed here because those are **workspace-root dev-dependencies** — all child packages share them.

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

- `"extends": "@n3o/build/tsconfig"` — inherits the shared base at `src/N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/base.json`. That base sets `"target": "ES2022"`, enables `experimentalDecorators` (needed for `@customElement`), uses `"moduleResolution": "bundler"` (lets TypeScript understand how Vite resolves imports), and references the Umbraco backoffice extension types.
- `"jsx": "react-jsx"` — tells TypeScript to transform JSX using the modern automatic React runtime (`react/jsx-runtime`) rather than `React.createElement`. This means you do not need `import React from 'react'` in every TSX file.
- `"include": ["src"]` — only type-check the `src/` subdirectory, not `node_modules` or config files.

See [../concepts/02-javascript-typescript-for-csharp-devs.md](../concepts/02-javascript-typescript-for-csharp-devs.md) for a TypeScript primer.

### `vite.config.ts`

```typescript
import { n3oPluginConfig } from '@n3o/build';

export default n3oPluginConfig({
    entries: { 'welcome-dashboard': 'src/welcome-dashboard.ts' },
    outDir: '../wwwroot/App_Plugins/N3O.Umbraco.WelcomeDashboard',
    react: true,
});
```

`n3oPluginConfig` is a factory function defined in `src/N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/vite-config.js`. It returns a Vite `defineConfig` object. Reading that file shows what `react: true` does:

```javascript
// from vite-config.js
const external = [/^@umbraco/];
if (react) {
    external.push('react', 'react-dom', 'react-dom/client', 'react/jsx-runtime');
}
// ...
return defineConfig({
    esbuild: { jsx: 'automatic' },
    build: {
        lib: { entry: entries, formats: ['es'] },
        outDir,
        rollupOptions: {
            external,
            output: { entryFileNames: '[name].js' },
        },
    },
});
```

Key points:
- **Library mode** (`formats: ['es']`) — Vite/Rollup produces a single ES module file rather than an HTML page with a script tag. This is the right mode for a plugin that will be loaded by another application (Umbraco). C# analogy: building a `.dll` rather than an `.exe`.
- **Externals** — `react`, `react-dom`, and all `@umbraco-cms/*` packages are declared external. Rollup will not bundle their code; it will leave the `import` statements in the output. At runtime in the browser, Umbraco's import map redirects those bare imports to the correct URLs. This is why `welcome-dashboard.js` is small.
- **`entries`** — maps output filename `welcome-dashboard` to the source entry point. Vite emits `welcome-dashboard.js` in `outDir`.
- **`outDir: '../wwwroot/...'`** — output goes directly into the `wwwroot` folder that the RCL serves as static assets.

See [../concepts/05-vite-and-the-build.md](../concepts/05-vite-and-the-build.md).

### `src/welcome-dashboard.ts` — the web-component shell

This is the most important file to understand. The full source:

```typescript
import { customElement } from '@umbraco-cms/backoffice/external/lit';
import { createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { WelcomeDashboardApp } from './welcome-dashboard-app';

const elementName = 'n3o-welcome-dashboard';

@customElement(elementName)
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

Line by line:

- **`import { customElement } from '@umbraco-cms/backoffice/external/lit'`** — imports the `@customElement` decorator. It is sourced from Umbraco's re-export of Lit, which is available as an external at runtime. This decorator registers the class with the browser's Custom Elements registry when the module is executed. C# analogy: like an `[AttributeUsage]` attribute that also runs a side-effect registration.
- **`extends HTMLElement`** — the class extends the native `HTMLElement` base class directly. This is called an "autonomous custom element." It is not a Lit element (which would extend `LitElement`) — it is the minimal raw browser API. See [../concepts/06-web-components-and-shadow-dom.md](../concepts/06-web-components-and-shadow-dom.md).
- **`#root` and `#mount`** — the `#` prefix is JavaScript private class fields (not TypeScript's `private` keyword). These cannot be accessed from outside the class at all, even by TypeScript tricks.
- **`this.attachShadow({ mode: 'open' })`** — creates a shadow DOM for this element. The shadow root is an isolated subtree: CSS from outside does not penetrate it; CSS inside does not leak out. The React app lives entirely inside this shadow root. `mode: 'open'` means JavaScript outside the element can still read `element.shadowRoot` (useful for debugging).
- **`createRoot(this.#mount)`** — this is React 18+/19's API for creating a concurrent React root. Think of it as React's equivalent of setting up a dependency injection container for the component tree.
- **`connectedCallback`** / **`disconnectedCallback`** — these are native Custom Elements lifecycle callbacks, equivalent to C# `IDisposable`. `connectedCallback` fires when the element is added to a document; `disconnectedCallback` fires when it is removed.
- **`this.#root ??= createRoot(...)`** — the `??=` operator only assigns if the left side is `null` or `undefined`. This prevents recreating the React root on repeated connect/disconnect cycles (e.g., if Umbraco temporarily removes and re-adds the element).

The `declare global { interface HTMLElementTagNameMap { ... } }` block at the bottom is a TypeScript declaration merge. It teaches the TypeScript compiler that `document.querySelector('n3o-welcome-dashboard')` returns `N3oWelcomeDashboardElement`. It has no runtime effect.

### `src/welcome-dashboard-app.tsx` — the React component

```tsx
import styles from './welcome-dashboard-app.css?inline';

export function WelcomeDashboardApp() {
    return (
        <uui-box headline="Help & Support">
            <p>...</p>
            <p><a href="https://support.n3o.ltd" ...>Visit Support Centre</a></p>
            <style>{styles}</style>
        </uui-box>
    );
}
```

- **`import styles from './welcome-dashboard-app.css?inline'`** — the `?inline` suffix is a Vite-specific query parameter. Instead of emitting a separate `.css` file and injecting a `<link>` tag, Vite inlines the CSS content as a JavaScript string. This string is then inserted as a `<style>` element inside the JSX (see `<style>{styles}</style>`). This is the correct approach for shadow DOM: a regular `<link rel="stylesheet">` or a globally-injected stylesheet would not reach inside the shadow root.
- **`<uui-box>`** — this is a custom element from Umbraco's UI Library (UUI). Because it is a web component (not a React component), React renders it as a generic HTML element. The `uui-react.d.ts` file tells TypeScript that `<uui-box>` is a valid JSX element. `headline` is an attribute on the `uui-box` custom element.
- **No props, no state, no hooks** — this component is pure static markup. It is the simplest possible React component.

See [../concepts/08-react.md](../concepts/08-react.md) for React fundamentals.

### `src/welcome-dashboard-app.css`

```css
:host { display: block; }
p { margin: 0 0 var(--uui-size-space-4); }
p:last-of-type { margin-bottom: 0; }
a { color: var(--uui-color-interactive); }
```

- **`:host`** — a CSS pseudo-class that targets the custom element itself (the `<n3o-welcome-dashboard>` tag). This only works inside shadow DOM stylesheets. `display: block` is needed because custom elements are inline by default.
- **`var(--uui-size-space-4)` / `var(--uui-color-interactive)`** — CSS custom properties (variables) defined by the UUI design system. They pierce shadow boundaries because CSS custom properties are inheritable. This is how the dashboard picks up the backoffice's spacing and colour tokens.
- The CSS is bundled into the JS module via the `?inline` import and injected at runtime as shown above.

### `src/uui-react.d.ts`

```typescript
import 'react';

declare module 'react' {
    namespace JSX {
        interface IntrinsicElements {
            'uui-box': any;
        }
    }
}
```

TypeScript knows about standard HTML elements (`<div>`, `<p>`, etc.) from its DOM lib. It does not know about custom elements. This file extends React's `JSX.IntrinsicElements` interface (a module augmentation — C# analogy: like adding extension methods to an existing type) to declare that `<uui-box>` is a valid JSX element. The type is `any` because UUI ships Lit-native types, not React JSX types. Each app's `uui-react.d.ts` lists only the UUI tags that app actually uses.

---

## 5. The `umbraco-package.json` manifest explained

```json
{
    "$schema": "http://json.schemastore.org/umbraco-package.json",
    "id": "N3O.Umbraco.WelcomeDashboard",
    "name": "N3O Welcome Dashboard",
    "version": "17.0.0",
    "extensions": [
        {
            "type": "dashboard",
            "alias": "N3O.Dashboard.Welcome",
            "name": "N3O Welcome Dashboard",
            "element": "/App_Plugins/N3O.Umbraco.WelcomeDashboard/welcome-dashboard.js",
            "weight": -10,
            "meta": {
                "label": "Welcome",
                "pathname": "welcome"
            },
            "conditions": [
                {
                    "alias": "Umb.Condition.SectionAlias",
                    "match": "Umb.Section.Content"
                }
            ]
        }
    ]
}
```

| Field | Meaning |
|-------|---------|
| `$schema` | JSON schema URL — enables IDE autocomplete and validation |
| `id` | Package identifier, unique across all installed packages |
| `name` | Human-readable package name |
| `version` | Package version (CalVer — matches the Umbraco major) |
| `extensions[].type` | **`"dashboard"`** — tells Umbraco this extension is a tab on a section's dashboard area |
| `extensions[].alias` | A dot-separated unique string identifying this particular extension. Umbraco uses this for deduplication and targeted overriding |
| `extensions[].element` | Absolute URL path to the JS file to load when this extension is activated. Umbraco dynamically imports this file |
| `extensions[].weight` | Sort order among dashboards. `-10` puts this tab before others (lower weight = appears first) |
| `meta.label` | The tab label shown in the UI ("Welcome") |
| `meta.pathname` | The URL slug for this dashboard within the section (`/umbraco/section/content/welcome`) |
| `conditions[]` | An array of conditions that must all be true for the extension to be shown. `Umb.Condition.SectionAlias` with `match: "Umb.Section.Content"` means "only show in the Content section" |

C# analogy: `umbraco-package.json` is like an Umbraco `IComposer` registration — it tells the framework about a new component. The `alias` is like a service registration key, and `conditions` is like a guard condition on a route.

---

## 6. Concepts demonstrated

| Concept | Where to learn more |
|---------|---------------------|
| Web-component shell (custom element) | [../concepts/06-web-components-and-shadow-dom.md](../concepts/06-web-components-and-shadow-dom.md) |
| Shadow DOM and style isolation | [../concepts/06-web-components-and-shadow-dom.md](../concepts/06-web-components-and-shadow-dom.md) |
| `@customElement` decorator | [../concepts/07-lit.md](../concepts/07-lit.md) |
| React component and JSX | [../concepts/08-react.md](../concepts/08-react.md) |
| External React runtime via import map | [../concepts/04-es-modules-and-import-maps.md](../concepts/04-es-modules-and-import-maps.md) |
| Vite library mode and `?inline` imports | [../concepts/05-vite-and-the-build.md](../concepts/05-vite-and-the-build.md) |
| Umbraco extension registration | [../concepts/09-umbraco-backoffice-extensions.md](../concepts/09-umbraco-backoffice-extensions.md) |
| N3O bridge pattern (shell + React) | [../concepts/10-the-n3o-bridge-pattern.md](../concepts/10-the-n3o-bridge-pattern.md) |

---

## 7. Gotchas

**CSS does not auto-pierce shadow DOM.** A stylesheet linked from the main page does not apply inside the shadow root. Every shadow-rooted element needs its own styles. Here, the CSS is inlined via `?inline` and injected as a `<style>` tag inside JSX. If you forget the `<style>{styles}</style>` line, the component renders unstyled.

**UUI design tokens do pierce shadow DOM.** CSS custom properties (`--uui-*`) are inherited, so they flow into the shadow root without any special handling. This is the intended mechanism for design system theming.

**The `@customElement` decorator registers the element globally.** Once the JS module has been imported, the element name is permanently registered with `customElements.define`. Registering the same name twice throws a `DOMException`. Umbraco's extension loader prevents double-importing, but if you test outside Umbraco, guard with `if (!customElements.get(elementName))`.

**React `connectedCallback`/`disconnectedCallback` are not React lifecycle hooks.** They are native browser APIs on `HTMLElement`. Do not confuse them with React's `useEffect` cleanup — those are separate mechanisms. Here, `disconnectedCallback` calls `this.#root.unmount()`, which triggers React's `useEffect` cleanups internally.

**JSX is transpiled to `react/jsx-runtime` calls — not `React.createElement`.** With `"jsx": "react-jsx"`, the TypeScript/Vite transform replaces `<Foo />` with `_jsx(Foo, {})` from `react/jsx-runtime`. That import is also marked external, so it resolves from the shared runtime at runtime. You do not need `import React from 'react'` in TSX files, but you do need the runtime available via the import map.

**This app acknowledges it is over-engineered.** The source comment in both `welcome-dashboard.ts` and `welcome-dashboard-app.tsx` notes that a plain Lit element would be lighter for a static panel. The React shell is kept for uniformity across all plugins.
