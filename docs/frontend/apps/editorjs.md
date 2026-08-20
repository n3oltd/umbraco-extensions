# EditorJS Property Editor

## 1. What it is

The EditorJS property editor is a **rich-text / structured-content block editor** built on the third-party [EditorJS](https://editorjs.io/) library. It lets content editors compose a document out of typed _blocks_ — paragraphs, headers, images, quotes, code, embeds, lists, checklists, and raw HTML — each stored as a JSON object. The whole document is serialised as a single JSON string and saved as a standard Umbraco property value.

In Umbraco's vocabulary this is a **propertyEditorUi** (see [../concepts/09-umbraco-backoffice-extensions.md](../concepts/09-umbraco-backoffice-extensions.md)). It is served to the browser as a static file from `App_Plugins/N3O.Umbraco.EditorJs/`, a path that is served automatically by Umbraco's static-files middleware because the `N3O.Umbraco.EditorJs.StaticAssets` project is a Razor Class Library (RCL). The editor is registered in `umbraco-package.json` so Umbraco discovers and loads it on backoffice startup.

It is one of the most structurally complete apps in the repo: it uses the **N3O bridge pattern** (web-component shell wrapping a React app — see [../concepts/10-the-n3o-bridge-pattern.md](../concepts/10-the-n3o-bridge-pattern.md)), integrates a bundled third-party imperative library, defines three custom EditorJS tools, and opens two different Umbraco picker modals from inside that bundled code.

---

## 2. Files

All source files are under `src/Plugins/EditorJs/N3O.Umbraco.EditorJs.StaticAssets/Apps/`.

| Path (relative to `Apps/`) | Role |
|---|---|
| `package.json` | npm package for this app: name, EditorJS dependencies, build scripts |
| `tsconfig.json` | TypeScript config — extends shared `@n3o/build/tsconfig`, adds `jsx: react-jsx` |
| `vite.config.ts` | Vite build config — calls `n3oPluginConfig()` from `@n3o/build` |
| `src/editor-js.ts` | **Shell file** — defines the `<n3o-editor-js>` custom element (web component) |
| `src/editor-js-app.tsx` | **React app** — creates/destroys the EditorJS instance, handles value in/out |
| `src/editor-js-app.css` | Scoped CSS for the editor canvas and fullscreen mode |
| `src/tools/UmbracoImageTool.ts` | Custom EditorJS _block_ tool: Umbraco media picker → image block |
| `src/tools/UmbracoLinkTool.ts` | Custom EditorJS _inline_ tool: Umbraco link picker → `<a>` wrap |
| `src/tools/EmbedWithUI.ts` | Custom EditorJS _block_ tool: extends `@editorjs/embed` with a URL input UI |
| `src/vendor.d.ts` | Ambient TypeScript declarations for EditorJS packages that ship no types |
| `src/uui-react.d.ts` | Ambient JSX declarations that let Umbraco UI Library web components be used in TSX |
| `wwwroot/App_Plugins/N3O.Umbraco.EditorJs/umbraco-package.json` | Umbraco backoffice manifest (hand-authored, not generated) |

The backend project `N3O.Umbraco.EditorJs` (sibling of `N3O.Umbraco.EditorJs.StaticAssets`) contains:

| Path (relative to `src/Plugins/EditorJs/N3O.Umbraco.EditorJs/`) | Role |
|---|---|
| `DataTypes/EditorJsDataEditor.cs` | `[DataEditor]` class — registers the property editor with Umbraco |
| `EditorJsConstants.cs` | Holds `PropertyEditorAlias = "N3O.Umbraco.EditorJs"` |
| `DataTypes/EditorJsValueConverter.cs` | Converts stored JSON to a typed C# model at render time |
| `Converters/BlockDataConverter.*.cs` | Per-block-type converters producing HTML from block data |
| `Models/EditorJsModel.cs`, `EditorJsBlock.cs` | C# models for the JSON structure |

---

## 3. End-to-end flow

```
umbraco-package.json
  └─ type: "propertyEditorUi"
  └─ alias: "N3O.Umbraco.EditorJs"          ← must match [DataEditor] alias (C#)
  └─ element: "/App_Plugins/.../editor-js.js"

Browser loads editor-js.js
  └─ defines <n3o-editor-js> custom element (editor-js.ts)
       │
       ├─ UmbElementMixin provides context plumbing
       ├─ consumes UMB_MODAL_MANAGER_CONTEXT → #modalManager
       ├─ attaches shadow root, creates <div> mount point
       │
       └─ connectedCallback → createRoot(mount) → #render()
            │
            └─ createElement(EditorJsApp, { value, bridge, onChange })
                 │
                 └─ useEffect([], ...) — runs ONCE after mount
                      │
                      ├─ new EditorJS({ holder, data: parse(value), tools, onChange })
                      │    └─ tools.image  = makeUmbracoImageTool(openMediaPicker)
                      │    └─ tools.link   = makeUmbracoLinkTool(openLinkPicker)
                      │    └─ tools.embed  = { class: EmbedWithUI, config: ... }
                      │    └─ (+ Header, Paragraph, List, Quote, Code, Raw, Checklist, AlignmentTune)
                      │
                      ├─ onReady: new DragDrop(editor)
                      │
                      ├─ onChange: editor.save() → JSON.stringify → props.onChange(json)
                      │                                              │
                      │                                              └─ shell: this.#value = json
                      │                                                 shell: dispatchEvent(UmbPropertyValueChangeEvent)
                      │
                      └─ cleanup (return fn): editor.destroy()

User picks an image (UmbracoImageTool button click)
  └─ openMediaPicker(tool) — closure captured by makeUmbracoImageTool
       └─ modalManager.open(host, UMB_MEDIA_PICKER_MODAL, ...)
       └─ await modal.onSubmit() → { selection: [guid] }
       └─ UmbMediaUrlRepository + UmbMediaItemRepository → url + name
       └─ tool.applyMediaSelection({ url, name, unique, udi })
            └─ updates DOM, calls tool.save(), scrolls image into view

User selects text + clicks link button (UmbracoLinkTool)
  └─ surround(range) → openLinkPicker(this, range)
       └─ modalManager.open(host, UMB_LINK_PICKER_MODAL, ...)
       └─ await modal.onSubmit() → { link: { url, unique } }
       └─ tool.wrap(range, url) → inserts <a> around selection
```

The bridge pattern (see [../concepts/10-the-n3o-bridge-pattern.md](../concepts/10-the-n3o-bridge-pattern.md)) is the reason for splitting into two files: the web-component shell holds the Umbraco contract (property value + events + context) while the React app holds the UI. The EditorJS tools, being neither the shell nor the React component, receive their Umbraco dependency (the modal manager) through _closures_ produced by factory functions.

---

## 4. File-by-file

### 4.1 `package.json`

```json
{
    "name": "n3o-umbraco-editorjs",
    "type": "module",
    "scripts": {
        "build": "tsc --noEmit && vite build",
        "watch": "vite build --watch"
    },
    "dependencies": {
        "@editorjs/checklist": "^1.6.0",
        "@editorjs/code": "^2.9.2",
        "@editorjs/editorjs": "^2.30.6",
        "@editorjs/embed": "^2.7.6",
        "@editorjs/header": "^2.8.8",
        "@editorjs/list": "^1.10.0",
        "@editorjs/paragraph": "^2.11.7",
        "@editorjs/quote": "^2.7.2",
        "@editorjs/raw": "^2.5.0",
        "editor-js-alignment-tune": "^1.0.1",
        "editorjs-drag-drop": "^1.1.16"
    },
    "devDependencies": {
        "@n3o/build": "*"
    }
}
```

All `dependencies` are **EditorJS** and its plugin packages. They live in `dependencies` (not `devDependencies`) because Vite bundles them into the output JS — they must be present at build time. `@n3o/build` is the shared build tooling package (see [../concepts/05-vite-and-the-build.md](../concepts/05-vite-and-the-build.md)) and only drives the build, never bundled.

Notice what is **absent**: `react`, `react-dom`, `@umbraco-cms/backoffice`. These are declared at the workspace root (`src/package.json`) and are marked _external_ by the build config — they are never bundled here. Think of it like a C# project that has NuGet references but some assemblies are marked `ExcludeAssets="runtime"` because they are provided by the host.

### 4.2 `tsconfig.json`

```json
{
    "extends": "@n3o/build/tsconfig",
    "compilerOptions": { "jsx": "react-jsx" },
    "include": ["src"]
}
```

This is minimal on purpose. Everything else — `target: ES2022`, `strict: true`, `experimentalDecorators: true`, `skipLibCheck: true`, etc. — is inherited from `@n3o/build/tsconfig` (`src/N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/base.json`). The only additions are:

- `jsx: "react-jsx"` — tells TypeScript how to transform JSX syntax. `"react-jsx"` uses the React 17+ automatic JSX transform (no `import React` needed at the top of every file). In C# terms this is like choosing which XML serialiser pre-processes your markup.
- `"include": ["src"]` — scope type-checking to this app's source only.

For a full explanation of TypeScript compilation see [../concepts/02-javascript-typescript-for-csharp-devs.md](../concepts/02-javascript-typescript-for-csharp-devs.md).

### 4.3 `vite.config.ts`

```ts
import { n3oPluginConfig } from '@n3o/build';

export default n3oPluginConfig({
    entries: { 'editor-js': 'src/editor-js.ts' },
    outDir: '../wwwroot/App_Plugins/N3O.Umbraco.EditorJs',
    react: true,
});
```

`n3oPluginConfig` is the shared Vite factory in `src/N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/vite-config.js`. Passing `react: true` makes it add `react`, `react-dom`, `react-dom/client`, and `react/jsx-runtime` to Vite's _externals_ list, in addition to the always-external `@umbraco-cms/*`. Externals are modules that Vite does _not_ bundle into the output; instead it emits bare `import` statements (`import React from 'react'`) that the browser resolves at runtime via Umbraco's import map.

The result is one output file: `wwwroot/App_Plugins/N3O.Umbraco.EditorJs/editor-js.js`.

**Why are EditorJS packages bundled but React is external?**

Umbraco 17's backoffice ships a browser-side [import map](../concepts/04-es-modules-and-import-maps.md) that maps bare specifiers like `"react"` to the shared runtime URL. The EditorJS packages (`@editorjs/*`) are _not_ in that map, so the browser has no way to resolve them at runtime — they must be baked into the output JS. React _is_ in the map (provided by `N3O.Umbraco.ReactRuntime`), so it stays external. If you bundled React too, you would get two copies of React on the page and React would break because the two copies would have separate state internals.

For a complete explanation see [../concepts/05-vite-and-the-build.md](../concepts/05-vite-and-the-build.md) and [../concepts/04-es-modules-and-import-maps.md](../concepts/04-es-modules-and-import-maps.md).

### 4.4 `src/editor-js.ts` — the web-component shell

This file defines the `<n3o-editor-js>` custom element. It is the **only** file Vite uses as its entry point. Everything in the dependency graph of this file ends up in `editor-js.js`.

```ts
@customElement('n3o-editor-js')
export class N3oEditorJsElement
    extends UmbElementMixin(HTMLElement)
    implements UmbPropertyEditorUiElement
{
    #root?: Root;
    #mount: HTMLDivElement;
    #value: string | undefined;
    #modalManager: any;
    ...
}
```

Key points:

- **`UmbElementMixin(HTMLElement)`** — a mixin provided by Umbraco that adds Umbraco's context-consumer machinery to a plain `HTMLElement`. In C# terms this is like a base class that injects an `IServiceProvider`. The app uses `UmbElementMixin` rather than `LitElement` (which is the usual Umbraco base) because the UI is rendered by React, not Lit; the mixin is used _only_ for context plumbing.
- **`implements UmbPropertyEditorUiElement`** — the Umbraco contract for a property editor. Requires a `value` getter/setter of type `string | undefined`. Umbraco sets the value via the setter and reads it via the getter; the editor signals a change by dispatching `UmbPropertyValueChangeEvent` on itself.
- **`#value` (private field, `#` prefix)** — JavaScript's private class field syntax. Equivalent to a C# `private` field. Unlike TypeScript's `private` keyword, `#` is enforced at the JavaScript runtime level and truly inaccessible outside the class.
- **`consumeContext(UMB_MODAL_MANAGER_CONTEXT, callback)`** — subscribes to the modal manager context that is provided higher up in the DOM tree by Umbraco's backoffice shell. The callback fires when the context is resolved (asynchronously). The modal manager is stored in `#modalManager` and passed down to the React app via the bridge.
- **`connectedCallback` / `disconnectedCallback`** — the web-component lifecycle hooks for "element was added to the DOM" and "element was removed". `connectedCallback` creates the React root; `disconnectedCallback` unmounts it. This is analogous to `OnInitialized` / `Dispose` in Blazor, or `OnStart` / `OnStop` in a .NET hosted service.
- **`createRoot(this.#mount)`** — React 18+ API. Creates a React rendering root attached to the `<div>` inside the shadow DOM.
- **`this.#root.render(createElement(EditorJsApp, props))`** — renders the React component tree. `createElement(EditorJsApp, props)` is the non-JSX equivalent of `<EditorJsApp ...props />`.
- **`EditorJsHostBridge`** — a plain object `{ host: this, modalManager: this.#modalManager }` passed as a prop to `EditorJsApp`. It gives the React app (and the EditorJS tools created inside it) access to the Umbraco context objects without those tools needing to be web components or context consumers themselves.

For background on shadow DOM and custom elements see [../concepts/06-web-components-and-shadow-dom.md](../concepts/06-web-components-and-shadow-dom.md).

### 4.5 `src/editor-js-app.tsx` — the React app

This is the substantive file. It wraps an **imperative** third-party library (EditorJS) inside a React component using `useEffect` and `useRef`.

#### Imports

```ts
import EditorJS from '@editorjs/editorjs';
import Header from '@editorjs/header';
import RawTool from '@editorjs/raw';        // declared `any` in vendor.d.ts
import Checklist from '@editorjs/checklist'; // declared `any` in vendor.d.ts
import List from '@editorjs/list';
import Quote from '@editorjs/quote';
import CodeTool from '@editorjs/code';
import Paragraph from '@editorjs/paragraph';
import DragDrop from 'editorjs-drag-drop';   // declared `any` in vendor.d.ts
import AlignmentTuneCtor from 'editor-js-alignment-tune'; // declared `any` in vendor.d.ts
import styles from './editor-js-app.css?inline';
```

`@editorjs/editorjs` ships its own `.d.ts` so TypeScript is happy. The others either have no types or have broken types — they are declared in `vendor.d.ts` (see section 4.8). The `?inline` suffix on the CSS import is a Vite feature: instead of injecting a `<link>` tag, Vite returns the CSS file's contents as a plain string, which is then injected into the shadow DOM via a `<style>` tag in the JSX (see line 329 of the app: `<style>{styles}</style>`). This is necessary because a `<link>` in a shadow root would not cross the shadow boundary to reach `App_Plugins/`.

#### The bridge interface

```ts
export interface EditorJsHostBridge {
    host: any;
    modalManager: any;
}
```

`host` is the `<n3o-editor-js>` element itself (typed as `any` for simplicity). It is needed because `modalManager.open(host, ...)` takes the host element as the modal's "opener" (used by Umbraco to associate the modal with the right element context for focus management). `modalManager` is the consumed `UMB_MODAL_MANAGER_CONTEXT`.

#### `EditorJsApp` function component

```ts
export function EditorJsApp({ value, bridge, onChange }: EditorJsAppProps) {
    const containerRef = useRef<HTMLDivElement>(null);
    const editorRef    = useRef<any>(null);
    const valueRef     = useRef(value);
    const onChangeRef  = useRef(onChange);
    const bridgeRef    = useRef(bridge);
    valueRef.current   = value;
    onChangeRef.current = onChange;
    bridgeRef.current  = bridge;
    const editorId = useRef('skrivlet-editor-' + randomUUID());
    ...
}
```

**Refs for closing over mutable values** — This is an important React pattern for wrapping imperative libraries. EditorJS is initialised _once_ in `useEffect([], [])` (the empty dependency array means "run once after mount"). If the `onChange` callback was captured directly in the closure at init time, it would be stale after re-renders. Instead, `onChangeRef.current` is updated on every render, and the EditorJS `onChange` callback reads `onChangeRef.current` at call time. In C# terms: rather than closing over `onChange` as a delegate captured once, you store the delegate in a field and always invoke `this.onChangeRef.Current`.

#### `useEffect` — EditorJS lifecycle

```ts
useEffect(() => {
    const holder = containerRef.current?.querySelector('#' + editorId.current);
    ...
    const editor = new (EditorJS as any)({
        holder: holder,
        data: getInitialData(),
        inlineToolbar: true,
        sanitizer: { a: {} },    // NOTE: has limited effect — see Gotchas
        tools: buildTools(),
        onChange: () => {
            stopUmbracosInterferingHotKeys();
            editorRef.current?.save().then((outputData) => {
                onChangeRef.current(JSON.stringify(outputData));
            });
        },
        onReady: () => {
            new DragDrop(editorRef.current);
            stopUmbracosInterferingHotKeys();
        },
    });
    editorRef.current = editor;

    return () => {           // cleanup: runs when the component unmounts
        editorRef.current?.destroy();
        editorRef.current = null;
    };
}, []);  // [] = run once
```

- `getInitialData()` parses `valueRef.current` (a JSON string) into an object. Umbraco may hand back either a string or an already-parsed object (happens inside block-grid contexts); both cases are handled.
- `buildTools()` constructs the tools configuration object. Tools that need Umbraco integration (`image`, `link`) are produced by factory functions (`makeUmbracoImageTool`, `makeUmbracoLinkTool`) so they can close over `openMediaPicker` / `openLinkPicker` — functions defined in the same `useEffect` closure.
- `stopUmbracosInterferingHotKeys()` iterates EditorJS content-editable elements and sets `disable-hotkeys="true"`. This prevents Umbraco's global keyboard-shortcut handler from intercepting keystrokes that should be handled by EditorJS (e.g., `ctrl+b` for bold). See the original Umbraco PR referenced in the comment.
- The cleanup function (returned from `useEffect`) calls `editor.destroy()`. React calls this when the component unmounts (i.e., when the web-component's `disconnectedCallback` calls `this.#root?.unmount()`). Failing to call `destroy()` would leak EditorJS event listeners.

#### Modal pickers

`openMediaPicker` and `openLinkPicker` are defined as `async` arrow functions inside the `useEffect`. They close over `bridgeRef` (not `bridge` directly, for the same stale-closure reason as `onChangeRef`).

**`openMediaPicker`**:
1. `bridgeRef.current.modalManager.open(host, UMB_MEDIA_PICKER_MODAL, { data: { multiple: false } })` — opens the standard Umbraco media picker modal. `host` is the `<n3o-editor-js>` element, which is required by Umbraco to establish the context chain for the modal.
2. `await modal.onSubmit()` — returns `{ selection: [guid | null] }` (v17 verified). The `await` suspends the async function until the editor clicks "Submit" or the Promise rejects (modal was cancelled — caught by the `catch` block).
3. Two Umbraco repositories (`UmbMediaUrlRepository`, `UmbMediaItemRepository`) resolve the GUID into a URL and a name. Both are attached to `host` (they register controllers on the element). They are destroyed after use so they do not accumulate across repeated picks.
4. `tool.applyMediaSelection(...)` — calls back into the `UmbracoImageTool` instance to update its DOM and internal data.

**`openLinkPicker`**:
1. `modalManager.open(host, UMB_LINK_PICKER_MODAL, ...)` — opens the standard Umbraco multi-URL picker.
2. `await modal.onSubmit()` → `{ link: { url, unique } }`. The `url` is used for external links; `unique` (a GUID) is used for internal Umbraco content links.
3. `tool.wrap(range, url)` — calls back into `UmbracoLinkTool` to insert the `<a>` element.

### 4.6 `src/tools/UmbracoImageTool.ts` — block tool for images

EditorJS block tools are plain JavaScript classes that follow a specific interface. EditorJS calls `new ToolClass({ data, api, config })` to create a tool instance for each block. The class must implement:

| Method / property | Role |
|---|---|
| `static get toolbox()` | Returns `{ title, icon }` — shown in EditorJS's block-insertion menu |
| `constructor({ data, api, config })` | Receives saved block data (from the JSON) and the EditorJS API object |
| `render()` | Returns an `HTMLElement` that EditorJS inserts into the editor canvas |
| `save()` | Returns the block's data object (EditorJS serialises this to JSON) |
| `validate(savedData)` | Returns `true` if the block is valid enough to save; invalid blocks are dropped |

`UmbracoImageTool` follows this interface. Because it needs access to `openMediaPicker` (a closure that uses the modal manager), it cannot be a top-level class registered directly with EditorJS. Instead it is produced by a **factory function**:

```ts
export function makeUmbracoImageTool(openMediaPicker: OpenMediaPicker) {
    return class UmbracoImageTool {
        ...
        constructor({ data, api, config }: BlockToolConstructorArg) { ... }

        render(): HTMLDivElement {
            // builds DOM: image, alt text input, "Select an image" button
            this.button.addEventListener('click', () => {
                void openMediaPicker(this);   // captures openMediaPicker from factory closure
            });
            ...
        }

        applyMediaSelection(item: MediaPickerResultItem): void {
            // called back by openMediaPicker after the user picks
            // updates this.data.url, this.data.udi, DOM elements, calls this.save()
        }

        save(): ImageData {
            return { url: this.data.url, alt: this.altTextInput?.value ?? '', udi: this.data.udi, ... };
        }

        validate(savedData: ImageData): boolean {
            return !!(savedData.url.trim() && savedData.udi.trim());
        }
    };
}
```

The factory pattern is analogous to a C# method that returns a configured `Func<>` or delegate — the returned class "closes over" `openMediaPicker` the way a lambda closes over a captured variable.

`applyMediaSelection` is the callback interface expected by `openMediaPicker`. When the modal resolves, `openMediaPicker` calls `tool.applyMediaSelection(item)`. The `tool` parameter type is a structural interface (`{ applyMediaSelection(item): void }`), not an explicit base class — TypeScript uses structural typing (duck typing), so any object with that method matches.

The `ImageData` interface (`url`, `alt`, `udi`, `width?`, `height?`) defines the JSON shape for each image block in the saved property value.

### 4.7 `src/tools/UmbracoLinkTool.ts` — inline tool for links

EditorJS _inline_ tools work on text selections rather than whole blocks. They appear in the floating toolbar when text is selected. The interface differences from block tools:

| Method / property | Role |
|---|---|
| `static get isInline()` | Must return `true` |
| `render()` | Returns the toolbar button element |
| `surround(range)` | Called when the button is clicked; `range` is the current text selection |
| `checkState()` | Called whenever the selection changes; should set `this.state` to indicate whether the selection is currently inside a link |
| `static get sanitize()` | **Important**: declares which HTML attributes are allowed on which tags when EditorJS saves the block's HTML. Without this, EditorJS would strip `href` from `<a>` tags. |

```ts
static get sanitize(): object {
    return { a: { href: true } };
}
```

This is the _only_ way HTML sanitization actually works per-tool in EditorJS v2.x — the global `sanitizer` config on the EditorJS instance does not apply during `save()`. See the sanitizer gotcha in section 6.

The `surround` method either calls `unwrap(range)` (if the selection is already inside a link) or delegates to `openLinkPicker(this, range)`. The `this` is passed so `openLinkPicker` can call back `tool.wrap(range, url)` after the picker resolves.

`wrap(range, url)`:
1. `range.extractContents()` — removes the selected DOM nodes and returns them as a DocumentFragment (like cutting from a clipboard).
2. Creates an `<a>` element, appends the fragment, inserts it back at the range position.
3. `this.api.selection.expandToTag(link)` — updates the EditorJS selection to encompass the new `<a>`.

Like `UmbracoImageTool`, the class is produced by a factory function (`makeUmbracoLinkTool`) so it can close over `openLinkPicker`.

### 4.8 `src/tools/EmbedWithUI.ts` — embed block with URL input

This tool extends `@editorjs/embed` to add a text input that appears _before_ a service (YouTube/Vimeo) has been detected. The base `Embed` class handles rendering the actual embed once the service is identified; `EmbedWithUI` overrides `render()` to show the URL input first.

```ts
export class EmbedWithUI extends (Embed as new (...args: any[]) => any) {
    static get toolbox() { ... }

    render(): HTMLElement {
        if (!(this as any).data?.service) {
            // No service yet — show URL input
            const input = RenderHelper.createInput('embed-input', '', '', 'url');
            input.addEventListener('paste', (event) => {
                const url = event.clipboardData?.getData('text') ?? '';
                const service = Object.keys(EmbedClass.services).find(
                    key => EmbedClass.services[key].regex.test(url)
                );
                if (service) {
                    (this as any).onPaste({ detail: { key: service, data: url } });
                }
            });
            ...
        }
        return (super.render as () => HTMLElement).call(this);
    }

    validate(savedData: any): boolean {
        return !!(savedData.service && savedData.source);
    }
}
```

`Embed` is declared as `any` in `vendor.d.ts` because it ships no `.d.ts`. To be able to `extend` a class typed as `any`, the code casts it to `new (...args: any[]) => any` — a constructor type with any arguments returning any. This tells TypeScript "this is a class you can extend" without knowing anything about its shape.

When the editor pastes a URL, the `paste` handler checks whether it matches a known service's regex (accessed via the class-level `EmbedClass.services` static property). If it does, it calls `(this as any).onPaste(...)` — invoking the base class method to trigger service detection and re-render.

### 4.9 `src/vendor.d.ts` — ambient declarations for untyped packages

```ts
declare module '@editorjs/raw' {
    const RawTool: any;
    export default RawTool;
}
declare module '@editorjs/checklist' { ... }
declare module '@editorjs/embed' { ... }
declare module 'editorjs-drag-drop' { ... }
declare module 'editor-js-alignment-tune' { ... }
```

TypeScript requires that every imported module has a type declaration. When a package ships `.d.ts` files, those are used automatically. When a package has no type declarations at all, TypeScript errors on the import with "Could not find a declaration file for module '...'".

The fix is an **ambient module declaration** — a `declare module '...'` block in any `.d.ts` file that TypeScript can see. This tells TypeScript "trust me, this module exists and its exports look like this". Using `any` is the minimal declaration — it turns off type-checking for everything imported from that module. This is analogous to writing `dynamic` in C# when you have an object from a COM API that has no type library.

`vendor.d.ts` is picked up automatically because it is inside the `src/` directory included by `tsconfig.json`. The `skipLibCheck: true` setting in the shared tsconfig suppresses errors inside the packages' own `.d.ts` files (some have malformed declarations) but does _not_ suppress import-resolution errors — hence the need for `declare module`.

### 4.10 `src/uui-react.d.ts` — JSX declarations for web-component tags

```ts
import 'react';

declare module 'react' {
    namespace JSX {
        interface IntrinsicElements {
            'uui-box': any;
            'uui-label': any;
            // ...
        }
    }
}
```

When you use a custom element tag in JSX (`<uui-box>`), TypeScript does not know whether it is a valid HTML element. By augmenting `React.JSX.IntrinsicElements`, you tell TypeScript "these custom element names are valid JSX elements". Without this, every `<uui-box>` would be a type error.

The comment at the top of the file explains an important constraint: **interactive UUI controls (buttons, inputs, toggles, etc.) are excluded** because they fail to mount when rendered by React in Umbraco 17 — they do not appear and produce console errors. Only display-only elements (`uui-box`, `uui-label`, `uui-icon`, `uui-loader`, `uui-load-indicator`) are safe. Use native HTML `<button>` and `<input>` inside `uui-box` wrappers instead. (See [../concepts/10-the-n3o-bridge-pattern.md](../concepts/10-the-n3o-bridge-pattern.md) for context on the React/Lit incompatibility.)

### 4.11 `src/editor-js-app.css`

Styles are scoped to the `.skriv-let` class (the root `<div>` rendered by `EditorJsApp`). Because CSS injected via `<style>{styles}</style>` inside a shadow DOM is scoped to the shadow root, the EditorJS canvas is fully isolated from the page. Notable rules:

- `.ce-block__content` and `.ce-toolbar__content` are constrained in width with `!important` to keep the editor at a readable column width.
- `.skriv-let__fullscreen-button` — positioned absolutely at the top-right; shows a fullscreen icon.
- `.skriv-let__container:fullscreen` — activated when the browser's Fullscreen API is active; scrolls the editor in a full viewport. `height: 100dvh` uses the "dynamic viewport height" unit (correct on mobile where the browser chrome changes height).
- Image and link tools are **hidden** in fullscreen (`.skriv-let__container:fullscreen .skriv-let__add-image-button` etc.) because the Umbraco modal manager cannot open modals from a fullscreen context.

### 4.12 `umbraco-package.json` — the backoffice manifest

```json
{
    "$schema": "http://json.schemastore.org/umbraco-package.json",
    "id": "N3O.Umbraco.EditorJs",
    "name": "N3O EditorJs",
    "version": "17.0.0",
    "extensions": [
        {
            "type": "propertyEditorUi",
            "alias": "N3O.Umbraco.EditorJs",
            "name": "N3O EditorJs",
            "element": "/App_Plugins/N3O.Umbraco.EditorJs/editor-js.js",
            "meta": {
                "label": "N3O EditorJs",
                "icon": "icon-document",
                "group": "richContent",
                "propertyEditorSchemaAlias": "N3O.Umbraco.EditorJs"
            }
        }
    ]
}
```

This file is the backoffice extension manifest. Umbraco scans all `umbraco-package.json` files under `App_Plugins/` and registers the declared extensions. See [../concepts/09-umbraco-backoffice-extensions.md](../concepts/09-umbraco-backoffice-extensions.md) for a full explanation of the manifest format and extension types.

**Critical: the alias rule.**

The `alias` field (`"N3O.Umbraco.EditorJs"`) must exactly match the string passed to the `[DataEditor]` attribute on the C# backend class:

```csharp
// src/Plugins/EditorJs/N3O.Umbraco.EditorJs/DataTypes/EditorJsDataEditor.cs
[DataEditor(EditorJsConstants.PropertyEditorAlias, ValueType = ValueTypes.Json)]
public class EditorJsDataEditor : DataEditor { ... }

// src/Plugins/EditorJs/N3O.Umbraco.EditorJs/EditorJsConstants.cs
public const string PropertyEditorAlias = "N3O.Umbraco.EditorJs";
```

This string is the glue between the frontend UI and the backend data editor. If they do not match, the data type created in the Umbraco backoffice will show a blank editor, or the data type will not be able to find the UI at all. The `propertyEditorSchemaAlias` in `meta` must also match: it tells Umbraco which C# `[DataEditor]` provides the schema (configuration options) for this UI.

Think of it like a WCF contract name or a .NET serialization type discriminator: the string ties together two independently deployed pieces.

**`element`** points to the JS file that Umbraco will load in a `<script type="module">` tag. The path is relative to the site root. The custom element (`<n3o-editor-js>`) that is defined inside `editor-js.js` is registered via `@customElement('n3o-editor-js')` (the `customElements.define()` call is emitted by the `@customElement` decorator at module load time). Umbraco then renders `<n3o-editor-js>` in the document.

---

## 5. Concepts demonstrated

| Concept | Where in this app |
|---|---|
| npm workspace + shared build tooling | `package.json` → `@n3o/build`; `tsconfig.json` extends `@n3o/build/tsconfig` |
| Vite external vs. bundled modules | `react: true` in `n3oPluginConfig`; EditorJS packages bundled; React/Umbraco external |
| ES module import maps | Runtime resolution of `react`, `react-dom`, `@umbraco-cms/*` |
| Web component + shadow DOM | `N3oEditorJsElement`, `attachShadow`, `connectedCallback/disconnectedCallback` |
| Umbraco context plumbing | `UmbElementMixin`, `consumeContext(UMB_MODAL_MANAGER_CONTEXT, ...)` |
| N3O bridge pattern | Shell `editor-js.ts` + React app `editor-js-app.tsx` + bridge object |
| React `useEffect` with empty deps | Wraps imperative EditorJS init; cleanup function destroys EditorJS on unmount |
| Stale-closure prevention via refs | `valueRef`, `onChangeRef`, `bridgeRef` updated each render |
| TypeScript ambient module declarations | `vendor.d.ts` for untyped packages |
| JSX IntrinsicElements augmentation | `uui-react.d.ts` for custom element tags in TSX |
| EditorJS block tool interface | `UmbracoImageTool`: `render()`, `save()`, `validate()`, `static get toolbox()` |
| EditorJS inline tool interface | `UmbracoLinkTool`: `isInline`, `surround()`, `checkState()`, `static get sanitize()` |
| Factory function for dependency injection | `makeUmbracoImageTool(openMediaPicker)`, `makeUmbracoLinkTool(openLinkPicker)` |
| Extending a class typed as `any` | `EmbedWithUI extends (Embed as new (...args: any[]) => any)` |
| Umbraco modal manager | `modalManager.open(host, UMB_MEDIA_PICKER_MODAL, ...)`, `await modal.onSubmit()` |
| Umbraco repositories | `UmbMediaUrlRepository`, `UmbMediaItemRepository` — resolve GUIDs, must be destroyed after use |
| CSS `?inline` import | Vite feature; injects CSS as a string into shadow DOM via `<style>` |
| `propertyEditorUi` manifest + alias rule | `umbraco-package.json` `alias` must match `[DataEditor]` string in C# |

---

## 6. Gotchas

### The sanitizer config on `new EditorJS({sanitizer: ...})` has limited effect

The EditorJS v2.x documentation suggests a global `sanitizer` option on the constructor. In practice, this option is **only applied to certain paste/clipboard flows** and is **not applied during `editor.save()`**. The `save()` call uses each tool's `static get sanitize()` property exclusively. The code correctly acknowledges this in a comment:

```ts
// TODO: Not working — the global `sanitizer` option in EditorJS v2.x is NOT applied
// during save(); save() uses each tool's static `sanitize` property exclusively.
// The global option only affects certain paste/clipboard flows. To enforce per-output
// HTML sanitization, add a `static get sanitize()` to each tool class. See TECH_DEBT
// entry F-24 in TECH_DEBT_AND_MODERNIZATION.md for the full investigation.
sanitizer: { a: {} },
```

`UmbracoLinkTool` does implement `static get sanitize()` correctly (it declares `a: { href: true }`). Other tools (UmbracoImageTool, EmbedWithUI) do not have `static get sanitize()` blocks. This is tracked as tech-debt F-24.

### React and Umbraco UI Library interactive controls do not mix

Interactive `uui-*` elements (buttons, inputs, toggles, selects) rendered from within a React component inside the shadow DOM do not mount correctly in Umbraco 17 — they appear blank and log console errors. This is because UUI elements use Lit's `FormControlMixin`, which expects a specific DOM lifecycle Lit provides but React does not. The `uui-react.d.ts` file explicitly excludes these elements from the IntrinsicElements declarations. Use native HTML controls (`<button>`, `<input>`, etc.) inside `<uui-box>` wrappers. The fullscreen button in `editor-js-app.tsx` is a native `<button>` for exactly this reason.

### `UmbMediaUrlRepository` and `UmbMediaItemRepository` must be destroyed

Both repositories register background controllers on the host element. If you do not call `.destroy()` after each use, the registrations accumulate with every image the editor picks. The `try/finally` block in `openMediaPicker` ensures `destroy()` is always called even if URL/item resolution fails.

### Modal pickers need the `host` element as the opener

`modalManager.open(host, ...)` requires the shell element (`<n3o-editor-js>`) as the first argument. The React app cannot call `modalManager.open` with a React component as the opener — React components are not DOM elements. This is why the shell element reference is threaded through the `EditorJsHostBridge` all the way into the `useEffect` closure. If you omit the host or pass `null`, the modal may still open but will fail to set up its context chain correctly, which can cause context-consumer errors in the opened modal.

### `useEffect` must have empty deps `[]` — do not add `value` to the dep array

EditorJS is an imperative editor with its own internal state. Re-initialising it when `value` changes (i.e., making `value` a `useEffect` dependency) would destroy and recreate the entire editor on every keystroke, because every keystroke fires `onChange → editor.save() → onChangeRef.current(json) → props.onChange(json)`, which causes a re-render with a new `value` prop. The workaround is the ref pattern: `valueRef.current = value` is updated each render, but the `useEffect` reads only `valueRef.current` (at init time, once, for the initial data) and is never re-triggered.

### The alias string is the only link between frontend and backend

There is no compile-time check that the `alias` in `umbraco-package.json` matches `EditorJsConstants.PropertyEditorAlias` in C#. If you rename one without the other, the data type silently breaks: existing content nodes that use this data type will show no editor UI, and the data may be stored but not convertible. Always grep for the alias string in both places when renaming.

### EditorJS tool injection via factory, not constructor injection

EditorJS instantiates tools by calling `new ToolClass(args)` internally — you cannot pass extra constructor arguments. The factory pattern (`makeUmbracoImageTool(openMediaPicker)`) is the correct way to inject dependencies into a tool class. Do not try to pass additional config through EditorJS's `config` field for object references that need to hold closures (the `config` field is intended for plain data, not function references, though it would technically work).
