# Scheduler Dashboard

**Source app directory:**
`src/Scheduler/N3O.Umbraco.Scheduler.StaticAssets/Apps/`

**Manifest (built output):**
`src/Scheduler/N3O.Umbraco.Scheduler.StaticAssets/wwwroot/App_Plugins/N3O.Umbraco.Scheduler/umbraco-package.json`

---

## 1. What it is

The Scheduler Dashboard is a **dashboard extension** in the Umbraco Settings section. Its entire visible content is a single `<iframe>` pointing to the [Hangfire](https://www.hangfire.io/) dashboard at `/umbraco/backoffice/hangfire/`. Hangfire is a .NET background job library; it ships its own self-contained web UI, which is embedded here rather than rewritten.

This plugin is structurally almost identical to the Welcome Dashboard — same web-component shell + React bridge pattern — but it demonstrates two additional techniques:

1. **Embedding an external web UI via `<iframe>`** inside a backoffice dashboard.
2. **Applying shadow-root CSS via `adoptedStyleSheets`** (the Constructable Stylesheets API) rather than injecting a `<style>` tag inside JSX.

C# analogy: the iframe approach is like embedding an existing MVC area or a third-party admin UI inside your application layout. Constructable Stylesheets are like pre-compiled CSS that the browser can apply without re-parsing.

| Attribute | Value |
|-----------|-------|
| Extension type | `dashboard` |
| Umbraco alias | `N3O.Dashboard.Scheduler` |
| Section it appears in | Settings section (`Umb.Section.Settings`) |
| Tab label | "Scheduler" |
| URL pathname within the section | `scheduler` |
| Custom element tag | `n3o-scheduler-dashboard` |
| Built output file | `App_Plugins/N3O.Umbraco.Scheduler/scheduler-dashboard.js` |

**How it is served:** The `N3O.Umbraco.Scheduler.StaticAssets` project is a Razor Class Library (RCL) whose `wwwroot/App_Plugins/` folder is published as static web assets.

---

## 2. Files

| File | Role |
|------|------|
| `package.json` | npm metadata; no additional dependencies beyond the workspace root |
| `tsconfig.json` | TypeScript config; extends shared base |
| `vite.config.ts` | Vite config via `n3oPluginConfig` |
| `src/scheduler-dashboard.ts` | **Web-component shell** — creates shadow root, applies CSS via `adoptedStyleSheets`, mounts React |
| `src/scheduler-dashboard-app.tsx` | **React component** — renders a single `<iframe>` |
| `src/scheduler-dashboard-app.css` | CSS for layout (`display: block`, iframe sizing); applied via Constructable Stylesheets in the shell |
| `wwwroot/App_Plugins/N3O.Umbraco.Scheduler/umbraco-package.json` | Umbraco extension manifest |
| `wwwroot/App_Plugins/N3O.Umbraco.Scheduler/scheduler-dashboard.js` | **Built output** (produced by Vite) |

There is no `uui-react.d.ts` in this app — the React component renders only a native `<iframe>`, which TypeScript already knows about. No UUI web components are used.

---

## 3. End-to-end flow

The bridge pattern is the same as the other apps — see [../concepts/10-the-n3o-bridge-pattern.md](../concepts/10-the-n3o-bridge-pattern.md). The styling approach is the distinguishing detail:

1. Umbraco reads `umbraco-package.json` at startup and registers the `dashboard` extension for the Settings section.
2. When the user opens the Settings section and clicks the Scheduler tab, Umbraco dynamically imports `scheduler-dashboard.js`.
3. The module registers `<n3o-scheduler-dashboard>`. Umbraco inserts the element into the DOM.
4. The element's `constructor` runs:
   - Creates a shadow root.
   - Builds a `CSSStyleSheet` object from the inlined CSS string, and assigns it to `shadow.adoptedStyleSheets`. This applies the layout CSS to the shadow root **without creating a DOM `<style>` node**.
   - Creates a `<div>` mount point inside the shadow root.
5. `connectedCallback` creates a React root and renders `SchedulerDashboardApp`.
6. React renders `<iframe src="/umbraco/backoffice/hangfire/" ...>`. The browser loads the Hangfire UI inside the iframe.
7. When the user navigates away, `disconnectedCallback` unmounts React. The shadow root (and its `adoptedStyleSheets`) is discarded with the element.

---

## 4. File-by-file

### `package.json`

```json
{
    "name": "n3o-umbraco-scheduler",
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

Structurally identical to the Welcome Dashboard. Note `"version": "17.0.0"` — this matches the Umbraco 17 major. No additional runtime dependencies: the scheduler UI is entirely provided by Hangfire (an external URL inside an iframe) and by the shared React runtime.

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

Identical to the Welcome Dashboard. The shared base at `src/N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/base.json` provides all necessary compiler settings. `"jsx": "react-jsx"` enables the modern JSX transform.

### `vite.config.ts`

```typescript
import { n3oPluginConfig } from '@n3o/build';

export default n3oPluginConfig({
    entries: { 'scheduler-dashboard': 'src/scheduler-dashboard.ts' },
    outDir: '../wwwroot/App_Plugins/N3O.Umbraco.Scheduler',
    react: true,
});
```

Identical in structure to the Welcome Dashboard. `react: true` marks `react`, `react-dom`, `react-dom/client`, and `react/jsx-runtime` as external (not bundled). All `@umbraco-cms/*` imports are also external. The only code bundled is the small amount from `scheduler-dashboard.ts` and `scheduler-dashboard-app.tsx`. The CSS is also inlined into the JS bundle via the `?inline` import.

### `src/scheduler-dashboard.ts` — the web-component shell

```typescript
import { customElement } from '@umbraco-cms/backoffice/external/lit';
import { createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { SchedulerDashboardApp } from './scheduler-dashboard-app';
import cssText from './scheduler-dashboard-app.css?inline';

const elementName = 'n3o-scheduler-dashboard';

@customElement(elementName)
export class N3oSchedulerDashboardElement extends HTMLElement {
    #root?: Root;
    #mount: HTMLDivElement;

    constructor() {
        super();
        const shadow = this.attachShadow({ mode: 'open' });
        const sheet = new CSSStyleSheet();
        sheet.replaceSync(cssText);
        shadow.adoptedStyleSheets = [sheet];
        this.#mount = document.createElement('div');
        shadow.appendChild(this.#mount);
    }

    connectedCallback(): void {
        this.#root ??= createRoot(this.#mount);
        this.#root.render(createElement(SchedulerDashboardApp));
    }

    disconnectedCallback(): void {
        this.#root?.unmount();
        this.#root = undefined;
    }
}
```

The structure is the same as `N3oWelcomeDashboardElement`. The key difference is in the constructor:

**`import cssText from './scheduler-dashboard-app.css?inline'`** — the `?inline` query parameter (a Vite feature) causes Vite to read the CSS file at build time and embed its content as a JavaScript string named `cssText`. At runtime, this string contains the raw CSS text.

**`const sheet = new CSSStyleSheet()`** — creates a Constructable Stylesheet. This is a browser API (no framework needed) for creating a stylesheet object programmatically. See [../concepts/06-web-components-and-shadow-dom.md](../concepts/06-web-components-and-shadow-dom.md) for shadow DOM context.

**`sheet.replaceSync(cssText)`** — parses the CSS string into the sheet synchronously. (`replaceSync` is the synchronous version; `replace` returns a Promise for async loading. `replaceSync` is appropriate when the CSS is already available as an inline string.)

**`shadow.adoptedStyleSheets = [sheet]`** — attaches the constructed stylesheet to the shadow root. This applies the CSS rules to everything inside the shadow root. The `adoptedStyleSheets` property accepts an array, so multiple sheets can be applied.

**Why `adoptedStyleSheets` instead of a `<style>` tag?**

Both approaches work. The difference:

| Approach | How | Advantage |
|----------|-----|-----------|
| `<style>{cssText}</style>` in JSX | React renders a DOM `<style>` node inside the shadow root | Simple; co-located with the component |
| `adoptedStyleSheets` | Constructable Stylesheet applied before React mounts | CSS is applied before the first React render; no DOM node; sheets can be shared across elements |

The Scheduler uses `adoptedStyleSheets` because the CSS must be applied to the shadow root's own dimensions (`:host` and `iframe`) — dimensions that exist independently of React. Applying it in the constructor (before React mounts) ensures the iframe sizing is correct from the moment the element is inserted into the DOM. If the CSS were injected via a `<style>` in JSX, there could be a brief unstyled flash before React's first render completes.

The Welcome Dashboard takes the simpler `<style>{styles}</style>` approach because it renders inside `<uui-box>`, which provides its own layout, and the CSS is only needed after React renders.

### `src/scheduler-dashboard-app.tsx` — the React component

```tsx
export function SchedulerDashboardApp() {
    return (
        <iframe
            name="hangfireIFrame"
            id="hangfire"
            title="Scheduler"
            frameBorder="0"
            scrolling="yes"
            src="/umbraco/backoffice/hangfire/"
            allowFullScreen
        />
    );
}
```

This is the most minimal React component in the codebase — no imports, no hooks, no state. It renders a single `<iframe>`.

- **`src="/umbraco/backoffice/hangfire/"`** — the URL of the Hangfire dashboard, served by the Hangfire ASP.NET middleware registered in `Startup.cs` / `Program.cs`. This is an absolute path relative to the application root.
- **`frameBorder="0"` / `scrolling="yes"`** — legacy iframe attributes. `frameBorder` has been superseded by CSS (`border: 0`), but it is included for compatibility. `scrolling="yes"` allows the iframe to scroll independently of the outer page.
- **`title="Scheduler"`** — required for accessibility (screen readers announce iframe titles).
- **`allowFullScreen`** — a React boolean attribute (equivalent to the HTML attribute `allowfullscreen`) that permits the iframe content to request fullscreen.

**Why an iframe?** Hangfire is a third-party library that ships a complete, self-contained web application serving its own HTML, CSS, and JavaScript. Rather than rewriting it as Umbraco backoffice components, the simplest integration is to embed its existing UI in an iframe. The Umbraco backoffice provides the chrome (navigation, header); the iframe provides the scheduler UI. This is a pragmatic trade-off: the Hangfire UI is fully functional inside the iframe, and no Hangfire frontend code needs to be maintained in this repo.

**Note on the source comment:** `scheduler-dashboard.ts` and `scheduler-dashboard-app.tsx` both note that the React shell is "overhead" for a component that only renders an iframe, and that a plain Lit element would be lighter. The React shell is kept for uniformity per migration decision, so all backoffice plugins follow the same pattern.

### `src/scheduler-dashboard-app.css`

```css
:host { display: block; width: 100%; }
iframe { display: block; width: 100%; height: calc(100dvh - 200px); min-height: 600px; border: 0; }
```

- **`:host`** — targets the `<n3o-scheduler-dashboard>` element itself. `display: block; width: 100%` makes it fill the dashboard area. Without `display: block`, custom elements are inline by default, and the content would not expand to fill the available space.
- **`iframe`** — the iframe inside the shadow root. `height: calc(100dvh - 200px)` sets the iframe height to the full dynamic viewport height minus 200px (accounting for the Umbraco header and the backoffice navigation chrome). `100dvh` uses the dynamic viewport height unit (handles mobile browser chrome that shows/hides). `min-height: 600px` prevents the iframe from becoming unusably small on short viewports. `border: 0` removes the default iframe border (the CSS equivalent of `frameBorder="0"`).
- This CSS is applied via `adoptedStyleSheets` in the shell's constructor, as described above. There is no `<style>` element in the React JSX.

---

## 5. The `umbraco-package.json` manifest explained

```json
{
    "$schema": "http://json.schemastore.org/umbraco-package.json",
    "id": "N3O.Umbraco.Scheduler",
    "name": "N3O Scheduler",
    "version": "17.0.0",
    "extensions": [
        {
            "type": "dashboard",
            "alias": "N3O.Dashboard.Scheduler",
            "name": "N3O Scheduler",
            "element": "/App_Plugins/N3O.Umbraco.Scheduler/scheduler-dashboard.js",
            "meta": {
                "label": "Scheduler",
                "pathname": "scheduler"
            },
            "conditions": [
                {
                    "alias": "Umb.Condition.SectionAlias",
                    "match": "Umb.Section.Settings"
                }
            ]
        }
    ]
}
```

This manifest is structurally identical to the Welcome Dashboard manifest. The meaningful differences:

| Field | Welcome Dashboard | Scheduler |
|-------|-------------------|-----------|
| `alias` | `N3O.Dashboard.Welcome` | `N3O.Dashboard.Scheduler` |
| `element` | `welcome-dashboard.js` | `scheduler-dashboard.js` |
| `meta.label` | `"Welcome"` | `"Scheduler"` |
| `meta.pathname` | `"welcome"` | `"scheduler"` |
| `conditions[0].match` | `"Umb.Section.Content"` | `"Umb.Section.Settings"` |
| `weight` | `-10` (explicit) | not set (default) |

The condition `Umb.Condition.SectionAlias` with `match: "Umb.Section.Settings"` means the Scheduler tab only appears in the Settings section of the backoffice. A user without access to the Settings section will never see or load this extension. There is no `weight` property set — the dashboard will appear at Umbraco's default sort position among Settings dashboards.

See [../concepts/09-umbraco-backoffice-extensions.md](../concepts/09-umbraco-backoffice-extensions.md) for the full dashboard extension specification.

---

## 6. Concepts demonstrated

| Concept | Where to learn more |
|---------|---------------------|
| Web-component shell and shadow DOM | [../concepts/06-web-components-and-shadow-dom.md](../concepts/06-web-components-and-shadow-dom.md) |
| Constructable Stylesheets (`adoptedStyleSheets`) | [../concepts/06-web-components-and-shadow-dom.md](../concepts/06-web-components-and-shadow-dom.md) |
| Vite `?inline` CSS import | [../concepts/05-vite-and-the-build.md](../concepts/05-vite-and-the-build.md) |
| React component (minimal JSX, no state) | [../concepts/08-react.md](../concepts/08-react.md) |
| Umbraco dashboard extension | [../concepts/09-umbraco-backoffice-extensions.md](../concepts/09-umbraco-backoffice-extensions.md) |
| N3O bridge pattern | [../concepts/10-the-n3o-bridge-pattern.md](../concepts/10-the-n3o-bridge-pattern.md) |
| External React runtime and import maps | [../concepts/04-es-modules-and-import-maps.md](../concepts/04-es-modules-and-import-maps.md) |

---

## 7. Gotchas

**The iframe URL must be accessible when the dashboard loads.** `/umbraco/backoffice/hangfire/` is served by the Hangfire ASP.NET middleware, which must be registered and running. If Hangfire is not configured, the iframe will show a 404. This is a server-side concern, not a frontend one, but it is the most common reason the dashboard appears blank.

**`adoptedStyleSheets` is applied in the constructor — before React mounts.** This is intentional: the iframe sizing CSS (`:host { display: block; }` and the `height: calc(100dvh - 200px)` rule) needs to be in effect from the moment the element enters the DOM. If you move the `adoptedStyleSheets` assignment to `connectedCallback`, there may be a brief frame where the element has no height, causing the iframe to collapse.

**The `CSSStyleSheet.replaceSync()` method does not return the sheet.** It modifies the sheet in place and returns `undefined`. The call pattern `sheet.replaceSync(cssText)` is correct; `const sheet = new CSSStyleSheet().replaceSync(cssText)` would store `undefined`. This is a common mistake when chaining.

**`adoptedStyleSheets` requires an array assignment.** You cannot push to `adoptedStyleSheets` directly (`shadow.adoptedStyleSheets.push(sheet)` does nothing in some environments). Always assign a new array: `shadow.adoptedStyleSheets = [sheet]` or `shadow.adoptedStyleSheets = [...shadow.adoptedStyleSheets, sheet]`.

**The iframe is sandboxed by the browser's same-origin policy.** Because the iframe `src` is on the same origin (same host and port as Umbraco), the Hangfire UI can make API calls and set cookies normally. If the Hangfire URL were on a different origin (e.g., a separate port), cross-origin restrictions would apply.

**There is no `uui-react.d.ts` in this app.** The React component uses only a native `<iframe>` element, which TypeScript knows about through the standard DOM type library. UUI web components are not used. If you add a `<uui-box>` or other UUI element to this app in the future, you must add `uui-react.d.ts` (copying the pattern from WelcomeDashboard) and declare the new tags.

**The React shell is acknowledged as over-engineering.** Both source files carry a comment noting that a plain Lit element would be sufficient for a component that only wraps an iframe. The React shell is kept for uniformity. If you are building a new dashboard that is equally simple, you may follow this pattern or write a Lit-only element — the manifest format is the same either way.
