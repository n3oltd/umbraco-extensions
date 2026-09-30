# 06 — Web Components and Shadow DOM

**Prerequisites:** [04 — ES Modules and Import Maps](./04-es-modules-and-import-maps.md), [05 — Vite and the Build](./05-vite-and-the-build.md).

---

## What are web components?

The Umbraco 17 ("Bellissima") backoffice is built entirely from **web components**. Every panel, editor, button, and dialog you see in the backoffice UI is a web component. Understanding what they are and how they work is essential for reading any plugin code in this repo.

Web components are a set of three browser-native standards that together let you create custom, reusable HTML elements:

1. **Custom Elements** — define a new HTML tag backed by a JavaScript class.
2. **Shadow DOM** — attach a private, encapsulated DOM tree to an element.
3. **HTML Templates** — declare markup fragments that are not rendered until cloned (less relevant to this repo; not covered here).

The key difference from .NET controls or Razor components: web components are **native browser features**, not a framework. They require no runtime library to function at their core.

---

## Custom Elements

### Defining a custom element

A custom element is a plain JavaScript class that extends `HTMLElement`:

```ts
class MyCounter extends HTMLElement {
    #count = 0;

    connectedCallback(): void {
        this.textContent = String(this.#count);
    }
}

customElements.define('my-counter', MyCounter);
```

After `customElements.define('my-counter', MyCounter)`, any occurrence of `<my-counter>` in HTML causes the browser to instantiate `MyCounter` for that DOM node.

Rules:
- The tag name **must contain a hyphen** (`my-counter`, not `mycounter`). This avoids collisions with built-in HTML elements.
- `customElements.define` is called once per element type — registering the same name twice throws.

### The `@customElement` decorator

TypeScript + the `@umbraco-cms/backoffice/external/lit` package expose a class decorator that handles the `customElements.define` call automatically:

```ts
import { customElement } from '@umbraco-cms/backoffice/external/lit';

@customElement('n3o-scheduler-dashboard')
export class N3oSchedulerDashboardElement extends HTMLElement {
    // ...
}
```

This is syntactic sugar — equivalent to writing `customElements.define('n3o-scheduler-dashboard', N3oSchedulerDashboardElement)` after the class definition.

### Using a custom element in HTML

Once defined, the element is just HTML:

```html
<n3o-scheduler-dashboard></n3o-scheduler-dashboard>
```

The Umbraco backoffice does not write HTML files manually. Instead, the extension manifest (`umbraco-package.json`) tells the backoffice which custom element name to use for a given extension slot (workspace view, dashboard, property editor, etc.), and the backoffice inserts the element into its own DOM at the appropriate time.

---

## Lifecycle callbacks

Every custom element class can implement these methods, which the browser calls automatically:

| Callback                  | When it fires                                               | C# analogy                              |
|---------------------------|-------------------------------------------------------------|-----------------------------------------|
| `constructor()`           | Element object is created (may be before it enters the DOM) | Class constructor / `new MyControl()`   |
| `connectedCallback()`     | Element is inserted into a live document                    | `OnLoad` / `Page.Load` / control mounting |
| `disconnectedCallback()`  | Element is removed from the document                        | `Dispose()` / `IDisposable.Dispose`     |
| `attributeChangedCallback(name, oldVal, newVal)` | An observed HTML attribute changes  | Property setter with notification |
| `adoptedCallback()`       | Element is moved to a different document (rare)             | (no close analogy)                      |

The `constructor` runs before the element is in the DOM. DOM manipulation (adding children, reading layout) belongs in `connectedCallback`. Cleanup (cancelling timers, unmounting React) belongs in `disconnectedCallback`.

### `attributeChangedCallback` and `observedAttributes`

HTML attributes are string key/value pairs (`<my-el color="red">`). To be notified when an attribute changes, declare a static `observedAttributes` array and implement `attributeChangedCallback`:

```ts
class MyEl extends HTMLElement {
    static observedAttributes = ['color'];

    attributeChangedCallback(name: string, _old: string | null, newVal: string | null): void {
        if (name === 'color') {
            this.style.color = newVal ?? '';
        }
    }
}
```

**Attributes are always strings.** If you need to pass complex data (objects, arrays) to a custom element, use a **JavaScript property** instead of an HTML attribute.

### Attributes vs properties

| Feature     | Attribute                        | Property                             |
|-------------|----------------------------------|--------------------------------------|
| Set from    | HTML markup `<el attr="val">`    | JS/TS: `el.myProp = someObject`      |
| Type        | Always `string`                  | Any JavaScript type                  |
| Observation | `attributeChangedCallback`       | Custom setter / getter               |
| Reflection  | Optional (`getAttribute`)        | N/A                                  |

In the N3O plugin code, the Umbraco backoffice sets complex data (block content, settings objects) on custom elements via **properties**, not attributes. In `block-preview.ts` you can see getter/setter pairs on `content` and `settings`:

```ts
// src/Blocks/N3O.Umbraco.Blocks.StaticAssets/Apps/src/block-preview.ts (lines 40–56)

set content(value: UmbBlockEditorCustomViewElement['content']) {
    this.#content = value;
    this.#onDataChanged();
}

set settings(value: UmbBlockEditorCustomViewElement['settings']) {
    this.#settings = value;
    this.#onDataChanged();
}
```

When the backoffice sets `el.content = { ... }` (a JavaScript assignment), the setter fires, the element re-renders. This is the equivalent of a C# property setter triggering `OnPropertyChanged`.

---

## Shadow DOM

### The problem Shadow DOM solves

In a normal HTML document, CSS is global — any rule can accidentally match any element. This is unmanageable at scale: a `div { margin: 0 }` rule somewhere in the backoffice CSS could break your plugin's layout.

Shadow DOM creates a **private, isolated DOM tree** attached to a host element. Styles inside the shadow root don't leak out; styles outside don't leak in. The shadow root is its own encapsulated document fragment.

### Attaching a shadow root

```ts
constructor() {
    super();
    const shadow = this.attachShadow({ mode: 'open' });
    // Everything you append to `shadow` is inside the shadow root.
    const p = document.createElement('p');
    p.textContent = 'Inside shadow DOM';
    shadow.appendChild(p);
}
```

`mode: 'open'` means JavaScript outside the element can still access the shadow root via `el.shadowRoot`. `mode: 'closed'` hides it. All N3O elements use `'open'`.

### Light DOM vs shadow DOM

| Concept    | Meaning                                                                      |
|------------|------------------------------------------------------------------------------|
| Light DOM  | The element's regular children, visible to the outside world                 |
| Shadow DOM | The private DOM tree attached via `attachShadow`; isolated from outside CSS  |

From inside the shadow root, `:host` is a CSS pseudo-selector that refers to the custom element itself (the host). The Scheduler uses this:

```css
/* src/Scheduler/N3O.Umbraco.Scheduler.StaticAssets/Apps/src/scheduler-dashboard-app.css */
:host { display: block; width: 100%; }
iframe { display: block; width: 100%; height: calc(100dvh - 200px); min-height: 600px; border: 0; }
```

The `iframe` rule here only applies to `<iframe>` elements inside this element's shadow root — it cannot affect any other iframe on the page.

### React inside a shadow root

Both of the main N3O plugin custom elements (`N3oSchedulerDashboardElement`, `N3oBlockPreviewElement`, `N3oDataExportElement`) mount React inside the shadow root. The pattern:

1. In `constructor()`: call `attachShadow`, create a `<div>` mount point, append it to the shadow root.
2. In `connectedCallback()`: call `createRoot(this.#mount)` and render the React app into it.
3. In `disconnectedCallback()`: call `this.#root?.unmount()` to clean up React.

```ts
// src/Scheduler/N3O.Umbraco.Scheduler.StaticAssets/Apps/src/scheduler-dashboard.ts (lines 22–41)

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
```

`createRoot` and `unmount` are the React 18+ APIs (in `react-dom/client`). This is discussed further in [10 — The N3O Bridge Pattern](./10-the-n3o-bridge-pattern.md).

---

## `<slot>` — content projection

In Razor, `@RenderBody()` lets a layout render its page's content at a chosen position. In web components, `<slot>` does the same: it projects the host element's **light DOM children** into the shadow root.

```ts
// In the constructor:
const shadow = this.attachShadow({ mode: 'open' });
shadow.innerHTML = `
  <header>My Header</header>
  <slot></slot>
  <footer>My Footer</footer>
`;
```

```html
<!-- Usage -->
<my-card>
    <p>This content is projected into the slot.</p>
</my-card>
```

The `<p>` lives in the light DOM but is rendered where `<slot>` appears inside the shadow root.

Named slots (`<slot name="actions">`) allow multiple projection points. The N3O plugin elements don't use slots directly — they mount React into a `<div>` instead — but slots are fundamental to how Umbraco's built-in `<uui-*>` and `<umb-*>` components work.

---

## `adoptedStyleSheets` — injecting CSS into shadow DOM

Global `<link>` tags and `<style>` tags in the `<head>` don't affect shadow roots. To style content inside a shadow root, two approaches exist:

1. A `<style>` tag appended to the shadow root itself.
2. The **Constructable Stylesheets** API — `new CSSStyleSheet()`.

The N3O elements use approach 2 via `adoptedStyleSheets`:

```ts
const sheet = new CSSStyleSheet();
sheet.replaceSync(cssText);   // cssText is the string from the ?inline import
shadow.adoptedStyleSheets = [sheet];
```

This is more efficient than approach 1 for shared stylesheets (the same `CSSStyleSheet` object can be adopted by multiple shadow roots with no duplication). For the `?inline` CSS import that makes `cssText` a string, see [05 — Vite and the Build](./05-vite-and-the-build.md).

---

## Events and `CustomEvent`

The browser's native event system works fine across shadow boundaries. A custom element can emit an event:

```ts
this.dispatchEvent(new CustomEvent('my-change', {
    detail: { value: 42 },
    bubbles: true,
    composed: true,   // crosses the shadow boundary upward
}));
```

- `bubbles: true` — the event travels up the DOM tree.
- `composed: true` — the event crosses shadow boundaries. Without this, an event stops at the shadow root edge and is not visible to the outside document.

Listeners attach the same way as for built-in events:

```ts
el.addEventListener('my-change', (e) => {
    const ev = e as CustomEvent<{ value: number }>;
    console.log(ev.detail.value);
});
```

The N3O plugin elements don't emit custom events externally — they use Umbraco's context API (RxJS observables, `consumeContext`) to communicate. But the `CustomEvent` / `dispatchEvent` mechanism is how Umbraco's own components communicate, so understanding it helps when reading `@umbraco-cms` source.

---

## Walkthrough — `N3oSchedulerDashboardElement`

This is the simplest N3O custom element — no context plumbing, pure React shell:

```ts
// src/Scheduler/N3O.Umbraco.Scheduler.StaticAssets/Apps/src/scheduler-dashboard.ts

import { customElement } from '@umbraco-cms/backoffice/external/lit';
import { createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { SchedulerDashboardApp } from './scheduler-dashboard-app';
import cssText from './scheduler-dashboard-app.css?inline';   // (1)

const elementName = 'n3o-scheduler-dashboard';

@customElement(elementName)                                    // (2)
export class N3oSchedulerDashboardElement extends HTMLElement {
    #root?: Root;
    #mount: HTMLDivElement;

    constructor() {
        super();
        const shadow = this.attachShadow({ mode: 'open' });   // (3)
        const sheet = new CSSStyleSheet();
        sheet.replaceSync(cssText);
        shadow.adoptedStyleSheets = [sheet];                   // (4)
        this.#mount = document.createElement('div');
        shadow.appendChild(this.#mount);                       // (5)
    }

    connectedCallback(): void {                                // (6)
        this.#root ??= createRoot(this.#mount);
        this.#root.render(createElement(SchedulerDashboardApp));
    }

    disconnectedCallback(): void {                             // (7)
        this.#root?.unmount();
        this.#root = undefined;
    }
}
```

1. `cssText` is the raw CSS string from the `?inline` Vite import.
2. `@customElement` registers `'n3o-scheduler-dashboard'` in the browser's custom element registry — equivalent to calling `customElements.define(...)`.
3. A shadow root is attached. Styles and markup inside it are isolated.
4. The CSS string is loaded into a `CSSStyleSheet` and adopted by the shadow root — styles apply inside the shadow root only.
5. A plain `<div>` is created and appended to the shadow root. This `div` is the **mount point** where React will render.
6. `connectedCallback` fires when the backoffice inserts `<n3o-scheduler-dashboard>` into the page. `createRoot` initialises React on `this.#mount`; `render` draws the React component tree.
7. `disconnectedCallback` fires when the backoffice navigates away and removes the element. `unmount` tears down React and frees all React state — equivalent to `Dispose()` on a .NET component.

---

## Walkthrough — `N3oBlockPreviewElement`

This is a more complex element that also uses Umbraco's context system (`consumeContext`) to observe data from the surrounding document workspace and block editor:

```ts
// src/Blocks/N3O.Umbraco.Blocks.StaticAssets/Apps/src/block-preview.ts (lines 33–113, abridged)

@customElement(elementName)
export class N3oBlockPreviewElement extends UmbAuthFetchMixin(UmbElementMixin(HTMLElement))
        implements UmbBlockEditorCustomViewElement {

    // Properties set by the Umbraco block editor (complex objects — not HTML attributes)
    set content(value: UmbBlockEditorCustomViewElement['content']) {
        this.#content = value;
        this.#onDataChanged();
    }

    set settings(value: UmbBlockEditorCustomViewElement['settings']) {
        this.#settings = value;
        this.#onDataChanged();
    }

    constructor() {
        super();
        const shadow = this.attachShadow({ mode: 'open' });
        this.#mount = document.createElement('div');
        shadow.appendChild(this.#mount);

        // Consume Umbraco contexts — equivalent to requesting services from the DI container.
        this.consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, (context) => {
            this.observe(context.unique, (unique) => { this.#nodeKey = unique; }, '_observeUnique');
        });

        this.consumeContext(UMB_BLOCK_ENTRY_CONTEXT, (context) => {
            this.observe(context.contentKey, (key) => { this.#contentKey = key; }, '_observeContentKey');
        });

        this.consumeContext(UMB_BLOCK_MANAGER_CONTEXT, (context) => {
            this.#blockManager = context;
        });
    }

    connectedCallback(): void {
        super.connectedCallback();
        this.#root ??= createRoot(this.#mount);
        this.#render();
        this.#scheduleReload(0);                // fetch preview HTML from the C# backend
    }

    disconnectedCallback(): void {
        super.disconnectedCallback();
        if (this.#reloadHandle !== undefined) {
            clearTimeout(this.#reloadHandle);
            this.#reloadHandle = undefined;
        }
        this.#root?.unmount();
        this.#root = undefined;
    }
}
```

Key points compared to the Scheduler element:

- **`extends UmbAuthFetchMixin(UmbElementMixin(HTMLElement))`** — JavaScript **mixins** (like C# extension methods that add state + behaviour via inheritance). `UmbElementMixin` adds Umbraco's context plumbing (`consumeContext`, `observe`). `UmbAuthFetchMixin` (from `@n3o/backoffice-core`) adds `this.authFetch` — an authenticated HTTP client. Both are explained further in [10 — The N3O Bridge Pattern](./10-the-n3o-bridge-pattern.md).
- **`consumeContext`** — requests a named context object from the Umbraco context system. Think of it as resolving a service from a scoped DI container — the "scope" is the element's position in the DOM tree. `UMB_DOCUMENT_WORKSPACE_CONTEXT` provides information about the document being edited.
- **`this.observe(observable, callback, key)`** — subscribes to an RxJS observable (Umbraco's reactive value type). Fires the callback whenever the value changes. The `key` string is a deduplication identifier. This is Umbraco's equivalent of event subscriptions or `INotifyPropertyChanged` listeners.
- **`super.connectedCallback()` / `super.disconnectedCallback()`** — required because the mixin bases have their own lifecycle logic (subscribing/unsubscribing contexts and observables). Omitting `super` calls would leak subscriptions.
- **Property setters on `content` / `settings`** — the Umbraco block editor sets these properties on the element directly (JavaScript property assignment). The setter calls `#onDataChanged()` which debounces a re-fetch from the C# preview API.

---

## Summary — custom element anatomy

```
┌──────────────────────────────────────┐
│  <n3o-my-element>   ← host element   │
│  ┌────────────────────────────────┐  │
│  │  Shadow Root                   │  │
│  │  ┌──────────────────────────┐  │  │
│  │  │  <div id="mount">        │  │  │
│  │  │  ← React mounts here     │  │  │
│  │  └──────────────────────────┘  │  │
│  │  adoptedStyleSheets: [sheet]   │  │
│  └────────────────────────────────┘  │
└──────────────────────────────────────┘
```

- The **host element** (`<n3o-my-element>`) is what the backoffice inserts.
- The **shadow root** encapsulates markup and styles.
- The **mount div** is a plain DOM node that React treats as its root.
- **React renders inside** the mount div — it never knows (or cares) that it is inside a shadow root.
- **Lifecycle methods** on the custom element class control when React is created (`connectedCallback`) and destroyed (`disconnectedCallback`).

---

**Next:** [07 — Lit](./07-lit.md)
