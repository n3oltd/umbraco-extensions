# N3O.Umbraco.TextResourceEditor

**Package name:** `n3o-umbraco-textresourceeditor`
**Source (Apps folder):** `src/Plugins/TextResourceEditor/N3O.Umbraco.TextResourceEditor.StaticAssets/Apps/`
**Build output:** `src/Plugins/TextResourceEditor/N3O.Umbraco.TextResourceEditor.StaticAssets/wwwroot/App_Plugins/N3O.Umbraco.TextResourceEditor/`
**Manifest:** `wwwroot/App_Plugins/N3O.Umbraco.TextResourceEditor/umbraco-package.json`

---

## What it is

The Text Resource Editor is a **property editor UI** for Umbraco content types. It presents a list of keyed text entries where each entry has a read-only source string (the default text, populated by the backend) and an editable custom override. Editors can delete entries from the list and type their own replacement text into each entry's input field.

The property stores an array of `{ source, custom }` objects. The backend populates `source` values (e.g. from a template or resource file); the editor writes back the `custom` overrides that the rendering layer uses in preference to the defaults.

Extension type: `propertyEditorUi`
Served from: `/App_Plugins/N3O.Umbraco.TextResourceEditor/` (static web assets via ASP.NET Core RCL)

Unlike the SERP editor, the Text Resource Editor makes **no authenticated backend calls** — all data arrives via the property `value` prop from Umbraco's content model. There is no `UmbAuthFetchMixin` and no `additionalExternals` in the Vite config.

---

## Files

| File | Role |
|------|------|
| `package.json` | npm package manifest for this app |
| `tsconfig.json` | TypeScript compiler config; extends shared base from `@n3o/build` |
| `vite.config.ts` | Vite build config; uses the shared `n3oPluginConfig` preset |
| `src/text-resource-editor.ts` | **The web-component shell** — the entry point Umbraco loads |
| `src/text-resource-editor-app.tsx` | **The React app** — all UI and state management |
| `src/text-resource-editor-app.css` | CSS for the editor layout |
| `src/uui-react.d.ts` | TypeScript ambient declarations allowing `<uui-box>` etc. inside JSX |
| `wwwroot/.../umbraco-package.json` | Umbraco manifest — tells Umbraco this file is a `propertyEditorUi` |

---

## End-to-end flow

This section traces the full lifecycle from Umbraco startup to the editor appearing on screen and saving a value. For the general bridge-pattern architecture, see [../concepts/10-the-n3o-bridge-pattern.md](../concepts/10-the-n3o-bridge-pattern.md).

### 1. Umbraco discovers the manifest

At startup Umbraco scans every `App_Plugins/*/umbraco-package.json` and registers extensions. The manifest declares:

```json
{
    "type": "propertyEditorUi",
    "alias": "N3O.Umbraco.TemplateTextEditor",
    "element": "/App_Plugins/N3O.Umbraco.TextResourceEditor/text-resource-editor.js"
}
```

The alias `N3O.Umbraco.TemplateTextEditor` must match the `[DataEditor]` alias on the C# backend class. Note that the alias and the package/folder name differ intentionally: the package is named `TextResourceEditor` but the alias references `TemplateTextEditor` (the historical name). For the alias rule, see [../concepts/09-umbraco-backoffice-extensions.md](../concepts/09-umbraco-backoffice-extensions.md).

### 2. Browser loads `text-resource-editor.js` and registers the custom element

When the module is first imported, the `@customElement('n3o-text-resource-editor')` decorator fires as a module-level side effect and registers the `<n3o-text-resource-editor>` tag. This is the same pattern as every bridge-pattern shell in this repo.

### 3. Umbraco instantiates the element

When an editor opens a document with a text resource property, Umbraco creates an instance of `<n3o-text-resource-editor>`. The constructor attaches a shadow root and creates the React mount point:

```typescript
constructor() {
    super();
    const shadow = this.attachShadow({ mode: 'open' });
    this.#mount = document.createElement('div');
    shadow.appendChild(this.#mount);
}
```

See [../concepts/06-web-components-and-shadow-dom.md](../concepts/06-web-components-and-shadow-dom.md) for shadow DOM isolation.

### 4. Umbraco sets `value` and `config`

Umbraco calls the element's setters with the persisted value (an array of `TextResourceEntry`) and the config (prevalues). The `set value` setter stores the array and calls `#render()`:

```typescript
set value(value: TextResourceEntry[] | undefined) {
    this.#value = Array.isArray(value) ? value : [];
    this.#render();
}
```

`Array.isArray(value) ? value : []` guards against Umbraco passing `undefined` or a non-array — in that case the editor starts empty rather than crashing. The `set config` setter accepts and discards the config because this editor has no prevalues:

```typescript
public set config(_config: UmbPropertyEditorConfigCollection | undefined) {}
```

The underscore prefix on `_config` is a TypeScript convention for "this parameter is intentionally unused."

### 5. `connectedCallback` fires, React mounts

```typescript
connectedCallback(): void {
    this.#root ??= createRoot(this.#mount);
    this.#render();
}
```

`??=` creates the React root if it does not exist. Note that this shell does **not** call `super.connectedCallback?.()` because it does not extend `UmbElementMixin` — it extends `HTMLElement` directly. There is no Umbraco context consumption in this editor (no auth context needed), so `UmbElementMixin` is not required.

### 6. React renders the list

`#render()` calls `this.#root?.render(createElement(TextResourceEditorApp, { value, onChange }))`. The `onChange` callback is defined inline:

```typescript
onChange: (value: TextResourceEntry[]) => {
    this.#value = value;
    this.dispatchEvent(new UmbPropertyValueChangeEvent());
},
```

As in the SERP editor, the callback stores the new array on the shell and fires the Umbraco change event to persist the new value. React diffs the previous render against the new one and updates only the changed DOM nodes.

### 7. Editing a value

When the user types in a custom text `<input>`, the `updateCustom` handler fires:

```typescript
function updateCustom(source: string, custom: string): void {
    onChange(value.map((entry) =>
        entry.source === source ? { ...entry, custom } : entry
    ));
}
```

`value.map(...)` creates a **new array** where the matching entry is replaced by a new object (`{ ...entry, custom }` — spread all existing fields, override `custom`). The original array is not mutated. This is idiomatic React state management: always produce new objects rather than mutating existing state, so React can detect the change by reference comparison.

`onChange` is the prop callback, which calls back to the shell, which updates `#value` and fires `UmbPropertyValueChangeEvent`.

### 8. Deleting an entry

The delete flow uses a two-step confirmation to prevent accidental data loss:

1. User clicks `[x]` → `requestDelete(entry.source)` is called → `setPendingDelete(entry.source)` marks the entry.
2. React re-renders: the row now shows "Delete this entry? Yes No" buttons.
3. User clicks "Yes" → `confirmDelete(entry.source)` → `onChange(value.filter(...))` removes the entry from the array.
4. User clicks "No" → `cancelDelete()` → `setPendingDelete(null)` restores normal rendering.

`value.filter((entry) => entry.source !== source)` creates a new array with the matching entry removed. `source` is used as the stable identifier for each entry.

### 9. `disconnectedCallback` — cleanup

```typescript
disconnectedCallback(): void {
    this.#root?.unmount();
    this.#root = undefined;
}
```

React is unmounted and the root handle is cleared, allowing garbage collection.

---

## File-by-file walkthrough

### `package.json`

```json
{
    "name": "n3o-umbraco-textresourceeditor",
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

Identical structure to `serpeditor`'s `package.json` — see the [SerpEditor package.json walkthrough](serpeditor.md#packagejson) for a full explanation of each field.

The only difference that matters architecturally: this app has no dependency on `@n3o/backoffice-core` because it makes no authenticated API calls. The dependency list is therefore even simpler — just `@n3o/build` for the build tooling.

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

Identical to the SerpEditor `tsconfig.json`. Inherits all compiler settings from the shared base; adds `"jsx": "react-jsx"` to enable the automatic JSX transform for `.tsx` files. See [../concepts/05-vite-and-the-build.md](../concepts/05-vite-and-the-build.md).

---

### `vite.config.ts`

```typescript
import { n3oPluginConfig } from '@n3o/build';

export default n3oPluginConfig({
    entries: { 'text-resource-editor': 'src/text-resource-editor.ts' },
    outDir: '../wwwroot/App_Plugins/N3O.Umbraco.TextResourceEditor',
    react: true,
});
```

Uses the shared `n3oPluginConfig` preset. Compared to the SERP editor's config there is one notable difference: **no `additionalExternals`**. This editor does not use `@n3o/backoffice-core`, so there is nothing extra to exclude from the bundle. The `react: true` flag still excludes `react`, `react-dom`, `react-dom/client`, and `react/jsx-runtime` as externals (resolved at runtime via the import map from `N3O.Umbraco.ReactRuntime`).

For a full explanation of `n3oPluginConfig`, library mode, and externals, see [../concepts/05-vite-and-the-build.md](../concepts/05-vite-and-the-build.md).

---

### `src/text-resource-editor.ts` — the web-component shell

**Imports**

```typescript
import { customElement } from '@umbraco-cms/backoffice/external/lit';
import {
    UmbPropertyValueChangeEvent,
    type UmbPropertyEditorConfigCollection,
    type UmbPropertyEditorUiElement,
} from '@umbraco-cms/backoffice/property-editor';
import { createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { TextResourceEditorApp, type TextResourceEntry } from './text-resource-editor-app';
```

Compared to `serp-editor.ts`, two imports are absent: `UmbElementMixin` and anything from `@n3o/backoffice-core`. Because this editor makes no authenticated calls and needs no Umbraco context consumption, neither the element mixin nor the auth mixin is required.

**Class declaration**

```typescript
@customElement(elementName)
export class N3oTextResourceEditorElement
    extends HTMLElement
    implements UmbPropertyEditorUiElement
```

The class extends `HTMLElement` directly — the simplest possible base. Compare with the SERP editor which extends `UmbAuthFetchMixin(UmbElementMixin(HTMLElement))`. Both implement `UmbPropertyEditorUiElement`, which is the interface Umbraco checks to confirm a custom element can act as a property editor.

**`set value` guard**

```typescript
set value(value: TextResourceEntry[] | undefined) {
    this.#value = Array.isArray(value) ? value : [];
    this.#render();
}
```

`Array.isArray(...)` is a runtime type guard. In TypeScript you can declare the parameter type as `TextResourceEntry[]`, but that is a compile-time guarantee only — Umbraco's property system may pass `undefined` or a non-array at runtime (e.g. if the property has no saved value). The `? value : []` fallback ensures the React component always receives a valid array.

**`set config` — accepting but ignoring**

```typescript
public set config(_config: UmbPropertyEditorConfigCollection | undefined) {}
```

The `UmbPropertyEditorUiElement` interface requires a `config` setter to be present (it is part of the contract). This editor has no prevalues, so the setter accepts the argument and does nothing. The parameter is prefixed `_` to tell the TypeScript compiler "intentionally unused" — without the underscore, `noUnusedParameters: true` in the base tsconfig would make this a compile error.

**`connectedCallback` — no `super.connectedCallback`**

```typescript
connectedCallback(): void {
    this.#root ??= createRoot(this.#mount);
    this.#render();
}
```

Because this class extends plain `HTMLElement` (not `UmbElementMixin(...)`), there is no `super.connectedCallback` to call. The SERP editor calls `super.connectedCallback?.()` because `UmbElementMixin` uses it to initialise context observation. If you see `super.connectedCallback?.()` in a shell, it means the element uses Umbraco context features; its absence here is correct and intentional.

---

### `src/text-resource-editor-app.tsx` — the React app

**Interfaces**

```typescript
export interface TextResourceEntry {
    source: string;
    custom: string | null | undefined;
}

interface TextResourceEditorAppProps {
    value: TextResourceEntry[];
    onChange: (value: TextResourceEntry[]) => void;
}
```

`TextResourceEntry` is exported because the shell needs to type `#value` and the `onChange` callback. `custom` is `string | null | undefined` (a union type — the TypeScript equivalent of `string?` in nullable-context C#) to reflect that the persisted JSON may have no override, a null override, or an empty string — all of which mean "use the source text."

There is no `authFetch` prop here. This is the whole difference in prop surface between an editor that calls the backend and one that does not.

**Early return**

```typescript
if (!value.length) {
    return null;
}
```

`return null` in a React component means "render nothing." This handles the case where the property has no entries — no DOM is produced, which is cleaner than rendering an empty `<uui-box>`. In TypeScript `!value.length` is `true` when `value` is an empty array (`length === 0`).

**State: `pendingDelete`**

```typescript
const [pendingDelete, setPendingDelete] = useState<string | null>(null);
```

`useState` is called with a type parameter `<string | null>` (TypeScript generics — equivalent to `State<string?>` in a hypothetical C# API). The state holds the `source` key of the entry the user wants to delete, or `null` if no delete is in progress.

Only one entry can be in the pending-delete state at a time — storing the key (a string) rather than a boolean is what enables this without needing a separate state variable for each row.

**`updateCustom` — immutable array update**

```typescript
function updateCustom(source: string, custom: string): void {
    onChange(value.map((entry) =>
        entry.source === source ? { ...entry, custom } : entry
    ));
}
```

`value.map(...)` iterates the array and returns a new array. For the matching entry, `{ ...entry, custom }` creates a new object that copies all properties from `entry` (the spread `...entry`) then overwrites `custom` with the new value. Entries that do not match are returned unchanged (same object reference).

This is the React immutable-update pattern. Do not write `entry.custom = custom` — that mutates the existing object in place. React uses reference equality to detect changes; mutating an object without replacing it means React sees the same reference and may skip the re-render, leading to the UI showing stale data.

In C# terms this is similar to using `record with { Custom = custom }` to create a modified copy rather than modifying a mutable object.

**`confirmDelete` — immutable array filter**

```typescript
function confirmDelete(source: string): void {
    setPendingDelete(null);
    onChange(value.filter((entry) => entry.source !== source));
}
```

`value.filter(...)` returns a new array containing only entries where `entry.source !== source` — i.e. every entry except the deleted one. Again, no mutation.

**JSX — the list**

```tsx
{value.map((entry) => (
    <div className="row-wrapper" key={entry.source}>
        ...
    </div>
))}
```

`value.map(...)` inside JSX renders a list of elements. The `key` prop is **mandatory** for list items in React and must be stable and unique within the list. React uses `key` to reconcile the list across re-renders — it matches old rendered items to new ones to decide what to add, update, or remove. If keys are unstable (e.g. using array index) or missing, React may re-render or re-mount items unnecessarily, causing visual glitches or loss of input focus.

`entry.source` is the stable key here. It is the property's "source text" identifier that the backend sets and never changes. Using the array index (`key={i}`) would be incorrect: if an entry is deleted, all subsequent indices shift by one, causing React to treat them as different elements and re-mount their inputs.

**Two-step delete: accessibility**

```tsx
<button
    type="button"
    className="delete"
    aria-label={`Delete ${entry.source}`}
    onClick={() => requestDelete(entry.source)}
>
    x
</button>
```

Several details here:

- **`<button type="button">`** — `type="button"` is required on any `<button>` inside a form to prevent the button from submitting the form. Even though there is no explicit `<form>` element here, the backoffice document editor wraps property editors in a form context. Without `type="button"`, clicking would submit the form and navigate away instead of triggering the click handler. This is a frequent HTML gotcha.
- **`aria-label`** — the button's visible text is just `x`, which is not meaningful to a screen reader. `aria-label` provides an accessible label (`"Delete <source text>"`) so screen reader users understand the button's purpose. This is the HTML equivalent of the `AutomationProperties.Name` property in XAML. See [../concepts/09-umbraco-backoffice-extensions.md](../concepts/09-umbraco-backoffice-extensions.md) for accessibility context.
- **`<button>` not `<a>`** — a `<button>` is the correct element for an action that modifies state. An `<a>` (anchor) is for navigation. Using `<a href="#">` as a button is a common anti-pattern that breaks keyboard navigation and screen reader behaviour. See [../concepts/09-umbraco-backoffice-extensions.md](../concepts/09-umbraco-backoffice-extensions.md).

**Native `<input>` instead of `<uui-input>`**

```tsx
<input
    type="text"
    className="custom"
    value={entry.custom ?? ''}
    onChange={(e) => updateCustom(entry.source, e.currentTarget.value)}
/>
```

This is a plain HTML `<input>` element, not the `<uui-input>` Umbraco UI Library component. This is intentional: interactive `uui-*` form controls break when rendered by React in Umbraco 17 due to a conflict between UUI's `FormControlMixin` and React's DOM reconciliation. See the Gotchas section and [../concepts/10-the-n3o-bridge-pattern.md](../concepts/10-the-n3o-bridge-pattern.md).

`value={entry.custom ?? ''}` uses `?? ''` because `entry.custom` is `string | null | undefined`. React's controlled input requires the `value` prop to be a string (not `null` or `undefined`); passing `null` would cause React to switch the input to uncontrolled mode, producing a warning and unpredictable behaviour.

`e.currentTarget.value` reads from `currentTarget` (the element the handler is attached to) rather than `e.target` (the element the event originated from). For simple cases both are the same; using `currentTarget` is the safer habit for delegated event patterns.

**CSS injection via `?inline`**

```tsx
<style>{styles}</style>
```

Same pattern as the SERP editor: CSS is imported as a string with `?inline` and injected as a `<style>` element inside the shadow root. See [../concepts/06-web-components-and-shadow-dom.md](../concepts/06-web-components-and-shadow-dom.md) for why global stylesheets cannot reach inside a shadow root.

---

### `src/text-resource-editor-app.css`

```css
.n3o-text-resource-editor .row-wrapper { margin-bottom: 40px; width: 100%; }
.n3o-text-resource-editor .row-1 { display: block; width: 90%; }
.n3o-text-resource-editor .row-2 { display: block; width: 90%; }
.n3o-text-resource-editor .delete { cursor: pointer; }
.n3o-text-resource-editor .text { font-weight: bold; }
.n3o-text-resource-editor .custom { width: 100%; margin-top: 10px; }
```

All rules are scoped under `.n3o-text-resource-editor` — the class applied to the container `<div>` in the JSX. This provides an extra namespacing layer (even though shadow DOM already provides isolation) and makes the rules easy to identify when reading or debugging.

`.delete { cursor: pointer; }` changes the mouse cursor to a hand pointer when hovering over the delete button. Native `<button>` elements do not have pointer cursor by default (unlike `<a>` elements), so this is needed for the expected visual affordance.

`.custom { width: 100%; }` makes each override input span the full width of its container, providing a generous editing area for potentially long text.

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

Identical to `serp-editor`'s `uui-react.d.ts`. Augments React's JSX type definitions to allow `<uui-box>` in `.tsx` files. The file deliberately declares only display-only elements; interactive form controls are omitted because they fail at runtime. See the Gotchas section and [../concepts/10-the-n3o-bridge-pattern.md](../concepts/10-the-n3o-bridge-pattern.md).

For a full explanation of `declare module` and declaration merging, see the SerpEditor's [uui-react.d.ts walkthrough](serpeditor.md#srcuui-reactdts).

---

### `wwwroot/App_Plugins/N3O.Umbraco.TextResourceEditor/umbraco-package.json`

```json
{
    "$schema": "http://json.schemastore.org/umbraco-package.json",
    "id": "N3O.Umbraco.TextResourceEditor",
    "name": "N3O Text Resource Editor",
    "version": "17.0.0",
    "extensions": [
        {
            "type": "propertyEditorUi",
            "alias": "N3O.Umbraco.TemplateTextEditor",
            "name": "N3O Text Resource Editor",
            "element": "/App_Plugins/N3O.Umbraco.TextResourceEditor/text-resource-editor.js",
            "meta": {
                "label": "N3O Text Resource Editor",
                "icon": "icon-document",
                "group": "common",
                "propertyEditorSchemaAlias": "N3O.Umbraco.TemplateTextEditor"
            }
        }
    ]
}
```

Key fields:

| Field | Value | Explanation |
|-------|-------|-------------|
| `id` | `"N3O.Umbraco.TextResourceEditor"` | Unique package identifier across all installed packages |
| `type` | `"propertyEditorUi"` | Extension type — a property editor UI |
| `alias` | `"N3O.Umbraco.TemplateTextEditor"` | **Must match** the C# `[DataEditor]` alias. Note: alias is `TemplateTextEditor`, package name is `TextResourceEditor` |
| `element` | `"/App_Plugins/N3O.Umbraco.TextResourceEditor/text-resource-editor.js"` | URL of the built JS file |
| `propertyEditorSchemaAlias` | `"N3O.Umbraco.TemplateTextEditor"` | Reinforces the backend schema link |
| `icon` | `"icon-document"` | Icon shown in the Umbraco data types list |

**Alias vs package name mismatch.** Notice that the `alias` is `N3O.Umbraco.TemplateTextEditor` while the package `id` and folder name are `N3O.Umbraco.TextResourceEditor`. This is intentional: the alias is the historical name from the C# `[DataEditor]` attribute, which was set when the feature was called "Template Text Editor". Renaming the alias would require migrating every existing data type record in the Umbraco database. The package/folder name was updated to be more descriptive without changing the backend contract. The lesson: the `alias` in `umbraco-package.json` is a contract with the database — change it only with a corresponding database migration. For the alias rule, see [../concepts/09-umbraco-backoffice-extensions.md](../concepts/09-umbraco-backoffice-extensions.md).

---

## Concepts demonstrated

| Concept | Where demonstrated |
|---------|--------------------|
| The N3O bridge pattern (shell + React, no auth) | Entire shell/app split without `UmbAuthFetchMixin` | Link: [../concepts/10-the-n3o-bridge-pattern.md](../concepts/10-the-n3o-bridge-pattern.md) |
| Umbraco property editor UI extension | `umbraco-package.json` + `implements UmbPropertyEditorUiElement` | Link: [../concepts/09-umbraco-backoffice-extensions.md](../concepts/09-umbraco-backoffice-extensions.md) |
| Native HTML controls instead of `uui-*` form controls | `<input>`, `<button>` in `text-resource-editor-app.tsx` | Link: [../concepts/10-the-n3o-bridge-pattern.md](../concepts/10-the-n3o-bridge-pattern.md) |
| Editing a list in React state (immutable updates) | `updateCustom` with `value.map(...)`, `confirmDelete` with `value.filter(...)` | Link: [../concepts/08-react.md](../concepts/08-react.md) |
| Stable list keys | `key={entry.source}` on each row | Link: [../concepts/08-react.md](../concepts/08-react.md) |
| Two-step confirmation UX | `pendingDelete` state variable driving conditional render | Link: [../concepts/08-react.md](../concepts/08-react.md) |
| `button` vs `anchor` for actions | `<button type="button">` for delete | Explained in this doc |
| `aria-label` for icon-only buttons | `aria-label={"Delete " + entry.source}` | Explained in this doc |
| Shadow DOM + CSS `?inline` injection | `attachShadow` + `import styles from '...css?inline'` | Link: [../concepts/06-web-components-and-shadow-dom.md](../concepts/06-web-components-and-shadow-dom.md) |
| Intentionally unused setter parameter | `set config(_config: ...)` | Explained in this doc |

---

## Gotchas

**Do not use interactive `uui-*` controls inside React.**
`uui-input`, `uui-textarea`, `uui-button`, `uui-toggle`, `uui-select`, `uui-form-layout-item`, and `uui-button-group` fail to mount when React renders them in Umbraco 17 — the components do not appear and the browser console shows an error. Use native HTML elements (`<input>`, `<textarea>`, `<button>`, `<select>`) inside React. Only display-only elements (`uui-box`, `uui-icon`, `uui-label`, `uui-loader`) work correctly. The file `src/uui-react.d.ts` documents this by deliberately omitting interactive elements from the type declarations. See [../concepts/10-the-n3o-bridge-pattern.md](../concepts/10-the-n3o-bridge-pattern.md) for the root cause.

**Never pass `null` or `undefined` to a controlled `<input value={...}>`.**
React switches an input between controlled and uncontrolled mode based on whether `value` is a string or `null`/`undefined`. Switching from one mode to the other logs a warning and can cause the input to display stale text. Always provide a fallback: `value={entry.custom ?? ''}`.

**List keys must be stable — do not use array index as `key`.**
`key={entry.source}` works because `source` is the stable identifier for each entry. If you replaced it with `key={i}` (the array index from `.map((entry, i) => ...)`) and then deleted the first entry, React would see the same key for what was previously the second entry and might preserve its DOM node (including input focus and cursor position) rather than cleaning up correctly.

**`Array.isArray` guard in `set value`.**
The `Array.isArray(value) ? value : []` guard in the shell's setter is load-bearing. Umbraco may set `value` to `undefined` if the property has never been saved, or to a non-array in some edge cases. Without the guard, passing a non-array to React's `value.map(...)` throws a `TypeError` and the editor fails to render entirely.

**The `alias` is a database contract.**
The `alias: "N3O.Umbraco.TemplateTextEditor"` in `umbraco-package.json` differs from the folder and package name (`TextResourceEditor`). Do not "fix" this mismatch by changing the alias — doing so would break all existing content that references data types using the old alias. A change requires a C# migration to rename the stored editor alias in the Umbraco database.

**`type="button"` on every `<button>` inside a property editor.**
Without `type="button"`, clicking a button inside the property editor form can trigger form submission, causing the backoffice to navigate away from the content item. Always specify `type="button"` on any button that is not explicitly intended to submit a form.

**The delete confirmation state is local to the React component, not the shell.**
`pendingDelete` lives in `useState` inside `TextResourceEditorApp`. If Umbraco re-renders the shell (calling `set value` with new data), `#render()` is called again. React reconciles the existing component tree with the new props rather than unmounting and remounting, so `pendingDelete` state is preserved across external value updates. If React did fully remount the component (e.g. `key` on the root element changed), the confirmation state would be lost. This is correct behaviour — but be aware of it if you add a `key` prop to the top-level React element.
