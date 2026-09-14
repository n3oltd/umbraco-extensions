# 07 — Lit and Umbraco's Backoffice Element Model

This document explains what Lit is, how Umbraco uses it, and how this repo's backoffice plugins use it. It is written for a .NET/C# developer with no prior exposure to JavaScript UI frameworks.

---

## What is Lit?

**Lit** is a small JavaScript library (~5 KB) from Google. It does not replace the browser's built-in DOM — it is a thin layer on top of the **Web Components** browser standard.

**Web Components** are the browser's native way of defining custom HTML elements. You can literally write:

```html
<my-button label="Click me"></my-button>
```

…and have the browser treat `my-button` as a real element with its own DOM, styles, and behaviour. No framework required. Lit simply makes this less tedious to write.

**Analogy:** Think of Web Components as the CLR's `System.ComponentModel` infrastructure, and Lit as a thin base class that implements the boilerplate (`INotifyPropertyChanged`, lifecycle, templating) so you don't have to.

---

## `LitElement` — the base class

Every Lit component extends `LitElement`, which in turn extends the browser's built-in `HTMLElement`.

```
HTMLElement  (browser built-in, like System.Object for DOM nodes)
    └── LitElement  (Lit's base class, adds reactive rendering)
        └── YourElement  (your custom element)
```

In C# terms: `LitElement` is an abstract base class. You inherit from it, declare reactive properties, and override one method (`render()`). Lit handles when and how to call `render()`.

In this repo, the imports come from the Umbraco package namespace:

```typescript
// src/N3O.Umbraco.Cms/Apps/N3O.Umbraco.DynamicListViews/src/dynamic-list-view.ts  line 1
import { LitElement, css, customElement, html, nothing, state } from '@umbraco-cms/backoffice/external/lit';
```

That path re-exports the real Lit library from inside the Umbraco backoffice bundle. You never import directly from `'lit'`.

---

## `@customElement` — registering the element

```typescript
// dynamic-list-view.ts  line 7-10
const elementName = 'n3o-dynamic-list-view';

@customElement(elementName)
export class N3oDynamicListViewElement extends UmbElementMixin(LitElement) {
```

`@customElement` is a **decorator** (like a C# `[Attribute]`). It calls the browser's `customElements.define('n3o-dynamic-list-view', N3oDynamicListViewElement)` for you. After this, any HTML like `<n3o-dynamic-list-view>` is an instance of your class.

**Important rule:** custom element names must contain a hyphen (the browser enforces this to distinguish them from built-in elements).

---

## Reactive properties vs state

Lit has two categories of reactive data, analogous to different C# patterns:

| Lit concept | Decorator | C# analogy | Notes |
|---|---|---|---|
| **Reactive property** | `@property()` | A public `[JsonProperty]`-decorated field | Settable from outside; can reflect to/from an HTML attribute |
| **Internal state** | `@state()` | A private backing field with `INotifyPropertyChanged` | Private to the component; triggers re-render when changed |

**This repo uses the decorator form** (`@state()` / `@property()`), not the `static properties = { ... }` class-field form. You will see `@state()` throughout:

```typescript
// dynamic-list-view.ts  lines 11-18
@state()
private _items: DynamicListViewItem[] = [];

@state()
private _total = 0;

@state()
private _loading = true;
```

The leading `_` is a naming convention for private state. When you assign to one of these fields (e.g. `this._loading = false`), Lit schedules a re-render automatically — you do not call any update method explicitly.

**The alternative (class-field) form** looks like this and works identically — it is just the older style used in some Umbraco docs:

```typescript
// You will NOT see this form in this repo, but you may see it in external examples:
static properties = {
    _loading: { state: true },
    label: { type: String },   // @property() equivalent
};
```

---

## The `html` tagged template literal

The `render()` method returns the element's DOM using the `html` tag:

```typescript
// dynamic-list-view.ts  lines 50-53
override render() {
    if (this._loading) {
        return html`<div class="center"><uui-loader></uui-loader></div>`;
    }
```

`html\`...\`` is a **tagged template literal**. In JavaScript, a tag is a function called with the template's static parts and dynamic expressions separately. Lit uses this to build a "template" object once and then efficiently patch only the changed parts into the real DOM on re-renders.

**C# analogy:** it is similar to how `FormattableString` separates the format string from its arguments, allowing the consumer (e.g. a SQL parameterisation library) to treat them differently rather than just concatenating.

The key practical points:
- Write HTML inside the backticks.
- Embed JavaScript expressions with `${...}`.
- Lit only updates DOM nodes whose expressions changed — it does not re-create the whole subtree.

---

## The `render()` method

`render()` is the single method you must override. It is called by Lit whenever reactive state changes. It must return a Lit `TemplateResult` (from `html\`...\``).

```typescript
// dynamic-list-view.ts  lines 50-74
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

`nothing` is a Lit sentinel value meaning "render nothing here" — equivalent to returning `null` from a C# method that builds a UI tree.

---

## Event binding in templates

Inside `html\`...\``, prefix an attribute with `@` to attach a DOM event listener:

```typescript
// platforms-urls-info-app.ts  line 96
<button class="copy" @click=${() => this.#copy(url)} title="Copy">Copy</button>
```

`@click=${handler}` is equivalent to `element.addEventListener('click', handler)`. Lit attaches and removes these listeners automatically as the template is updated or the element is removed.

---

## `nothing` and conditional rendering

```typescript
// dynamic-list-view.ts  lines 69-71
${this._total > this._items.length
    ? html`<div class="footnote">Showing ${this._items.length} of ${this._total} items.</div>`
    : nothing}
```

Use the ternary `condition ? html\`...\` : nothing` pattern for conditional blocks. You can also use `condition && html\`...\`` but `nothing` is preferred when the false branch should render no DOM.

---

## Styles

```typescript
// dynamic-list-view.ts  lines 92-97
static override styles = css`
    :host { display: block; padding: var(--uui-size-layout-1); }
    .center { display: flex; justify-content: center; padding: var(--uui-size-layout-1); }
    .footnote { color: var(--uui-color-text-alt); font-size: var(--uui-type-small-size); ... }
    uui-table-cell uui-button { --uui-button-padding-left-factor: 0; text-align: left; }
`;
```

`css\`...\`` is another tagged template literal. These styles are **scoped to this element's shadow DOM** — they cannot leak to the page and the page cannot accidentally override them. `:host` refers to the element itself (the `<n3o-dynamic-list-view>` node).

The `var(--uui-size-layout-1)` values are **CSS custom properties** (CSS variables). The Umbraco backoffice defines a design-token system via these variables; using them keeps your UI consistent with the rest of the backoffice.

---

## Lifecycle callbacks

Lit inherits from `HTMLElement` and keeps its lifecycle but adds hooks. The most important ones:

| Callback | When it fires | C# analogy |
|---|---|---|
| `connectedCallback()` | Element is added to a live document (inserted into the page) | Constructor or `OnInitialized` |
| `disconnectedCallback()` | Element is removed from the document | `IDisposable.Dispose()` |
| `firstUpdated(changedProperties)` | Lit's first render is committed to the DOM | `OnAfterRender(firstRender: true)` in Blazor |
| `updated(changedProperties)` | Every time Lit commits an update (including the first) | `OnAfterRender` |
| `willUpdate(changedProperties)` | Before Lit renders; used to compute derived values | Property change handler before rendering |

In this repo, constructor-based setup is preferred (see below), not `connectedCallback`.

---

## Private class fields (`#`)

You will see `#` prefixes:

```typescript
// dynamic-list-view.ts  lines 20-21
readonly #repository = new DynamicChildrenRepository(this);
```

This is the JavaScript `#` private field syntax. It is enforced by the runtime (not just a naming convention), unlike the TypeScript `private` keyword which is only a compile-time check. Think of it as a truly encapsulated private field — closer to C#'s `private` than TypeScript's `private`.

---

## Umbraco's additions: `UmbElementMixin` and `UmbLitElement`

Umbraco extends Lit with backoffice-specific capabilities. There are two ways to get them:

### `UmbElementMixin(LitElement)`

A **mixin** is a function that takes a base class and returns an extended class. It is the JavaScript equivalent of multiple interface implementation or trait composition.

```typescript
// dynamic-list-view.ts  line 10
export class N3oDynamicListViewElement extends UmbElementMixin(LitElement) {
```

`UmbElementMixin` adds `consumeContext()` and `observe()` to `LitElement`. Use this when you need both Lit rendering and Umbraco context.

### `UmbLitElement`

A pre-composed class that already applies `UmbElementMixin`:

```typescript
// platforms-urls-info-app.ts  line 1-2
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
// ...
export class N3oPlatformsUrlsInfoAppElement extends UmbLitElement {
```

`UmbLitElement` is functionally equivalent to `UmbElementMixin(LitElement)`. The difference is purely import style. **Both give you `consumeContext()` and `observe()`.**

---

## `consumeContext(TOKEN, callback)` — getting backoffice services

The Umbraco backoffice uses a **context API** to share services between elements. Think of it as a typed service-locator or dependency-injection container, but implemented through the DOM tree rather than a static container.

```typescript
// dynamic-list-view.ts  lines 25-34
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
```

- `UMB_DOCUMENT_WORKSPACE_CONTEXT` is a **context token** — a typed constant that identifies what service you want. Think of it as a `typeof(IDocumentWorkspaceContext)` in C# DI.
- The callback receives the context instance when it becomes available (contexts are resolved by walking up the DOM tree to find an ancestor that provides the token).
- Umbraco also provides an auth context token: `UMB_AUTH_CONTEXT` (seen in `platforms-urls-info-app.ts` line 4).

---

## `this.observe(observable, callback)` — reacting to live data

Umbraco contexts expose data as **observables** (a reactive stream, similar to `IObservable<T>` in .NET's Rx). `this.observe()` subscribes to one:

```typescript
// dynamic-list-view.ts  lines 27-33
this.observe(context.unique, (unique) => {
    if (unique) {
        void this.#load(unique);
    } else {
        this.#reset();
    }
});
```

- `context.unique` is an observable that emits the current document's unique key (a GUID string) whenever it changes.
- The callback fires immediately with the current value, and again whenever the value changes — like `IObservable<T>.Subscribe()` with an `OnNext` handler.
- Subscriptions created via `this.observe()` are automatically cleaned up when the element is disconnected from the DOM. You do not need to unsubscribe manually.

---

## Full walkthrough: `N3oDynamicListViewElement`

File: `src/N3O.Umbraco.Cms/Apps/N3O.Umbraco.DynamicListViews/src/dynamic-list-view.ts`

**What it does:** renders a table of child documents inside a document's workspace (a tab called "Children").

**How it works:**

1. The element extends `UmbElementMixin(LitElement)`, giving it both rendering and context APIs.
2. Three `@state()` fields — `_items`, `_total`, `_loading` — drive the render. Changing any of them triggers a re-render.
3. In the constructor, `consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, ...)` asks Umbraco: "when the document workspace context is available in my ancestor chain, give it to me."
4. Inside that callback, `observe(context.unique, ...)` subscribes to the current document's key. When the key changes (user navigates to a different document), `#load()` is called.
5. `#load()` calls the `DynamicChildrenRepository` (a data-access helper), then assigns the results to `_items` and `_total`. Lit sees those assignments and calls `render()`.
6. `render()` returns different templates depending on `_loading` and `_items.length`.
7. Private helper `#renderRow()` returns a `TemplateResult` for each table row — it is called via `this._items.map(...)` inside the main template.

---

## Full walkthrough: `N3oPlatformsUrlsInfoAppElement`

File: `src/Cloud/N3O.Umbraco.Cloud.Platforms.StaticAssets/Apps/src/platforms-urls-info-app.ts`

**What it does:** shows staging and production URLs in the document Info panel when the current document is a campaign or offering.

**How it works:**

1. Extends `UmbLitElement` (equivalent to `UmbElementMixin(LitElement)`).
2. Two `@state()` fields — `_stagingUrl`, `_productionUrl` — control visibility. If both are null, `render()` returns an empty template and nothing is shown.
3. Two private non-reactive fields — `#unique` and `#authConfig` — hold the document key and auth configuration. These are not `@state()` because changing them should not trigger a render on their own; a render is triggered only after the async load completes and sets the `@state()` fields.
4. `consumeContext(UMB_AUTH_CONTEXT, ...)` retrieves the auth context and stores `authConfig.getOpenApiConfiguration()`. This provides the bearer token needed for API calls.
5. `consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, ...)` + `observe(context?.unique, ...)` gives the current document key. Both pieces of data must be available before `#loadUrls()` is called.
6. `#loadUrls()` manually fetches the backend API with an `Authorization: Bearer <token>` header. If `permitted` is false (not a campaign/offering), both URL fields stay null and the element renders nothing — a self-managing visibility pattern.
7. `#renderRow()` produces a labelled row with a clickable URL and a copy button. `@click=${() => this.#copy(url)}` wires the button to the Clipboard API.

---

## Summary

| Concept | What it does |
|---|---|
| `LitElement` | Base class; adds reactive rendering to `HTMLElement` |
| `@state()` | Marks a private field as reactive state; triggers re-render on assignment |
| `@property()` | Like `@state()` but publicly settable and reflected to HTML attributes |
| `html\`...\`` | Tagged template literal; returns a Lit template for efficient DOM updates |
| `css\`...\`` | Tagged template literal; returns scoped styles for this element's shadow DOM |
| `render()` | Override this to describe the element's UI |
| `nothing` | Sentinel for "render no DOM here" |
| `@click=${fn}` | Attaches a DOM event listener inside a template |
| `@customElement(name)` | Registers the class as a custom HTML element |
| `UmbElementMixin` / `UmbLitElement` | Adds `consumeContext()` and `observe()` to Lit |
| `consumeContext(TOKEN, cb)` | DI-style lookup: get a backoffice service from ancestor context |
| `observe(observable, cb)` | Subscribe to a live data stream; auto-cleaned on disconnect |

---

*Next: [08 — React fundamentals](./08-react.md)*
