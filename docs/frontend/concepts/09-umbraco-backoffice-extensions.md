# 09 — The Umbraco 17 Backoffice Extension Model

**Prerequisites:** [06 — Web Components and Shadow DOM](./06-web-components-and-shadow-dom.md), [07 — Lit](./07-lit.md), [04 — ES Modules and Import Maps](./04-es-modules-and-import-maps.md).

---

## The backoffice is a client-side web-component application

In Umbraco 13 and earlier, the backoffice was a server-rendered AngularJS application. In Umbraco 17 ("Bellissima") it is a **client-side single-page application** built entirely from web components. The server delivers one HTML page; everything else — every panel, tree, workspace, editor, button, dialog — is a custom HTML element rendered in the browser.

The important consequence: you cannot add UI by writing C# HTML helpers or Razor views. **Custom backoffice UI must be JavaScript (or TypeScript).** You tell Umbraco what to load via a JSON manifest file, and Umbraco's SPA engine inserts your custom element at the right place and time.

---

## The manifest file — `umbraco-package.json`

Every backoffice plugin in this repo ships a file called `umbraco-package.json` inside its `wwwroot/App_Plugins/<id>/` folder. Umbraco scans all `App_Plugins/*/umbraco-package.json` files at application startup and registers whatever it finds.

### Full manifest shape

```json
{
    "$schema": "http://json.schemastore.org/umbraco-package.json",
    "id": "N3O.Umbraco.Cells",
    "name": "N3O Cells",
    "version": "17.0.0",
    "importmap": {
        "imports": {
            "@n3o/backoffice-core": "/App_Plugins/N3O.Umbraco.BackofficeCore/auth-fetch.js"
        }
    },
    "extensions": [
        {
            "type": "propertyEditorUi",
            "alias": "N3O.Umbraco.Cells",
            "name": "N3O Cells",
            "element": "/App_Plugins/N3O.Umbraco.Cells/n3o-cells.js",
            "meta": { ... },
            "conditions": [ ... ]
        }
    ]
}
```

### Top-level fields

| Field | Type | Meaning |
|-------|------|---------|
| `id` | string | Unique identifier for this package. Must be unique across all installed plugins. Convention: `N3O.Umbraco.<PluginName>`. |
| `name` | string | Human-readable label shown in backoffice package management UI. |
| `version` | string | Semantic version string. Used for display and package management only; it does not affect loading order. |
| `importmap` | object | Optional. Contributes entries to the browser's native import map. Umbraco merges all packages' import maps into a single `<script type="importmap">` in the HTML page. See [04 — ES Modules and Import Maps](./04-es-modules-and-import-maps.md). |
| `extensions` | array | The list of extension registrations this package provides. Each item follows an extension-type-specific schema. |

### The `importmap` field

This is not about extensions — it is about **module resolution**. When a plugin writes `import { UmbAuthFetchMixin } from '@n3o/backoffice-core'`, the browser needs to know what URL `@n3o/backoffice-core` maps to. This field is how the mapping gets into the browser.

The `N3O.Umbraco.BackofficeCore` manifest (`src/N3O.Umbraco.Cms/wwwroot/App_Plugins/N3O.Umbraco.BackofficeCore/umbraco-package.json`) contributes:

```json
"importmap": {
    "imports": {
        "@n3o/backoffice-core": "/App_Plugins/N3O.Umbraco.BackofficeCore/auth-fetch.js"
    }
}
```

And `N3O.Umbraco.ReactRuntime` contributes React's own mappings:

```json
"importmap": {
    "imports": {
        "react": "/App_Plugins/N3O.Umbraco.ReactRuntime/react.js",
        "react/jsx-runtime": "/App_Plugins/N3O.Umbraco.ReactRuntime/react-jsx-runtime.js",
        "react-dom": "/App_Plugins/N3O.Umbraco.ReactRuntime/react-dom.js",
        "react-dom/client": "/App_Plugins/N3O.Umbraco.ReactRuntime/react-dom.js"
    }
}
```

This is analogous to the .NET GAC or NuGet package resolution: a central manifest table that tells the runtime "when you see this specifier, load this URL."

---

## Extension fields common to every type

Every entry in the `extensions` array has these base fields:

| Field | Type | Meaning |
|-------|------|---------|
| `type` | string | The extension type. Controls how Umbraco uses this entry. Examples: `"propertyEditorUi"`, `"workspaceView"`, `"dashboard"`, `"condition"`. |
| `alias` | string | A unique string identifier for **this specific extension**. Other parts of the system reference extensions by alias. Must be unique across all plugins. |
| `name` | string | Human-readable name for tooling and dev UI. |
| `element` | string | URL path to the compiled JS file that exports the custom element. Umbraco lazy-loads this file and looks for a custom element registered with `customElements.define`. Not present on all types (e.g. `condition` uses `api` instead). |
| `weight` | number | Optional. Controls ordering when multiple extensions appear in the same slot (lower weight = higher priority / shown first). |
| `meta` | object | Type-specific metadata (labels, icons, pathnames, configuration). |
| `conditions` | array | Optional. Each entry is a condition that must be satisfied before this extension is shown. If all conditions pass, the extension is visible. |

---

## Extension types used in this repo

### 1. `propertyEditorUi`

A **property editor** is the input control that appears in the content editor when a document type property uses a particular data type. In Umbraco 17, each data type has a `propertyEditorUi` (the UI element) and optionally a `propertyEditorSchema` (the data contract).

**Real example — `N3O.Umbraco.Data.ImportDataEditor`:**

`src/Data/N3O.Umbraco.Data.StaticAssets/wwwroot/App_Plugins/N3O.Umbraco.Data.ImportDataEditor/umbraco-package.json`

```json
{
    "id": "N3O.Umbraco.Data.ImportDataEditor",
    "name": "N3O Import Data Editor",
    "version": "17.0.0",
    "extensions": [
        {
            "type": "propertyEditorUi",
            "alias": "N3O.Umbraco.Data.ImportDataEditor",
            "name": "N3O Import Data Editor",
            "element": "/App_Plugins/N3O.Umbraco.Data.ImportDataEditor/import-data-editor.js",
            "meta": {
                "label": "N3O Import Data Editor",
                "icon": "icon-document",
                "group": "common",
                "propertyEditorSchemaAlias": "N3O.Umbraco.Data.ImportDataEditor"
            }
        }
    ]
}
```

- **`meta.label`** — the display name shown in the data type picker in the Settings section.
- **`meta.icon`** — an Umbraco icon name (from the built-in icon set).
- **`meta.group`** — the group this editor appears under in the picker (e.g. `"common"`, `"media"`).
- **`meta.propertyEditorSchemaAlias`** — links this UI to a `propertyEditorSchema` extension that declares the data contract (value type, configuration, default value rules). When present, both the schema and the UI must share the same alias string.

The `element` URL points to a compiled JS file that registers a custom element. When a content editor opens a document with a property of this data type, Umbraco creates an instance of that element and sets its `value` and `config` properties.

#### The alias rule — connecting frontend to backend

This is the single most important rule for property editors:

> **The `alias` of the `propertyEditorUi` extension MUST exactly match the first argument of the C# `[DataEditor]` attribute on the backend `DataEditor` class.**

If they differ, Umbraco cannot connect the data type to its editor. The data type will exist in the database but will display nothing (or the wrong editor) in the content UI. This is a silent failure with no compile-time or startup error.

**Example — the Cells editor:**

Backend (`src/Plugins/Cells/N3O.Umbraco.Cells/DataTypes/CellsDataEditor.cs`):
```csharp
[DataEditor(CellsConstants.PropertyEditorAlias, ValueType = ValueTypes.Json)]
public class CellsDataEditor : DataEditor { ... }
```

Where (`src/Plugins/Cells/N3O.Umbraco.Cells/CellsConstants.cs`):
```csharp
public const string PropertyEditorAlias = "N3O.Umbraco.Cells";
```

Frontend (`src/Plugins/Cells/N3O.Umbraco.Cells.StaticAssets/wwwroot/App_Plugins/N3O.Umbraco.Cells/umbraco-package.json`):
```json
{
    "type": "propertyEditorUi",
    "alias": "N3O.Umbraco.Cells"
}
```

Both are `"N3O.Umbraco.Cells"` — they match. The same rule applies to the Import Data Editor where both backend and frontend use `"N3O.Umbraco.Data.ImportDataEditor"`.

**C# analogy:** Think of the alias as the string you pass to `[Route]` on a controller — it is the identifier the routing system uses to connect an incoming request to a handler. If you change one side without the other, the wiring breaks silently.

### 2. `propertyEditorSchema`

A `propertyEditorSchema` declares the *data contract* side of a property editor: value type, configuration properties (called "prevalues"), and the default UI alias. The UI (`propertyEditorUi`) handles rendering; the schema handles what data shape is stored.

**Real example — `N3O.Umbraco.Cells`:**

```json
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
}
```

- **`meta.defaultPropertyEditorUiAlias`** — which UI extension renders this schema by default. Must match the `alias` of a `propertyEditorUi` extension.
- **`meta.settings.properties`** — the configuration fields shown in the data type's Settings tab in the Umbraco backoffice. Each property uses another `propertyEditorUi` to render its own input. Here, `gridConfiguration` is a textarea where the administrator pastes JSON to configure the grid.

When you look at the N3O.Umbraco.Cells `umbraco-package.json` (`src/Plugins/Cells/N3O.Umbraco.Cells.StaticAssets/wwwroot/App_Plugins/N3O.Umbraco.Cells/umbraco-package.json`), you see both the schema and the UI registered together in the same manifest — they share the same alias `"N3O.Umbraco.Cells"` and each points to the other via `propertyEditorSchemaAlias` / `defaultPropertyEditorUiAlias`.

### 3. `workspaceView`

A **workspace view** is an additional tab in the content/media workspace — the editing surface that appears when you open a document or media item. Umbraco's built-in tabs are "Content" and "Info"; plugins can add more.

**Real example — `N3O.Umbraco.Data.Export`:**

`src/Data/N3O.Umbraco.Data.StaticAssets/wwwroot/App_Plugins/N3O.Umbraco.Data.Export/umbraco-package.json`

```json
{
    "type": "workspaceView",
    "alias": "N3O.WorkspaceView.DataExport",
    "name": "N3O Data Export",
    "element": "/App_Plugins/N3O.Umbraco.Data.Export/data-export.js",
    "meta": {
        "label": "Export",
        "pathname": "dynamic-children",
        "icon": "icon-download-alt"
    },
    "conditions": [
        {
            "alias": "Umb.Condition.WorkspaceAlias",
            "match": "Umb.Workspace.Document"
        },
        {
            "alias": "N3O.Condition.WorkspaceVisibility",
            "endpoint": "/umbraco/backoffice/api/ExportVisibility"
        }
    ]
}
```

- **`meta.label`** — the text shown on the tab.
- **`meta.pathname`** — the URL fragment appended when this tab is active (used by Umbraco's SPA routing).
- **`meta.icon`** — an icon name shown alongside the label.
- **`conditions`** — this extension only appears if every condition in this array is satisfied. See [Conditions](#5-condition) below.

When Umbraco renders the document workspace and all conditions pass, it inserts `<data-export-element>` (or whatever element name the JS registers) as the workspace view tab's content.

**Second example — `N3O.Umbraco.DynamicListViews`:**

`src/N3O.Umbraco.Cms/wwwroot/App_Plugins/N3O.Umbraco.DynamicListViews/umbraco-package.json`

```json
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
        { "alias": "Umb.Condition.WorkspaceAlias", "match": "Umb.Workspace.Document" },
        { "alias": "N3O.Condition.WorkspaceVisibility", "endpoint": "/umbraco/backoffice/api/DynamicListViewApi" }
    ]
}
```

The `weight: 300` pushes this tab toward the right in the tab bar.

### 4. `dashboard`

A **dashboard** is a full-panel widget shown in a backoffice section's landing area (e.g. the Content section's home screen, the Settings section's home screen). Dashboards are not tied to a specific document.

**Real example — `N3O.Umbraco.WelcomeDashboard`:**

`src/Plugins/WelcomeDashboard/N3O.Umbraco.WelcomeDashboard.StaticAssets/wwwroot/App_Plugins/N3O.Umbraco.WelcomeDashboard/umbraco-package.json`

```json
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
```

- `weight: -10` — a low weight means this dashboard appears first (leftmost tab).
- The `Umb.Condition.SectionAlias` condition restricts this dashboard to the Content section only.

**Second example — `N3O.Umbraco.Scheduler`:**

`src/Scheduler/N3O.Umbraco.Scheduler.StaticAssets/wwwroot/App_Plugins/N3O.Umbraco.Scheduler/umbraco-package.json`

```json
{
    "type": "dashboard",
    "alias": "N3O.Dashboard.Scheduler",
    "element": "/App_Plugins/N3O.Umbraco.Scheduler/scheduler-dashboard.js",
    "conditions": [
        { "alias": "Umb.Condition.SectionAlias", "match": "Umb.Section.Settings" }
    ]
}
```

This dashboard appears only in the Settings section (`Umb.Section.Settings`).

### 5. `condition`

A **condition** is a reusable gating mechanism. When an extension lists a condition, Umbraco evaluates it before showing that extension. If the condition returns `false`, the extension (tab, dashboard, editor, etc.) is hidden.

Conditions are themselves registered as extensions of type `"condition"`. They differ from the other types in that they use `"api"` instead of `"element"`:

```json
{
    "type": "condition",
    "alias": "N3O.Condition.WorkspaceVisibility",
    "name": "N3O Workspace Visibility Condition",
    "api": "/App_Plugins/N3O.Umbraco.BackofficeCore/workspace-visibility-condition.js"
}
```

- **`api`** — the JS file that exports a class (as the `default` export) extending Umbraco's `UmbConditionBase`. This class is instantiated by the extension registry, not inserted into the DOM.

**Built-in conditions used in this repo:**

| Alias | Meaning | Example usage |
|-------|---------|---------------|
| `Umb.Condition.WorkspaceAlias` | Matches the current workspace type. `match: "Umb.Workspace.Document"` means "only on document workspaces". | All workspace views in this repo |
| `Umb.Condition.SectionAlias` | Matches the current section. `match: "Umb.Section.Content"` restricts to the Content section. | Welcome Dashboard, Scheduler |

**Custom condition — `N3O.Condition.WorkspaceVisibility`:**

This repo's own condition, implemented in `src/N3O.Umbraco.Cms/Apps/N3O.Umbraco.BackofficeCore/src/workspace-visibility-condition.ts`. It takes a manifest-configured `endpoint` URL, calls `{endpoint}/{documentUniqueKey}` as an authenticated request, and shows the extension only if the JSON response contains `{ "visible": true }`.

```json
{
    "alias": "N3O.Condition.WorkspaceVisibility",
    "endpoint": "/umbraco/backoffice/api/ExportVisibility"
}
```

This restores the v13 `IContentAppFactory` per-node gating behaviour: each document's visibility is decided by a C# controller method that can check the document type, user permissions, or any business rule. See [BackofficeCore](../apps/backofficecore.md) for the full source walkthrough.

---

## Contexts — the extension's service locator

Umbraco 17 uses a **context system** rather than constructor-injection for communicating data to extensions. An extension consumes a context by calling `this.consumeContext(TOKEN, callback)`. The runtime searches up the DOM tree for a provider of that token and passes the resolved value to the callback. If the provider changes (e.g. the user navigates to a different document), the callback fires again.

**C# analogy:** `consumeContext` is similar to `IServiceProvider.GetService<T>()` but with two important differences:
1. Resolution is **scoped to DOM position** — the provider closest to the element in the DOM tree wins (like a scoped DI container whose scope boundary is a DOM node).
2. The resolution is **reactive** — if the service instance changes, your callback is re-invoked (unlike .NET DI, which gives you a fixed instance once per scope).

The contexts most commonly consumed in N3O plugins:

### `UMB_AUTH_CONTEXT`

Token: `UMB_AUTH_CONTEXT` from `@umbraco-cms/backoffice/auth`.

Provides the current authentication state and the method to build authenticated HTTP requests. In this repo, `UmbAuthFetchMixin` (from `@n3o/backoffice-core`) wraps this context to give elements a ready-to-use `authFetch` function. Direct usage:

```ts
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';

this.consumeContext(UMB_AUTH_CONTEXT, (authContext) => {
    // authContext.getOpenApiConfiguration() returns { token(): Promise<string>, credentials: ... }
});
```

Plain `fetch()` calls to `[Authorize]`-decorated backoffice controllers return `401`. You must obtain the bearer token from this context and add it to the `Authorization` header. See [10 — The N3O Bridge Pattern](./10-the-n3o-bridge-pattern.md) and [BackofficeCore](../apps/backofficecore.md) for details.

### `UMB_DOCUMENT_WORKSPACE_CONTEXT`

Token: `UMB_DOCUMENT_WORKSPACE_CONTEXT` from `@umbraco-cms/backoffice/document`.

Provides the current document workspace's state — including the document's unique key (a GUID string) and the active culture/variant. Consumed by both `N3O.Condition.WorkspaceVisibility` (to know which document to check) and `N3oBlockPreviewElement` (to include the document key in the preview API call).

```ts
import { UMB_DOCUMENT_WORKSPACE_CONTEXT } from '@umbraco-cms/backoffice/document';

this.consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, (context) => {
    this.observe(context.unique, (unique) => {
        // `unique` is the document's GUID, e.g. "11111111-2222-3333-4444-555555555555"
    });
});
```

The values on the context (like `context.unique`) are **RxJS Observables** — not plain values. You subscribe to them with `this.observe(observable, callback, key)`, where `key` is an arbitrary deduplication string. Each time the observable emits a new value, the callback fires.

### `UMB_BLOCK_ENTRY_CONTEXT` and `UMB_BLOCK_MANAGER_CONTEXT`

Tokens from `@umbraco-cms/backoffice/block`. Used only by the Block Preview element (`block-preview.ts`):

- `UMB_BLOCK_ENTRY_CONTEXT` — provides the specific block instance's `contentKey` and `contentElementTypeKey`.
- `UMB_BLOCK_MANAGER_CONTEXT` — provides the whole block grid's layouts, contentData, settingsData, and expose arrays, which are POST-ed to the C# preview endpoint.

### Property editor value and config

Property editor UI elements do not use `consumeContext` for their primary data. Umbraco sets value and configuration by **assigning JavaScript properties directly** on the custom element:

```ts
// Umbraco calls these setters directly:
element.value = currentValue;       // the stored property value
element.config = configCollection;  // the data type's prevalues (configuration)
```

Your element implements the `UmbPropertyEditorUiElement` interface (from `@umbraco-cms/backoffice/property-editor`), which declares the `value` getter/setter contract. Config arrives as a `UmbPropertyEditorConfigCollection` object; call `config.getValueByAlias('myKey')` to retrieve individual prevalues.

To save a new value, dispatch a `UmbPropertyValueChangeEvent`:

```ts
import { UmbPropertyValueChangeEvent } from '@umbraco-cms/backoffice/property-editor';

this.dispatchEvent(new UmbPropertyValueChangeEvent());
// Umbraco will then call element.value getter to read the new value
```

This event/setter pattern is the backoffice equivalent of a WinForms control raising `ValueChanged` and the form reading its `Value` property.

---

## The `uui` component library

The Umbraco backoffice ships a component library called **Umbraco UI Library** (`@umbraco-ui/uui-*`). These are **web components** — custom elements you use as HTML tags. They are not React components, not Razor tag helpers. They are loaded by Umbraco's own import map and available everywhere in the backoffice without any import.

### Common `uui-*` elements

| Tag | Description |
|-----|-------------|
| `<uui-box>` | A styled card/panel container. Accepts a `headline` attribute for a title bar. Used heavily as the outer wrapper for property editors and workspace views. |
| `<uui-button>` | A styled button. Attributes: `label`, `look` (`"primary"`, `"secondary"`, `"outline"`, `"default"`), `color` (`"positive"`, `"warning"`, `"danger"`), `compact`, `href`. |
| `<uui-loader>` | An animated spinner for loading states. Used in N3O plugins while waiting for async data. |
| `<uui-table>` | A styled data table container. Used with `<uui-table-head>`, `<uui-table-head-cell>`, `<uui-table-row>`, `<uui-table-cell>`. |
| `<uui-tag>` | A coloured inline badge (e.g. "Published", "Draft"). Attributes: `color`, `look`. |
| `<uui-icon>` | Renders an Umbraco backoffice icon by name. Attribute: `name` (e.g. `"icon-search"`). |

### Common `umb-*` layout elements

| Tag | Description |
|-----|-------------|
| `<umb-property-layout>` | The standard layout wrapper for a property inside a property editor — provides the label/description chrome and aligns with other properties. |

### Using `uui-*` inside React TSX

Web components are just HTML elements, so React can render them as JSX intrinsic elements:

```tsx
<uui-box headline="My Editor">
    <p>Content here</p>
</uui-box>
```

However, TypeScript does not know the types of these custom elements unless you declare them. Each `Apps/src/` folder that renders `uui-*` elements from TSX contains a `uui-react.d.ts` file that extends the React JSX namespace:

`src/Blocks/N3O.Umbraco.Blocks.StaticAssets/Apps/src/uui-react.d.ts`:
```ts
import 'react';

declare module 'react' {
    namespace JSX {
        interface IntrinsicElements {
            'uui-loader': any;
        }
    }
}
```

This is a **TypeScript ambient declaration** — it does not generate any code. It tells the TypeScript compiler "treat `<uui-loader>` in JSX as a valid element." The `any` type is intentional: UUI ships Lit types, not React types, and the full Lit type shape is incompatible with React's intrinsic element system.

**Important gotcha:** Interactive `uui-*` form controls — `<uui-input>`, `<uui-textarea>`, `<uui-select>`, `<uui-checkbox>`, and `<uui-button>` in its active/interactive forms — **break when rendered by React in Umbraco 17**. They do not mount and produce console errors. The reason is a conflict between React's synthetic event system and the way these Lit-based components manage their internal state in the shadow DOM.

**The rule:** Inside a React component (`.tsx`), use **native HTML controls** (`<input>`, `<textarea>`, `<select>`, `<button>`) for interactive inputs. You may use `<uui-box>` and `<umb-property-layout>` as structural wrappers — they are safe because they are non-interactive. You may also use read-only display elements like `<uui-loader>`, `<uui-tag>`, `<uui-icon>`.

The `serp-editor-app.tsx` demonstrates this correctly: it uses `<uui-box>` as the outer wrapper but `<input>` and `<textarea>` for the actual user input, not `<uui-input>` / `<uui-textarea>`.

---

## How Umbraco loads an extension — the sequence

1. ASP.NET Core starts. The static web assets pipeline serves `wwwroot/App_Plugins/` from every `*.StaticAssets` project (and their NuGet packages).
2. Umbraco scans all `App_Plugins/*/umbraco-package.json` files. It merges `importmap.imports` entries into a single browser import map and registers all `extensions` entries in its in-memory extension registry.
3. The browser loads the backoffice HTML page. Umbraco injects a `<script type="importmap">` block containing all merged import-map entries, followed by the backoffice bootstrap script.
4. As the user navigates, the extension registry evaluates which extensions should appear (checking conditions). For each visible extension, Umbraco fetches the `element` (or `api`) JS file and registers the custom element (or condition class).
5. Umbraco inserts the custom element tag into the DOM. The browser instantiates the class, calls `connectedCallback`, and the plugin renders.

Step 4 is **lazy** — JS files are fetched only when the user first visits a view that needs them, not at startup. This is why the `element` field is a URL, not an inline script.

---

## Summary table — extension types in this repo

| Type | Alias pattern | Where it appears | JS contract |
|------|---------------|-----------------|-------------|
| `propertyEditorUi` | `N3O.Umbraco.*` | Property input inside the content editor | Custom element with `value` getter/setter + `config` setter |
| `propertyEditorSchema` | `N3O.Umbraco.*` | Data type definition (config properties) | No element — pure manifest metadata |
| `workspaceView` | `N3O.WorkspaceView.*` | Extra tab on a document workspace | Custom element |
| `dashboard` | `N3O.Dashboard.*` | Tab in a section landing area | Custom element |
| `condition` | `N3O.Condition.*` | Gating mechanism for other extensions | Class extending `UmbConditionBase`, exported as `default` |

---

**Next:** [10 — The N3O Bridge Pattern](./10-the-n3o-bridge-pattern.md)
