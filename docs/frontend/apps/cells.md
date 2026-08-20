# Cells (Property Editor UI)

**Source app directory:**
`src/Plugins/Cells/N3O.Umbraco.Cells.StaticAssets/Apps/`

**Manifest (built output):**
`src/Plugins/Cells/N3O.Umbraco.Cells.StaticAssets/wwwroot/App_Plugins/N3O.Umbraco.Cells/umbraco-package.json`

---

## 1. What it is

The Cells plugin is a **property editor UI** — the frontend half of a custom Umbraco data type. It renders a spreadsheet-style grid using [Handsontable](https://handsontable.com/) (a commercial JavaScript grid library) for editing tabular data on a content node. The stored value is a 2-D JSON array; the grid appearance is driven by a `gridConfiguration` JSON prevalue set on the data type.

C# analogy: a property editor is like a custom ASP.NET model binder plus editor template, split into a backend `IDataEditor` (registered in C#) and a frontend UI (registered here via `umbraco-package.json`).

| Attribute | Value |
|-----------|-------|
| Extension type | `propertyEditorUi` (+ `propertyEditorSchema`) |
| Umbraco alias | `N3O.Umbraco.Cells` |
| Custom element tag | `n3o-cells` |
| Built output file | `App_Plugins/N3O.Umbraco.Cells/n3o-cells.js` |
| Third-party dependency bundled | `handsontable@12.3.0` |
| CSS strategy | Handsontable's CSS inlined via `?inline`; no separate `.css` file |

**How it is served:** The `N3O.Umbraco.Cells.StaticAssets` project is a Razor Class Library (RCL). Its `wwwroot/App_Plugins/` folder is published as static web assets.

---

## 2. Files

| File | Role |
|------|------|
| `package.json` | npm metadata; declares `handsontable` as a runtime dependency |
| `tsconfig.json` | TypeScript config; extends shared base; adds `paths` alias for Handsontable types |
| `vite.config.ts` | Vite config via `n3oPluginConfig`; Handsontable IS bundled, React is NOT |
| `src/n3o-cells.ts` | **Web-component shell** implementing the Umbraco property editor contract |
| `src/n3o-cells-app.tsx` | **React component** wrapping the Handsontable instance |
| `src/uui-react.d.ts` | Ambient JSX declarations for UUI tags used by this app |
| `wwwroot/App_Plugins/N3O.Umbraco.Cells/umbraco-package.json` | Umbraco extension manifest |
| `wwwroot/App_Plugins/N3O.Umbraco.Cells/n3o-cells.js` | **Built output** (produced by Vite) |

There is **no `n3o-cells-app.css`**. Handsontable ships its own CSS (`handsontable/dist/handsontable.full.min.css`), which is imported with the `?inline` Vite suffix directly in the TSX file and injected as a `<style>` element inside the React render output.

---

## 3. End-to-end flow

The shell + React bridge is the same pattern as the Welcome Dashboard — see [../concepts/10-the-n3o-bridge-pattern.md](../concepts/10-the-n3o-bridge-pattern.md). The key difference is that this shell participates in Umbraco's **property editor contract** for reading and writing values.

1. An editor creates a data type using the "N3O Cells" property editor schema. They set the `gridConfiguration` prevalue (a JSON string describing columns and default data).
2. When an editor opens a content node that uses this data type, Umbraco instantiates `<n3o-cells>` and does two things:
   - Sets `element.value` to the currently stored 2-D array (or `undefined` if the property is empty).
   - Sets `element.config` to a `UmbPropertyEditorConfigCollection` holding the prevalues (including `gridConfiguration`).
3. The shell's `set value(...)` and `set config(...)` setters store the data internally and call `#render()`, which renders the React component with the latest `value`, `gridConfiguration`, and `onChange` props.
4. The React component (`N3oCellsApp`) creates a Handsontable instance via a `useEffect` hook. The grid displays the data. When the user edits a cell, Handsontable fires `afterChange`; the callback calls `onChangeRef.current(hot.getData())`, which flows back to the shell's `onChange` prop handler.
5. The shell's `onChange` handler updates `this.#value` and dispatches `new UmbPropertyValueChangeEvent()`. Umbraco listens for this event and reads back `element.value` to stage the new value for saving.

---

## 4. File-by-file

### `package.json`

```json
{
    "name": "n3o-umbraco-cells",
    "version": "1.0.0",
    "private": true,
    "type": "module",
    "scripts": {
        "build": "tsc --noEmit && vite build",
        "watch": "vite build --watch"
    },
    "dependencies": {
        "handsontable": "12.3.0"
    },
    "devDependencies": {
        "@n3o/build": "*"
    }
}
```

Compared to the Welcome Dashboard, there is a new `dependencies` section listing `handsontable`. This is a **runtime dependency** (not a dev-dependency) because the library code is bundled into the output JS file by Vite and shipped to the browser. Dev-dependencies are only used during the build. C# analogy: this is like a `<PackageReference>` vs a `<PackageReference PrivateAssets="All">`.

Handsontable `12.3.0` is pinned exactly (no `^` range prefix) because minor versions of Handsontable sometimes change grid behaviour.

### `tsconfig.json`

```json
{
    "extends": "@n3o/build/tsconfig",
    "compilerOptions": {
        "jsx": "react-jsx",
        "paths": {
            "handsontable": [
                "./node_modules/handsontable/index.d.ts",
                "../../../../node_modules/handsontable/index.d.ts"
            ]
        }
    },
    "include": ["src"]
}
```

The `paths` entry is a TypeScript path alias. It maps the bare specifier `handsontable` to the type declaration file, with a fallback to the workspace root's `node_modules`. This is needed because npm workspaces hoist packages to the workspace root when possible — TypeScript needs to know both locations to find the types regardless of where npm chose to install. C# analogy: like having both a local project reference and a NuGet reference fallback.

### `vite.config.ts`

```typescript
import { n3oPluginConfig } from '@n3o/build';

export default n3oPluginConfig({
    entries: { 'n3o-cells': 'src/n3o-cells.ts' },
    outDir: '../wwwroot/App_Plugins/N3O.Umbraco.Cells',
    react: true,
});
```

This is identical in structure to the Welcome Dashboard config. The critical difference is what is **not** in `additionalExternals`: `handsontable` is absent, so Vite/Rollup **bundles** Handsontable into `n3o-cells.js`. Handsontable is not available on Umbraco's import map, so it cannot be resolved externally at runtime. React and all `@umbraco-cms/*` are still external (resolved via import map).

The resulting `n3o-cells.js` is significantly larger than `welcome-dashboard.js` because it contains Handsontable's full source.

### `src/n3o-cells.ts` — the web-component shell

```typescript
import { customElement } from '@umbraco-cms/backoffice/external/lit';
import {
    UmbPropertyValueChangeEvent,
    type UmbPropertyEditorConfigCollection,
    type UmbPropertyEditorUiElement,
} from '@umbraco-cms/backoffice/property-editor';
import { createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { N3oCellsApp, type CellsValue } from './n3o-cells-app';

const elementName = 'n3o-cells';

@customElement(elementName)
export class N3oCellsElement extends HTMLElement implements UmbPropertyEditorUiElement {
    // ...
}
```

**`implements UmbPropertyEditorUiElement`** — this is the Umbraco property editor contract. Think of it as implementing a C# interface. Umbraco expects the element to expose:
- A `value` getter/setter — the property's stored value.
- A `config` setter — the data type's prevalues (configuration set in the Umbraco backoffice data type editor).

The constructor is slightly different from the Welcome Dashboard:

```typescript
constructor() {
    super();
    const shadow = this.attachShadow({ mode: 'open' });
    const style = document.createElement('style');
    style.textContent = ':host { display: block; width: 100%; }';
    shadow.appendChild(style);
    this.#mount = document.createElement('div');
    shadow.appendChild(this.#mount);
}
```

It adds an inline `<style>` element directly to the shadow root in the constructor (before React mounts). This sets the minimum layout styles on `:host`. Handsontable needs the element to have a defined block size to render correctly.

**The `value` getter/setter:**

```typescript
get value(): CellsValue {
    return this.#value;
}

set value(value: CellsValue) {
    this.#value = value;
    this.#render();
}
```

Umbraco sets `element.value = currentStoredValue` before the element is connected. Whenever the setter is called, `#render()` pushes the new value into React as a prop. Umbraco reads back `element.value` (via the getter) after `UmbPropertyValueChangeEvent` fires.

**The `config` setter:**

```typescript
public set config(config: UmbPropertyEditorConfigCollection | undefined) {
    this.#gridConfiguration = JSON.parse(
        config?.getValueByAlias('gridConfiguration') ?? '{}',
    ) as Record<string, unknown>;
    this.#render();
}
```

`UmbPropertyEditorConfigCollection` is an Umbraco type that wraps an array of prevalue objects. `getValueByAlias('gridConfiguration')` finds the prevalue whose alias matches, returning the raw string value. The `??` null-coalescing operator (same semantics as C# `??`) provides an empty object as fallback. The result is parsed from JSON and stored as `#gridConfiguration`.

**The `#render()` private method:**

```typescript
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

This is called whenever value or config changes. It passes the current state as props to the React component. The `onChange` callback is the data flow back from React to Umbraco:

1. User edits a cell.
2. Handsontable fires `afterChange`.
3. `onChangeRef.current(hot.getData())` is called.
4. This `onChange` prop handler runs: stores the new value in `this.#value` and dispatches `UmbPropertyValueChangeEvent`.
5. Umbraco hears the event and calls `element.value` getter to get the updated data.

`dispatchEvent` is the native browser DOM event API. C# analogy: like raising a .NET event.

### `src/n3o-cells-app.tsx` — the React component

```typescript
import { useEffect, useRef } from 'react';
import Handsontable from 'handsontable';
import handsontableStyles from 'handsontable/dist/handsontable.full.min.css?inline';

export type CellsValue = unknown[][] | undefined;

interface N3oCellsAppProps {
    value: CellsValue;
    gridConfiguration: Record<string, unknown>;
    onChange: (value: unknown[][]) => void;
}
```

`CellsValue = unknown[][]` — a 2-D array of `unknown`. TypeScript's `unknown` is the type-safe alternative to `any`; values of type `unknown` cannot be used without a type guard or assertion. `unknown[][]` means "array of arrays of unknown values."

**`useRef` for the Handsontable container:**

```typescript
const containerRef = useRef<HTMLDivElement>(null);
```

`useRef` creates a ref object whose `.current` property points to a DOM element after the component mounts. C# analogy: roughly like a late-initialized field. The `<div id="grid" ref={containerRef}></div>` in the JSX tells React to set `containerRef.current` to the div element after rendering.

**`useRef` for the `onChange` callback:**

```typescript
const onChangeRef = useRef(onChange);
onChangeRef.current = onChange;
```

This is a subtle but important pattern. `onChange` is a function passed as a prop. If the parent passes a new function identity on each render (common in React), re-creating the Handsontable instance on every render would be expensive. By storing the latest `onChange` in a ref, the Handsontable `afterChange` callback can always call the current version without `onChange` being a dependency of `useEffect`.

**`useEffect` — Handsontable lifecycle:**

```typescript
useEffect(() => {
    const container = containerRef.current;
    if (!container) { return; }

    const data = (value ?? (gridConfiguration.data as unknown[][] | undefined)) as unknown[][] | undefined;

    const globalConfig: Handsontable.GridSettings = {
        licenseKey: 'non-commercial-and-evaluation',
        height: 'auto',
        width: 'auto',
        data: data,
        afterChange: (_change, source) => {
            if (source !== 'loadData') {
                onChangeRef.current(hot.getData() as unknown[][]);
            }
        },
    };

    const hot = new Handsontable(container, { ...gridConfiguration, ...globalConfig });

    return () => {
        hot.destroy();
    };
}, [gridConfiguration]);
```

`useEffect` runs code after React has rendered and updated the DOM. The second argument `[gridConfiguration]` is the **dependency array** — this effect only re-runs when `gridConfiguration` changes (i.e., when the data type's prevalue configuration changes). React will call the cleanup function (`hot.destroy()`) before re-running the effect or when the component unmounts. C# analogy: the cleanup function is like `IDisposable.Dispose`.

`value` is deliberately **not** in the dependency array. The comment explains why: value updates flow through Handsontable's own internal state (the grid has already updated itself via `afterChange`). Making `value` a dependency would destroy and recreate the entire grid on every keypress.

`data = value ?? gridConfiguration.data` — fall back to the data type's configured default data when the property has never been saved (i.e., `value` is `undefined`).

`licenseKey: 'non-commercial-and-evaluation'` — Handsontable requires a license key. This string is the free/eval key for non-commercial use.

`{ ...gridConfiguration, ...globalConfig }` — spread the data-type configuration first (column definitions, etc.) then override with the mandatory settings. C# analogy: like `Dictionary.Merge` where the second dictionary wins on key conflicts.

**Why there is no separate CSS file:**

Handsontable ships its own stylesheet. Rather than emitting it as a separate network request (which would also fail to reach inside the shadow DOM), it is imported with `?inline`:

```typescript
import handsontableStyles from 'handsontable/dist/handsontable.full.min.css?inline';
```

And injected as a `<style>` element in the JSX:

```tsx
return (
    <>
        <div id="grid" ref={containerRef}></div>
        <style>{handsontableStyles}</style>
    </>
);
```

The `<>...</>` is a React Fragment — a way to return multiple sibling elements without a wrapper `<div>`. C# has no direct equivalent; think of it as returning multiple elements from an enumerable.

### `src/uui-react.d.ts`

```typescript
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

This is the same pattern as in WelcomeDashboard but lists more UUI tags. The cells app uses (or is prepared to use) more UUI components for loading states. See the Welcome Dashboard explanation for how module augmentation works.

---

## 5. The `umbraco-package.json` manifest explained

```json
{
    "$schema": "http://json.schemastore.org/umbraco-package.json",
    "id": "N3O.Umbraco.Cells",
    "name": "N3O Cells",
    "version": "17.0.0",
    "extensions": [
        {
            "type": "propertyEditorSchema",
            "alias": "N3O.Umbraco.Cells",
            "name": "N3O Cells",
            "meta": {
                "defaultPropertyEditorUiAlias": "N3O.Umbraco.Cells",
                "settings": {
                    "properties": [
                        {
                            "alias": "gridConfiguration",
                            "label": "Grid Configuration",
                            "propertyEditorUiAlias": "Umb.PropertyEditorUi.TextArea"
                        }
                    ]
                }
            }
        },
        {
            "type": "propertyEditorUi",
            "alias": "N3O.Umbraco.Cells",
            "name": "N3O Cells",
            "element": "/App_Plugins/N3O.Umbraco.Cells/n3o-cells.js",
            "meta": {
                "label": "N3O Cells",
                "icon": "icon-grid",
                "group": "common",
                "propertyEditorSchemaAlias": "N3O.Umbraco.Cells"
            }
        }
    ]
}
```

This manifest registers **two extensions** — a schema and a UI. Understanding why requires a brief explanation of Umbraco's data type architecture:

**`propertyEditorSchema`** defines the backend data contract: what is the data type's alias, what value type does it store, and what prevalues (configuration) does it accept. Think of it as a C# interface or abstract base class for the data type.

| Field | Meaning |
|-------|---------|
| `type: "propertyEditorSchema"` | Registers the data contract in Umbraco's extension registry |
| `alias: "N3O.Umbraco.Cells"` | Unique identifier. The C# `[DataEditor]` attribute on the server side must use the same alias |
| `meta.defaultPropertyEditorUiAlias` | Which UI to use by default when this schema is selected — points to the sibling `propertyEditorUi` |
| `meta.settings.properties[]` | Declares the prevalues (configuration fields) shown in the data type editor. Here, one textarea for `gridConfiguration` |

**`propertyEditorUi`** defines the frontend widget used in the content editor to display and edit property values of this type.

| Field | Meaning |
|-------|---------|
| `type: "propertyEditorUi"` | Registers the visual editor component |
| `alias: "N3O.Umbraco.Cells"` | Must match `meta.defaultPropertyEditorUiAlias` from the schema, and the alias in `UmbPropertyEditorUiElement` |
| `element` | Path to the JS file to load — Umbraco dynamically imports this when a content node with this property type is opened |
| `meta.icon` | Icon shown in the data type picker in the backoffice |
| `meta.group` | Groups the editor in the data type picker list |
| `meta.propertyEditorSchemaAlias` | Links this UI back to its schema |

**Why the alias must match the C# backend:** the C# `[DataEditor("N3O.Umbraco.Cells")]` attribute registers the same alias in the server-side property editor registry. If these do not match, the backoffice loads the UI element but the data type editor will show an error (the schema is not found).

See [../concepts/09-umbraco-backoffice-extensions.md](../concepts/09-umbraco-backoffice-extensions.md) for the full extension type taxonomy.

---

## 6. Concepts demonstrated

| Concept | Where to learn more |
|---------|---------------------|
| Web-component shell and shadow DOM | [../concepts/06-web-components-and-shadow-dom.md](../concepts/06-web-components-and-shadow-dom.md) |
| Umbraco property editor contract (`UmbPropertyEditorUiElement`) | [../concepts/09-umbraco-backoffice-extensions.md](../concepts/09-umbraco-backoffice-extensions.md) |
| `UmbPropertyValueChangeEvent` | [../concepts/09-umbraco-backoffice-extensions.md](../concepts/09-umbraco-backoffice-extensions.md) |
| React `useEffect` and `useRef` | [../concepts/08-react.md](../concepts/08-react.md) |
| Wrapping an imperative library in React | [../concepts/08-react.md](../concepts/08-react.md) |
| Vite `?inline` CSS and library mode | [../concepts/05-vite-and-the-build.md](../concepts/05-vite-and-the-build.md) |
| Bundled vs external dependencies | [../concepts/05-vite-and-the-build.md](../concepts/05-vite-and-the-build.md) |
| N3O bridge pattern | [../concepts/10-the-n3o-bridge-pattern.md](../concepts/10-the-n3o-bridge-pattern.md) |

---

## 7. Gotchas

**The `onChange` ref pattern prevents stale closures.** Handsontable's `afterChange` callback captures the function at initialization time. If `onChange` were referenced directly (without the ref), every grid edit would call the `onChange` from the first render, which may be stale. Always use `onChangeRef.current(...)` inside long-lived callbacks initialized in `useEffect`.

**`source !== 'loadData'` guard prevents a write-on-mount loop.** When Handsontable is created with initial data, it fires `afterChange` with `source = 'loadData'`. Without this guard, the grid would immediately fire `onChange` → `dispatchEvent(UmbPropertyValueChangeEvent)` → Umbraco reads `element.value` → re-sets `element.value` → triggers `#render()` again. The guard breaks this cycle.

**Handsontable must be destroyed in the `useEffect` cleanup.** Handsontable attaches event listeners and creates DOM elements internally. If you do not call `hot.destroy()` in the cleanup, the old grid leaks memory when the effect re-runs (e.g., on config change). React's `useEffect` cleanup function is the correct place for this.

**`value` in the `useEffect` dependency array would cause thrashing.** Each `afterChange` → `onChange` → `#render()` cycle would cause `value` to change → `useEffect` fires → grid destroyed and recreated → grid fires `afterChange` with `loadData` → etc. Deliberately excluding `value` from the dependency array is correct here; the `// eslint-disable-next-line react-hooks/exhaustive-deps` comment acknowledges this intentional bypass of the linting rule.

**Handsontable is bundled — `n3o-cells.js` is large.** Unlike React (external) or Umbraco APIs (external), Handsontable has no presence in Umbraco's import map. It must be bundled. This is a trade-off: a large single file vs. a separate network request for Handsontable. The trade-off was made in favour of the single-file simplicity.

**The `propertyEditorSchema` alias must match the C# `[DataEditor]` alias.** If they diverge, data types break silently — Umbraco will load the element but fail to associate it with the stored value. The memory note at `~/.claude/projects/.../memory/bellissima_property_editor_alias.md` documents this as a known migration pitfall.
