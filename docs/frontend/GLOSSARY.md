# Glossary

Every term/acronym used across the frontend docs, in one place. Each entry links to the concept doc that explains it in depth. Ordered roughly by topic, then alphabetically within.

## Tooling & packaging

| Term | Plain meaning (with C# analogy) | See |
|---|---|---|
| **Node.js** | A JavaScript runtime that runs outside the browser. Here it's used **only at build time** (the toolchain), never at runtime. ≈ the .NET SDK/CLI host. | [03](concepts/03-node-npm-and-the-workspace.md) |
| **npm** | Node's package manager + registry. ≈ NuGet + `dotnet restore`. | [03](concepts/03-node-npm-and-the-workspace.md) |
| **`package.json`** | A project/package manifest: name, scripts, dependencies. ≈ `.csproj` + `packages.config`. | [03](concepts/03-node-npm-and-the-workspace.md) |
| **`package-lock.json`** | Exact locked dependency versions. ≈ `packages.lock.json`. | [03](concepts/03-node-npm-and-the-workspace.md) |
| **`node_modules`** | The restored packages on disk (git-ignored, hoisted to `src/`). ≈ the restored `~/.nuget` + `obj` graph. | [03](concepts/03-node-npm-and-the-workspace.md) |
| **dependency vs devDependency** | Runtime vs build-time-only packages. Here almost everything is a **devDependency** because only the *built output* ships. | [03](concepts/03-node-npm-and-the-workspace.md) |
| **npm workspace** | One repo (`src/`) holding many small packages sharing a single `node_modules`/lockfile. ≈ a `.sln` with project references. | [03](concepts/03-node-npm-and-the-workspace.md) |
| **`npm ci` / `npm install`** | Restore from the lockfile (ci = clean, exact) vs add/update. ≈ `dotnet restore`. | [03](concepts/03-node-npm-and-the-workspace.md) |
| **`@n3o/build`** | This repo's shared Vite + TypeScript config package (folder `BuildConfig`). | [buildconfig](apps/buildconfig.md), [05](concepts/05-vite-and-the-build.md) |
| **Vite** | The frontend build tool (bundler/compiler). ≈ MSBuild + Roslyn for the frontend. | [05](concepts/05-vite-and-the-build.md) |
| **bundler** | A tool that turns many source modules into a few optimized output files. | [05](concepts/05-vite-and-the-build.md) |
| **library mode** | A Vite build that emits a reusable `.js` module (not a standalone web app). All apps here use it. | [05](concepts/05-vite-and-the-build.md) |
| **external** | A dependency Vite is told NOT to bundle — expected to exist at runtime (via the import map). | [05](concepts/05-vite-and-the-build.md), [04](concepts/04-es-modules-and-import-maps.md) |
| **sourcemap** | A `.js.map` file mapping built JS back to TS source for debugging. ≈ a `.pdb`. | [05](concepts/05-vite-and-the-build.md) |
| **`?inline` CSS import** | Vite turns `import s from './x.css?inline'` into the raw CSS *string* (for injection into Shadow DOM). | [05](concepts/05-vite-and-the-build.md), [06](concepts/06-web-components-and-shadow-dom.md) |
| **`Directory.Build.targets`** | The repo-root MSBuild file that runs `npm ci` + `npm run build` during `dotnet build`. | [05](concepts/05-vite-and-the-build.md) |
| **`App_Plugins`** | The `wwwroot/App_Plugins/<id>/` folder where each app's built JS is served from and where Umbraco looks for plugins. | [01](concepts/01-the-big-picture.md) |

## Language & modules

| Term | Plain meaning (with C# analogy) | See |
|---|---|---|
| **JavaScript (JS)** | The browser's scripting language. | [02](concepts/02-javascript-typescript-for-csharp-devs.md) |
| **TypeScript (TS)** | JS + static types; compiles/erases to JS. ≈ C# is to IL, loosely. | [02](concepts/02-javascript-typescript-for-csharp-devs.md) |
| **`tsconfig.json`** | TypeScript compiler config. ≈ the `<PropertyGroup>` compiler settings in a `.csproj`. | [02](concepts/02-javascript-typescript-for-csharp-devs.md), [buildconfig](apps/buildconfig.md) |
| **`.d.ts`** | A declaration (types-only) file — no runtime output. ≈ a reference assembly / type stub. | [02](concepts/02-javascript-typescript-for-csharp-devs.md) |
| **structural typing** | TS matches types by *shape*, not by name (unlike C#'s nominal typing). | [02](concepts/02-javascript-typescript-for-csharp-devs.md) |
| **`null` vs `undefined`** | Two "no value" values; `?.` (optional chaining) and `??` (nullish coalescing) handle them. | [02](concepts/02-javascript-typescript-for-csharp-devs.md) |
| **`as` cast** | A **compile-time-only** type assertion (no runtime check, unlike a C# cast). | [02](concepts/02-javascript-typescript-for-csharp-devs.md) |
| **arrow function `=>`** | A concise function literal. ≈ a C# lambda. | [02](concepts/02-javascript-typescript-for-csharp-devs.md) |
| **`Promise` / `async` / `await`** | Async results. `Promise<T>` ≈ `Task<T>`; `async`/`await` work the same. | [02](concepts/02-javascript-typescript-for-csharp-devs.md) |
| **destructuring / spread `...`** | Pull fields out of / copy objects & arrays. | [02](concepts/02-javascript-typescript-for-csharp-devs.md) |
| **DOM** | The live tree of page elements the browser renders; JS manipulates it. | [02](concepts/02-javascript-typescript-for-csharp-devs.md) |
| **ES Module (ESM)** | The modern `import`/`export` module system. | [04](concepts/04-es-modules-and-import-maps.md) |
| **CommonJS (CJS)** | The older `require`/`module.exports` system; React ships as CJS, hence the runtime shim. | [04](concepts/04-es-modules-and-import-maps.md), [reactruntime](apps/reactruntime.md) |
| **bare specifier** | An import like `'react'` or `'@umbraco-cms/backoffice'` with no path — needs resolving. | [04](concepts/04-es-modules-and-import-maps.md) |
| **import map** | A `<script type="importmap">` the browser uses to resolve bare specifiers to URLs. Umbraco builds it from manifests. | [04](concepts/04-es-modules-and-import-maps.md) |
| **mixin** | A function `superClass => class extends superClass {…}` that adds behaviour to a class. ≈ a reusable base-class fragment / trait. | [backofficecore](apps/backofficecore.md) |

## Web components & frameworks

| Term | Plain meaning (with C# analogy) | See |
|---|---|---|
| **Web Component / Custom Element** | A custom HTML tag backed by a `class extends HTMLElement`, registered with `customElements.define`. ≈ a reusable UI control. | [06](concepts/06-web-components-and-shadow-dom.md) |
| **lifecycle callbacks** | `connectedCallback` (added to page ≈ OnLoad), `disconnectedCallback` (removed ≈ Dispose), `attributeChangedCallback`. | [06](concepts/06-web-components-and-shadow-dom.md) |
| **Shadow DOM** | An encapsulated DOM+CSS subtree inside an element (styles don't leak in/out). | [06](concepts/06-web-components-and-shadow-dom.md) |
| **light DOM** | The element's normal (non-shadow) children. | [06](concepts/06-web-components-and-shadow-dom.md) |
| **`<slot>`** | A placeholder for projected child content. ≈ Razor `@RenderBody()`. | [06](concepts/06-web-components-and-shadow-dom.md) |
| **`adoptedStyleSheets`** | The API to attach a constructed stylesheet to a shadow root (instead of a `<style>` tag). | [06](concepts/06-web-components-and-shadow-dom.md) |
| **`CustomEvent`** | A DOM event you create + `dispatchEvent`; `composed: true` lets it cross shadow boundaries. | [06](concepts/06-web-components-and-shadow-dom.md) |
| **Lit / `LitElement`** | A thin library over web components (reactive properties + `html\`\`` templates) that Umbraco's backoffice is built with. | [07](concepts/07-lit.md) |
| **`html\`\`` (tagged template)** | Lit's way to declare a template; updates the DOM efficiently. | [07](concepts/07-lit.md) |
| **reactive property / state** | A Lit field (`@property`/`@state`) that re-renders the element when it changes. ≈ `INotifyPropertyChanged`. | [07](concepts/07-lit.md) |
| **`UmbElementMixin` / `UmbLitElement`** | Umbraco's base/mixin adding `consumeContext` + `observe`. | [07](concepts/07-lit.md) |
| **`consumeContext(TOKEN, cb)`** | How a backoffice element gets a shared service/value from an ancestor. ≈ DI resolve via the element tree. | [07](concepts/07-lit.md), [09](concepts/09-umbraco-backoffice-extensions.md) |
| **`observe(observable, cb)`** | Subscribe to a backoffice observable (auto-cleaned). ≈ `IObservable<T>.Subscribe`. | [07](concepts/07-lit.md) |
| **React** | A UI library: UI = a function of state; you describe the UI, React updates the DOM. | [08](concepts/08-react.md) |
| **JSX / TSX** | HTML-like syntax inside JS/TS that compiles to `jsx(...)` calls. `.tsx` = TS + JSX. | [08](concepts/08-react.md) |
| **component** | A function returning JSX. ≈ a partial view / control. | [08](concepts/08-react.md) |
| **props** | A component's read-only inputs. ≈ constructor params / a DTO. | [08](concepts/08-react.md) |
| **state / `useState`** | A component's mutable, render-triggering data (never mutated in place). | [08](concepts/08-react.md) |
| **`useEffect`** | An **escape hatch** to synchronize with the outside world (fetch, subscriptions); has a cleanup + dependency array. NOT for deriving data. | [08](concepts/08-react.md) |
| **`useRef`** | A mutable box that survives re-renders (also holds DOM references). | [08](concepts/08-react.md) |
| **Rules of Hooks** | Call hooks (`use*`) only at the top level, same order every render. | [08](concepts/08-react.md) |
| **custom hook** | A reusable `use*` function that composes other hooks. | [08](concepts/08-react.md), [data-export](apps/data-export.md) |
| **`createRoot(el).render(<App/>)`** | Mounts a React tree into a DOM node — what every shell calls. | [08](concepts/08-react.md), [10](concepts/10-the-n3o-bridge-pattern.md) |
| **controlled input** | A form input whose value is driven by React state. | [08](concepts/08-react.md) |
| **`key`** | A stable identity for list items so React can reconcile efficiently. | [08](concepts/08-react.md) |
| **`AbortController`** | Cancels an in-flight `fetch`. ≈ `CancellationTokenSource`. | [data-import](apps/data-import.md) |

## Umbraco backoffice

| Term | Plain meaning | See |
|---|---|---|
| **backoffice** | The Umbraco admin UI — in v17 a client-side web-component app you extend with JS. | [09](concepts/09-umbraco-backoffice-extensions.md) |
| **`umbraco-package.json`** | The **manifest** that registers a plugin's extensions + import-map entries — the entry point Umbraco reads. | [09](concepts/09-umbraco-backoffice-extensions.md) |
| **extension** | A registered unit of backoffice UI/behaviour (an entry in the manifest's `extensions[]`). | [09](concepts/09-umbraco-backoffice-extensions.md) |
| **propertyEditorUi** | An extension type: the UI for editing a content property. | [09](concepts/09-umbraco-backoffice-extensions.md), [cells](apps/cells.md) |
| **propertyEditorSchema** | The server-side property editor definition the UI pairs with. | [09](concepts/09-umbraco-backoffice-extensions.md) |
| **the alias rule** | A `propertyEditorUi` alias **must equal** the C# `[DataEditor]` alias, or the data type breaks. | [09](concepts/09-umbraco-backoffice-extensions.md) |
| **workspaceView** | An extension type: a tab on a document/media workspace. | [09](concepts/09-umbraco-backoffice-extensions.md), [data-export](apps/data-export.md) |
| **dashboard** | An extension type: a panel on a section's dashboard. | [09](concepts/09-umbraco-backoffice-extensions.md), [welcomedashboard](apps/welcomedashboard.md) |
| **condition** | A rule that gates when an extension is shown (built-in `Umb.Condition.*` or custom `N3O.Condition.WorkspaceVisibility`). | [09](concepts/09-umbraco-backoffice-extensions.md), [backofficecore](apps/backofficecore.md) |
| **context** | A shared service/value provided down the element tree (e.g. `UMB_AUTH_CONTEXT`, `UMB_DOCUMENT_WORKSPACE_CONTEXT`). | [09](concepts/09-umbraco-backoffice-extensions.md) |
| **`UMB_AUTH_CONTEXT`** | The context that yields the OAuth bearer token + API config for authenticated calls. | [backofficecore](apps/backofficecore.md) |
| **`uui-*` / `umb-*`** | Umbraco's web-component UI library (`uui-box`, `uui-button`, `umb-property-layout`, …) — web components, NOT React. | [09](concepts/09-umbraco-backoffice-extensions.md) |
| **`uui-react.d.ts`** | Per-app declaration file letting TSX use `uui-*` tags as JSX elements. | [10](concepts/10-the-n3o-bridge-pattern.md) |

## N3O-specific

| Term | Plain meaning | See |
|---|---|---|
| **the bridge pattern** | The recurring architecture: a web-component **shell** consumes Umbraco contexts and **mounts a React app** (`createRoot`), props down + callbacks up. | [10](concepts/10-the-n3o-bridge-pattern.md) |
| **ReactRuntime** | The package providing the **single shared copy** of React for all plugins (via the import map). | [reactruntime](apps/reactruntime.md) |
| **`@n3o/backoffice-core`** | Shared `auth-fetch` (authenticated fetch) + the reusable `N3O.Condition.WorkspaceVisibility`. | [backofficecore](apps/backofficecore.md) |
| **`createAuthFetch` / `UmbAuthFetchMixin`** | Helpers that attach the bearer token so calls to `[Authorize]` backoffice API controllers don't 401. | [backofficecore](apps/backofficecore.md) |
| **the uui-form-control gotcha** | Interactive `uui-*` form controls break when rendered **by React** in v17 — use native HTML controls inside `uui-box`/`umb-property-layout` instead. | [10](concepts/10-the-n3o-bridge-pattern.md) |

---

*See [README](README.md) for the recommended reading order.*
