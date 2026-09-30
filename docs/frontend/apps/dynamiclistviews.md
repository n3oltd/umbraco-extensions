# N3O.Umbraco.DynamicListViews

**Source (frontend):** `src/N3O.Umbraco.Cms/Apps/N3O.Umbraco.DynamicListViews/`
**Output:** `src/N3O.Umbraco.Cms/wwwroot/App_Plugins/N3O.Umbraco.DynamicListViews/`
**Manifest:** `src/N3O.Umbraco.Cms/wwwroot/App_Plugins/N3O.Umbraco.DynamicListViews/umbraco-package.json`
**C# backend:** `src/N3O.Umbraco.Extensions/Features/DynamicListViews/`
**Framework:** Pure Lit — no React.

---

## What it is

DynamicListViews adds a **"Children" tab** to any document workspace in the Umbraco backoffice. When an editor opens a content node, a tab labelled "Children" appears alongside the default "Content", "Info", and "SEO" tabs. That tab renders a table of the node's immediate child documents — their name, publish status, and creation date — each row linking directly to the child's own edit page.

This plugin is **pure Lit**: the single custom element (`<n3o-dynamic-list-view>`) extends `LitElement` directly, with no React involved. It is a deliberate contrast to the React-bridge apps. Compare it to those (such as `blocks` or `data-export`) to see the difference between pure Lit and the bridge pattern described in [../concepts/10-the-n3o-bridge-pattern.md](../concepts/10-the-n3o-bridge-pattern.md).

The plugin demonstrates two Umbraco-specific patterns:

1. **WorkspaceView extension** — adding a tab to the document editing UI.
2. **Repository pattern** — a dedicated class (`DynamicChildrenRepository`) that wraps Umbraco's own tree-fetch API and maps the raw response to a simpler local model before the component sees it.

The "Children" tab is not shown on every document — it is gated by the shared `N3O.Condition.WorkspaceVisibility` condition provided by [BackofficeCore](backofficecore.md). That condition makes an HTTP request to the C# backend at startup to determine whether the tab should be visible for the current node. This replaced the app's own bespoke condition during the v17 migration.

---

## Files

| File | Role |
|------|------|
| `package.json` | npm package manifest; declares the build scripts and dev dependency on `@n3o/build` |
| `tsconfig.json` | TypeScript config; inherits shared compiler settings from `@n3o/build/tsconfig` |
| `vite.config.ts` | Vite build config; uses the shared `n3oPluginConfig` preset; no React flag |
| `src/dynamic-list-view.ts` | The Lit custom element (`<n3o-dynamic-list-view>`): renders the table, manages loading state, subscribes to document context |
| `src/dynamic-children.repository.ts` | Repository class: wraps `UmbDocumentTreeRepository`, paginates, and maps the result to a simpler local model |
| `wwwroot/.../umbraco-package.json` | Umbraco manifest: registers the workspaceView extension and its two conditions |

---

## End-to-end flow

```
dotnet build
  └─ MSBuild runs: npm run build  (in Apps/N3O.Umbraco.DynamicListViews/)
       └─ tsc --noEmit   (TypeScript type check — fails fast on errors)
       └─ vite build     (bundles dynamic-list-view.ts → dynamic-list-view.js)
                          src/dynamic-list-view.ts
                          src/dynamic-children.repository.ts
                          (imported by dynamic-list-view.ts)
                         All @umbraco-cms/* imports → kept EXTERNAL (not bundled)
                         Output → wwwroot/App_Plugins/N3O.Umbraco.DynamicListViews/
                                    dynamic-list-view.js
                                    dynamic-list-view.js.map

Umbraco starts
  └─ Scans App_Plugins/**/umbraco-package.json
  └─ Reads N3O.Umbraco.DynamicListViews/umbraco-package.json
  └─ Registers one workspaceView extension with two conditions

Browser opens a document workspace
  └─ Umbraco evaluates conditions for each registered workspaceView:
       Condition 1: "Umb.Condition.WorkspaceAlias" = "Umb.Workspace.Document"
                    → always true for the document editor
       Condition 2: "N3O.Condition.WorkspaceVisibility" (from BackofficeCore)
                    → calls GET /umbraco/backoffice/api/DynamicListViewApi
                    → C# controller returns { visible: true/false }
                    → if false, the "Children" tab is never rendered
  └─ If both conditions pass: Umbraco loads dynamic-list-view.js as an ES module
       → custom element <n3o-dynamic-list-view> is registered with the browser
       → Umbraco inserts <n3o-dynamic-list-view> into the workspace tab panel
       → element constructor runs, subscribes to the document workspace context
       → observes context.unique (the document's GUID key)
       → calls DynamicChildrenRepository.getChildren(unique)
       → repository calls UmbDocumentTreeRepository.requestTreeItemsOf(...)
       → maps results → assigns to @state() fields → Lit re-renders → table appears
```

---

## File-by-file walkthrough

### `package.json`

```json
{
    "name": "n3o-umbraco-dynamiclistviews",
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

- **`"name": "n3o-umbraco-dynamiclistviews"`** — the package name used by the npm workspace to identify this app. Not published externally; `"private": true` prevents accidental publication.
- **`"type": "module"`** — all `.js` files in this package use ES module syntax (`import`/`export`). Required because both `vite.config.ts` and the TypeScript sources use `import` statements.
- **`"build": "tsc --noEmit && vite build"`** — the two-phase build. `tsc --noEmit` runs the TypeScript compiler for type checking only (it produces no output files — think of it as `dotnet build` with errors suppressed but warnings treated as errors). `&&` means Vite only runs if TypeScript passes. `vite build` then bundles the output.
- **`"watch": "vite build --watch"`** — for development only; rebuilds on every file save. Not used in `dotnet build`.
- **`"devDependencies": { "@n3o/build": "*" }`** — the only declared dependency. `"*"` means "resolve from the workspace root" (npm workspaces treat `*` as "link to whatever the workspace root provides"). `@n3o/build` provides the shared Vite preset and the shared `tsconfig` base. All other packages (`@umbraco-cms/backoffice`, `typescript`, `vite`) are inherited from the workspace root `src/package.json`.

There are **no `dependencies`** (only `devDependencies`) because the built output keeps all Umbraco imports external — the bundle does not embed any external library code.

---

### `tsconfig.json`

```json
{
    "extends": "@n3o/build/tsconfig",
    "include": ["src"]
}
```

Inherits the shared TypeScript compiler config from `@n3o/build` (see [buildconfig.md](buildconfig.md) and [../concepts/05-vite-and-the-build.md](../concepts/05-vite-and-the-build.md)). This sets `"strict": true`, `"target": "ES2022"`, decorators (`experimentalDecorators: true`, `useDefineForClassFields: false` — required for Lit's `@state()` and `@customElement()` decorators), and the `?inline` CSS ambient type. The `"include": ["src"]` narrows type checking to only the `src/` subfolder — config files like `vite.config.ts` are excluded because the Vite config is type-checked through `@n3o/build`'s own tsconfig.

---

### `vite.config.ts`

```typescript
import { n3oPluginConfig } from '@n3o/build';

export default n3oPluginConfig({
    entries: {
        'N3O.Umbraco.DynamicListViews/dynamic-list-view': 'src/dynamic-list-view.ts',
    },
    outDir: '../../wwwroot/App_Plugins',
});
```

- **`n3oPluginConfig`** — the shared Vite preset from `@n3o/build`. See [../concepts/05-vite-and-the-build.md](../concepts/05-vite-and-the-build.md) for a full explanation of what it sets. Abbreviated summary: library mode, ESM output, `@umbraco-cms/*` kept external, no content hash in filenames, sourcemaps enabled, `emptyOutDir: false`.
- **`entries`** — one entry point: `src/dynamic-list-view.ts`. The key `'N3O.Umbraco.DynamicListViews/dynamic-list-view'` becomes the output path **relative to `outDir`**, producing `wwwroot/App_Plugins/N3O.Umbraco.DynamicListViews/dynamic-list-view.js`. Note that `dynamic-children.repository.ts` is **not** listed here — it does not need its own entry because it is `import`ed by `dynamic-list-view.ts` and Vite follows imports automatically when bundling.
- **`outDir: '../../wwwroot/App_Plugins'`** — two directory levels up from `Apps/N3O.Umbraco.DynamicListViews/`, landing at `N3O.Umbraco.Cms/wwwroot/App_Plugins/`. This is a shared output folder used by all apps inside `N3O.Umbraco.Cms`.
- **No `react: true`** — this app uses no React. `@umbraco-cms/*` is still external (always excluded by the preset), but `react` is not mentioned at all. There is no React import anywhere in this codebase. This means the output `.js` file only imports from `@umbraco-cms/*` bare specifiers — all resolved at runtime by Umbraco's own import map.
- **No `additionalExternals`** — this app does not use `@n3o/backoffice-core`. It has no authenticated API calls of its own; the visibility check is handled by the shared condition in BackofficeCore, not by this app's code.

---

### `src/dynamic-children.repository.ts`

This is a **repository class** — a pattern Umbraco's own codebase uses throughout the backoffice for data access. Think of it as a service or repository in C# terms: it hides the data-fetching mechanics from the component that displays the data.

```typescript
import {
    UMB_EDIT_DOCUMENT_WORKSPACE_PATH_PATTERN,
    UmbDocumentTreeRepository,
} from '@umbraco-cms/backoffice/document';
import type { UmbDocumentTreeItemModel } from '@umbraco-cms/backoffice/document';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
```

**Line 1-7 — imports:**

`UmbDocumentTreeRepository` is Umbraco's built-in repository for fetching a document's tree structure. It is part of the backoffice package — an `@umbraco-cms/*` external (not bundled). `UMB_EDIT_DOCUMENT_WORKSPACE_PATH_PATTERN` is a utility for constructing the URL to a document's edit page (equivalent to calling `Url.Action("Edit", new { id = x })` in an MVC Razor view). The `import type` lines are erased at compile time — they exist only for TypeScript's benefit.

`UmbControllerHost` is an interface that any Umbraco Lit element satisfies. It is the "host" concept from Umbraco's controller API: an element that owns a lifecycle and can be passed to controllers and repositories so those objects know when they should clean up. C# analogy: `IDisposable` — a contract that says "I have a lifetime you can hook into."

```typescript
export interface DynamicListViewItem {
    unique: string;
    name: string;
    state: string;
    icon: string;
    createDate: string;
    editPath: string;
}

export interface DynamicListViewChildren {
    items: DynamicListViewItem[];
    total: number;
}
```

**Lines 8-20 — local model interfaces:**

These are the data shapes this app cares about, stripped down from Umbraco's richer `UmbDocumentTreeItemModel`. In C# terms they are simple DTOs or record types. Defining your own model here — rather than letting the component directly consume `UmbDocumentTreeItemModel` — is intentional: if Umbraco's internal model changes, only the `toListViewItem` mapping function needs updating, not the component's rendering logic.

```typescript
const PAGE_SIZE = 50;
```

**Line 22 — page size constant:** the repository fetches at most 50 children per call. If there are more, the component shows a footnote. Increasing this number or adding paging controls would be a self-contained change to this file.

```typescript
export class DynamicChildrenRepository {
    readonly #treeRepository: UmbDocumentTreeRepository;

    constructor(host: UmbControllerHost) {
        this.#treeRepository = new UmbDocumentTreeRepository(host);
    }
```

**Lines 24-29 — class definition:**

`DynamicChildrenRepository` is a plain TypeScript class — not a custom element, not a Lit component. It wraps `UmbDocumentTreeRepository`, which is Umbraco's own repository for tree data. The `#treeRepository` field uses the JavaScript `#` private syntax — it is inaccessible outside this class at runtime (not just at compile time, unlike TypeScript's `private` keyword). See [../concepts/07-lit.md](../concepts/07-lit.md) for an explanation of `#` private fields.

The `host` parameter (the `UmbControllerHost`) is passed straight through to `UmbDocumentTreeRepository`. This is how Umbraco ties the repository's lifecycle to the component's lifecycle: when the component is disconnected from the DOM, the repository can clean up its internal controllers and subscriptions.

```typescript
    async getChildren(unique: string): Promise<DynamicListViewChildren> {
        const { data } = await this.#treeRepository.requestTreeItemsOf({
            parent: { unique, entityType: 'document' },
            paging: { skip: 0, take: PAGE_SIZE },
        });

        const items = (data?.items ?? []).map(toListViewItem);

        return { items, total: data?.total ?? items.length };
    }
```

**Lines 31-40 — `getChildren` method:**

`async/await` here is equivalent to C#'s `async Task<T>`. The method calls `UmbDocumentTreeRepository.requestTreeItemsOf`, passing the document's `unique` key and a paging specification. This is a network request to the Umbraco Management API — it fetches the document's children from the server.

`{ data }` is **destructuring**: equivalent to `var data = result.data;`. The `?.` is the optional-chaining operator (like C# `?.`): if `data` is `null` or `undefined`, the expression short-circuits to `undefined` instead of throwing. The `?? []` is the nullish-coalescing operator (like C# `??`): if the left side is `null`/`undefined`, use the right side instead.

The result is mapped through `toListViewItem` (a module-level function defined below), producing the simplified `DynamicListViewItem[]` the component needs.

```typescript
function toListViewItem(item: UmbDocumentTreeItemModel): DynamicListViewItem {
    return {
        unique: item.unique,
        name: item.variants?.[0]?.name ?? '(unnamed)',
        state: item.variants?.[0]?.state ?? 'Unknown',
        icon: item.documentType.icon,
        createDate: item.createDate,
        editPath: UMB_EDIT_DOCUMENT_WORKSPACE_PATH_PATTERN.generateAbsolute({ unique: item.unique }),
    };
}
```

**Lines 43-52 — `toListViewItem` mapper function:**

This is a module-level (file-scoped) function — not a class method and not exported. It is the mapping layer: `UmbDocumentTreeItemModel` → `DynamicListViewItem`. C# analogy: a static private method or an AutoMapper profile (though this repo does not use AutoMapper — explicit mapping functions like this are the preferred style).

`item.variants?.[0]?.name` — Umbraco documents can have multiple language variants. This takes the first variant's name, falling back to `'(unnamed)'` if none exists.

`UMB_EDIT_DOCUMENT_WORKSPACE_PATH_PATTERN.generateAbsolute(...)` generates the SPA route path for the document editor. The backoffice is a single-page application with its own client-side router. This is the backoffice equivalent of `Url.Action("Edit", "Content", new { id = x })` in an MVC app.

---

### `src/dynamic-list-view.ts`

This is the Lit custom element — the component that renders the "Children" tab UI. See [../concepts/07-lit.md](../concepts/07-lit.md) for detailed coverage of every Lit concept used here.

```typescript
import { LitElement, css, customElement, html, nothing, state } from '@umbraco-cms/backoffice/external/lit';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import { UMB_DOCUMENT_WORKSPACE_CONTEXT } from '@umbraco-cms/backoffice/document';
import { DynamicChildrenRepository } from './dynamic-children.repository';
import type { DynamicListViewItem } from './dynamic-children.repository';
```

**Lines 1-5 — imports:**

All Lit primitives come from `@umbraco-cms/backoffice/external/lit` — Umbraco re-exports Lit from its own bundle. You never import from `'lit'` directly in this codebase (see [../concepts/04-es-modules-and-import-maps.md](../concepts/04-es-modules-and-import-maps.md)).

`UmbElementMixin` is a function that enhances `LitElement` with Umbraco's context API (`consumeContext`, `observe`). See [../concepts/07-lit.md](../concepts/07-lit.md) — "Umbraco's additions".

`UMB_DOCUMENT_WORKSPACE_CONTEXT` is a **context token** — a typed constant used as a key to look up the document workspace context from the DOM ancestor chain. Think of it as the equivalent of `typeof(IDocumentWorkspaceContext)` in a C# DI container.

`DynamicChildrenRepository` and `DynamicListViewItem` are from the file in the same `src/` folder. The relative import (`'./dynamic-children.repository'`) works because both files live in the same directory. Note that the `.ts` extension is omitted — TypeScript resolves it automatically.

```typescript
const elementName = 'n3o-dynamic-list-view';

@customElement(elementName)
export class N3oDynamicListViewElement extends UmbElementMixin(LitElement) {
```

**Lines 7-10 — element registration:**

`@customElement(elementName)` is a TypeScript decorator (like a C# attribute). It calls `customElements.define('n3o-dynamic-list-view', N3oDynamicListViewElement)` on the browser's custom-element registry, which means any `<n3o-dynamic-list-view>` tag inserted into the DOM will be backed by this class. The custom element name must contain a hyphen — this is a browser requirement to distinguish custom elements from built-in HTML elements.

`UmbElementMixin(LitElement)` is the mixin composition: `UmbElementMixin` is a function that takes a class and returns an extended class. The result is `LitElement` + `consumeContext` + `observe`. This is JavaScript's way of doing multiple inheritance / trait composition.

```typescript
    @state()
    private _items: DynamicListViewItem[] = [];

    @state()
    private _total = 0;

    @state()
    private _loading = true;
```

**Lines 11-18 — reactive state:**

Three `@state()` fields drive the UI. When any of them is assigned (e.g. `this._loading = false`), Lit schedules a re-render. The `_` prefix is a naming convention for private Lit state. These are TypeScript `private` fields (compile-time only), not `#` private fields — this is the Lit convention for `@state()` because some internal Lit machinery needs to see the property.

Think of these as a very lightweight view-model: `_loading` controls whether a spinner shows, `_items` is the data for the table rows, `_total` is the count returned by the server (may be larger than `_items.length` if the server has more than `PAGE_SIZE` children).

```typescript
    readonly #repository = new DynamicChildrenRepository(this);
```

**Line 20 — repository instantiation:**

`this` is the element itself, which satisfies `UmbControllerHost` (because `UmbElementMixin` sets that up). The `#` prefix makes this a truly private field — not a Lit reactive field, just a plain instance field. It is `readonly` because the repository never needs to be replaced after construction.

```typescript
    constructor() {
        super();

        this.consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, (context) => {
            if (!context) { return; }
            this.observe(context.unique, (unique) => {
                if (unique) {
                    void this.#load(unique);
                } else {
                    this.#reset();
                }
            });
        });
    }
```

**Lines 22-35 — constructor and context subscription:**

`super()` calls `LitElement`'s constructor — mandatory in any derived class that has a constructor.

`this.consumeContext(TOKEN, callback)` is Umbraco's DI-style service lookup. It walks up the DOM tree to find an ancestor element that provides the `UMB_DOCUMENT_WORKSPACE_CONTEXT` token, then calls the callback with the context object. This is how the component learns which document is currently open in the workspace. If the element is placed outside a document workspace, `context` will be `undefined` and the guard `if (!context) { return; }` prevents a null-reference error.

`this.observe(context.unique, callback)` subscribes to the observable `context.unique`. In Umbraco, context properties are **observables** — reactive streams that emit a new value whenever the state changes. `context.unique` emits the document's unique key (a GUID string) immediately and again whenever the user navigates to a different document. C# analogy: `IObservable<string>` with `Subscribe`. Subscriptions wired through `this.observe()` are automatically unsubscribed when the element disconnects from the DOM.

`void this.#load(unique)` — the `void` operator discards the Promise returned by `#load`. This is a deliberate pattern in this codebase: calling an `async` method from a non-async context without awaiting it. `void` silences the TypeScript "unhandled promise" warning and signals to readers that the result is intentionally discarded. The loading state is managed inside `#load` via the `@state()` fields.

```typescript
    async #load(unique: string): Promise<void> {
        this._loading = true;
        const { items, total } = await this.#repository.getChildren(unique);
        this._items = items;
        this._total = total;
        this._loading = false;
    }

    #reset(): void {
        this._items = [];
        this._total = 0;
    }
```

**Lines 37-48 — data load and reset:**

`#load` uses `async/await` to call the repository (which in turn calls the Umbraco API). Setting `_loading = true` first causes an immediate re-render showing the spinner. Once the await resolves, assigning `_items`, `_total`, and `_loading = false` each individually queue a re-render; Lit batches them and re-renders once.

`#reset` clears the state (called when `context.unique` emits `undefined` — meaning no document is open).

```typescript
    override render() {
        if (this._loading) { 
            return html`<div class="center"><uui-loader></uui-loader></div>`;
        }
        
        if (!this._items.length) { 
            return html`<uui-box><div class="center">There are no child items.</div></uui-box>`;
        }

        return html`
            <uui-box>
                <uui-table>
                    <uui-table-head>
                        <uui-table-head-cell>Name</uui-table-head-cell>
                        <uui-table-head-cell>Status</uui-table-head-cell>
                        <uui-table-head-cell>Created</uui-table-head-cell>
                    </uui-table-head>
                    ${this._items.map((item) => this.#renderRow(item))}
                </uui-table>
                ${this._total > this._items.length
                    ? html`<div class="footnote">Showing ${this._items.length} of ${this._total} items.</div>`
                    : nothing}
            </uui-box>
        `;
    }
```

**Lines 50-74 — `render()`:**

`override render()` must be overridden in every Lit component. It returns a Lit `TemplateResult` produced by the `html` tagged template literal. See [../concepts/07-lit.md](../concepts/07-lit.md) — "The `html` tagged template literal" and "The `render()` method".

`<uui-loader>`, `<uui-box>`, `<uui-table>`, `<uui-table-head>`, `<uui-table-head-cell>`, `<uui-table-row>`, `<uui-table-cell>`, `<uui-button>`, `<uui-icon>`, and `<uui-tag>` are all Umbraco's UI component library (`uui`) elements — custom elements built and shipped by Umbraco. They are the backoffice's design system. You use them exactly like HTML elements. See [../concepts/09-umbraco-backoffice-extensions.md](../concepts/09-umbraco-backoffice-extensions.md).

`${this._items.map((item) => this.#renderRow(item))}` — `Array.map()` transforms each item into a Lit template result. Lit interpolates arrays of template results by rendering each one in order. This is the equivalent of a `foreach` loop in a Razor view.

`nothing` — Lit's sentinel for "render nothing here". Used in the ternary that conditionally shows the footnote.

```typescript
    #renderRow(item: DynamicListViewItem) {
        const color = item.state === 'Published' ? 'positive' : item.state === 'Draft' ? 'warning' : 'default';
        return html`
            <uui-table-row>
                <uui-table-cell>
                    <uui-button compact look="default" href=${item.editPath} label=${item.name}>
                        <uui-icon name=${item.icon}></uui-icon>
                        <span style="margin-left: var(--uui-size-space-2)">${item.name}</span>
                    </uui-button>
                </uui-table-cell>
                <uui-table-cell><uui-tag color=${color} look="secondary">${item.state}</uui-tag></uui-table-cell>
                <uui-table-cell>${new Date(item.createDate).toLocaleDateString()}</uui-table-cell>
            </uui-table-row>
        `;
    }
```

**Lines 76-90 — `#renderRow()`:**

A private helper that returns a Lit template for a single table row. Called from `render()` inside the `map`. The `color` ternary maps publish state strings to `uui-tag` colour values — `'positive'` (green), `'warning'` (yellow), `'default'` (grey).

`href=${item.editPath}` — setting an attribute from a JavaScript expression in Lit. The `${...}` interpolation inside `html\`...\`` updates the DOM attribute when the value changes.

`new Date(item.createDate).toLocaleDateString()` — converts the ISO date string from the API to a human-readable date using the browser's locale. This is a plain JavaScript expression embedded in the template.

```typescript
    static override styles = css`
        :host { display: block; padding: var(--uui-size-layout-1); }
        .center { display: flex; justify-content: center; padding: var(--uui-size-layout-1); }
        .footnote { color: var(--uui-color-text-alt); font-size: var(--uui-type-small-size); padding-top: var(--uui-size-space-4); }
        uui-table-cell uui-button { --uui-button-padding-left-factor: 0; text-align: left; }
    `;
```

**Lines 92-97 — styles:**

`css\`...\`` is a Lit tagged template literal for shadow-DOM-scoped styles. These styles only apply inside this element's shadow tree — they cannot leak to the rest of the page, and the rest of the page's CSS cannot accidentally override them. See [../concepts/07-lit.md](../concepts/07-lit.md) — "Styles".

`:host` targets the outer `<n3o-dynamic-list-view>` element itself. `var(--uui-size-layout-1)`, `var(--uui-color-text-alt)`, etc. are Umbraco's CSS custom property (CSS variable) design tokens — using them keeps spacing and colour consistent with the rest of the backoffice.

`uui-table-cell uui-button { --uui-button-padding-left-factor: 0; }` overrides a CSS custom property on `uui-button` from within this shadow tree. This is the standard pattern for customising `uui` components: the `uui` library exposes its own CSS custom properties as configuration points.

```typescript
export default N3oDynamicListViewElement;

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: N3oDynamicListViewElement;
    }
}
```

**Lines 100-106 — exports and global type augmentation:**

`export default N3oDynamicListViewElement` — named class export (also the default). Rarely imported directly by other files, but conventional.

The `declare global { interface HTMLElementTagNameMap { ... } }` block is a TypeScript ambient declaration that tells the compiler "when any code writes `document.querySelector('n3o-dynamic-list-view')`, the return type is `N3oDynamicListViewElement` rather than the generic `HTMLElement`." This is TypeScript module augmentation — extending an existing interface declared elsewhere. It has no runtime cost.

---

### `wwwroot/App_Plugins/N3O.Umbraco.DynamicListViews/umbraco-package.json`

```json
{
    "$schema": "http://json.schemastore.org/umbraco-package.json",
    "id": "N3O.Umbraco.DynamicListViews",
    "name": "N3O Dynamic List Views",
    "version": "17.0.0",
    "extensions": [
        {
            "type": "workspaceView",
            "alias": "N3O.WorkspaceView.DynamicListView",
            "name": "N3O Dynamic List View",
            "element": "/App_Plugins/N3O.Umbraco.DynamicListViews/dynamic-list-view.js",
            "weight": 300,
            "meta": {
                "label": "Children",
                "pathname": "dynamic-children",
                "icon": "icon-list"
            },
            "conditions": [
                {
                    "alias": "Umb.Condition.WorkspaceAlias",
                    "match": "Umb.Workspace.Document"
                },
                {
                    "alias": "N3O.Condition.WorkspaceVisibility",
                    "endpoint": "/umbraco/backoffice/api/DynamicListViewApi"
                }
            ]
        }
    ]
}
```

**Top-level fields:**

| Field | Value | Meaning |
|-------|-------|---------|
| `"$schema"` | JSONSchema URL | Enables IDE validation and IntelliSense for this file |
| `"id"` | `"N3O.Umbraco.DynamicListViews"` | Unique identifier for this package within the Umbraco backoffice |
| `"name"` | `"N3O Dynamic List Views"` | Display name (shown in Umbraco's package management UI) |
| `"version"` | `"17.0.0"` | Package version (CalVer in this repo; major matches Umbraco major) |

**Extension fields:**

| Field | Value | Meaning |
|-------|-------|---------|
| `"type"` | `"workspaceView"` | Registers this as a tab inside a document workspace. Umbraco knows to render it as a panel tab alongside the built-in "Content", "Info", etc. tabs. |
| `"alias"` | `"N3O.WorkspaceView.DynamicListView"` | The unique identifier for this specific extension. Used to override or reference it from other packages. Convention: `N3O.<Type>.<Name>`. |
| `"name"` | `"N3O Dynamic List View"` | Internal name (used in Umbraco's extension registry; not shown to editors) |
| `"element"` | `"/App_Plugins/.../dynamic-list-view.js"` | The absolute URL (served from ASP.NET Core's static-file middleware) of the compiled JS file to load. Umbraco loads this as an ES module when the extension activates. |
| `"weight"` | `300` | Controls tab ordering. Lower numbers appear first. Umbraco's built-in Content tab has weight 100; a weight of 300 places "Children" after the default tabs. |
| `"meta.label"` | `"Children"` | The tab label shown to the editor in the UI. |
| `"meta.pathname"` | `"dynamic-children"` | The URL path segment appended to the workspace URL when this tab is active (e.g. `/umbraco/section/content/workspace/document/<guid>/dynamic-children`). |
| `"meta.icon"` | `"icon-list"` | The icon displayed on the tab (from Umbraco's icon set). |

**Conditions:**

All conditions in the array must pass simultaneously for the workspaceView to be rendered. This is an AND relationship.

| Condition alias | Config | What it checks |
|-----------------|--------|----------------|
| `"Umb.Condition.WorkspaceAlias"` | `"match": "Umb.Workspace.Document"` | Built-in Umbraco condition. True only when the surrounding workspace is the document editor. Prevents this tab appearing in, for example, the media or member workspaces. |
| `"N3O.Condition.WorkspaceVisibility"` | `"endpoint": "/umbraco/backoffice/api/DynamicListViewApi"` | Shared N3O condition registered by BackofficeCore (see [backofficecore.md](backofficecore.md)). Makes an authenticated HTTP GET to the given endpoint, passing the current document's unique key as a query parameter. The C# `DynamicListViewApiController` returns `{ visible: true/false }` based on whether the current document type is configured for dynamic list views. If `visible` is `false`, this condition fails and the "Children" tab is never rendered. |

The `N3O.Condition.WorkspaceVisibility` condition was specifically designed to replace per-plugin bespoke conditions. Before this shared condition existed, DynamicListViews had its own condition implementation. The current design means the visibility logic lives entirely in C# (`DynamicListViewApiController`) and the frontend condition is a generic mechanism. This is the "endpoint-driven condition" pattern described in the BackofficeCore docs.

---

## Concepts demonstrated

- **Lit custom element** — a class extending `UmbElementMixin(LitElement)`, registered with `@customElement`. See [../concepts/07-lit.md](../concepts/07-lit.md).
- **`@state()` reactive fields** — `_items`, `_total`, `_loading` drive the render cycle. See [../concepts/07-lit.md](../concepts/07-lit.md).
- **`consumeContext` + `observe`** — subscribing to the document workspace context and reacting to document navigation. See [../concepts/07-lit.md](../concepts/07-lit.md).
- **Repository pattern** — `DynamicChildrenRepository` encapsulates Umbraco's tree data-source. Analogous to a C# repository class.
- **`uui` components** — `<uui-box>`, `<uui-table>`, `<uui-tag>`, etc. See [../concepts/09-umbraco-backoffice-extensions.md](../concepts/09-umbraco-backoffice-extensions.md).
- **WorkspaceView extension** — `"type": "workspaceView"` in `umbraco-package.json`. See [../concepts/09-umbraco-backoffice-extensions.md](../concepts/09-umbraco-backoffice-extensions.md).
- **Condition-gated extensions** — two conditions must pass before the tab renders. See [../concepts/09-umbraco-backoffice-extensions.md](../concepts/09-umbraco-backoffice-extensions.md).
- **`N3O.Condition.WorkspaceVisibility`** — the shared endpoint-driven visibility condition from [backofficecore.md](backofficecore.md).
- **No React** — pure Lit with no bridge pattern. Compare to [../concepts/10-the-n3o-bridge-pattern.md](../concepts/10-the-n3o-bridge-pattern.md) to see the difference.
- **Vite library build** — `n3oPluginConfig` without `react: true`. See [../concepts/05-vite-and-the-build.md](../concepts/05-vite-and-the-build.md).

---

## Gotchas

**The repository is not a service registration — it is just a class.**
`DynamicChildrenRepository` is instantiated directly with `new` in the component's class body (line 20). It is not registered in a DI container. This is normal in the Umbraco backoffice frontend. The `host` pattern provides lifecycle control instead of DI.

**The `@state()` decorator requires specific TypeScript settings.**
The `useDefineForClassFields: false` and `experimentalDecorators: true` settings in the shared `tsconfig` are mandatory for Lit decorators to work correctly. These come from `@n3o/build/tsconfig` and are already set. Do not override them in this app's `tsconfig.json`.

**Conditions are AND-ed.**
Both conditions must pass. If the `Umb.Condition.WorkspaceAlias` check were removed, this tab would appear in the media workspace, member workspace, etc. — in each case making a pointless API call and rendering nothing. Always include `Umb.Condition.WorkspaceAlias` when registering a document-specific workspaceView.

**The `endpoint` in `N3O.Condition.WorkspaceVisibility` must match the C# controller route exactly.**
`"/umbraco/backoffice/api/DynamicListViewApi"` is the route registered by `DynamicListViewApiController` in `src/N3O.Umbraco.Extensions/Features/DynamicListViews/`. A typo here silently fails: the condition will get an HTTP 404, interpret it as "not visible", and the "Children" tab will never appear — with no obvious error in the UI.

**The element name `'n3o-dynamic-list-view'` must match what is in the manifest's `element` JS file.**
If you rename the element name constant but forget to update the manifest, or vice versa, the backoffice loads the JS (which registers the new element name) but then tries to render the old name — and the tab renders as an unknown element with no content.

**`PAGE_SIZE = 50` is a hard constant — there is no server-side paging UI.**
The footnote "Showing X of Y items" appears when there are more than 50 children, but there is no button to load more. Adding pagination would require changes to `DynamicChildrenRepository.getChildren` (expose `skip` as a parameter) and to `N3oDynamicListViewElement` (add paging state and controls).

**Vite bundles both source files into one output file.**
`vite.config.ts` declares only one entry: `dynamic-list-view.ts`. Vite follows the `import` in `dynamic-list-view.ts` to `dynamic-children.repository.ts` and bundles both into a single `dynamic-list-view.js`. There is no separate `dynamic-children.repository.js`. Do not add `dynamic-children.repository.ts` as a second entry unless you specifically want a separate loadable module.
