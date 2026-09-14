# AGENTS.md — n3oltd/umbraco-extensions

Orientation for AI agents working in this repo. Read this before diving in; it captures things that are expensive to rediscover.

## What this is

Open-source **extensions to Umbraco 17** (the CMS), published as NuGet packages under the `N3O.Umbraco.*` namespace and npm packages under `@n3oltd/*`. It is a large multi-project .NET solution plus a frontend npm workspace. Umbraco 17 runs on **.NET 10**; its backoffice is a **Lit / web-component** SPA (the "Bellissima" backoffice) using the **UUI** component library (`@umbraco-ui/*`, re-exported via `@umbraco-cms/backoffice/external/uui`).

## Migration in progress (read before you build)

The repo is **mid-migration from Umbraco 13/.NET 8 to Umbraco 17/.NET 10**. At any time some projects are `net8.0` and some `net10.0` (`grep -l TargetFramework` to check). Consequences:

- A `net8.0` project **cannot** reference a `net10.0` project; `net10.0 → net8.0` is fine. So the solution often does **not** build end-to-end. The `DemoSite` host is usually un-buildable for this reason.
- To run/eyeball a migrated plugin, prefer a **minimal net10 host** that references only net10-clean projects, or copy the built `App_Plugins` into a vanilla Umbraco host (the plugin's backoffice is pure static frontend + manifests). `N3O.Umbraco.ReactRuntime`'s project closure is net10-clean and small.
- The `v17` branch is ahead of older feature branches — check it for already-migrated projects before migrating one yourself.

## Frontend / React client architecture

Backoffice plugins are migrating from AngularJS to **React 19**, mounted as Lit custom elements inside the backoffice. Structure under `src/`:

- **npm workspace** (`src/package.json`, name `@n3oltd/client-root`) orchestrated by **Turborepo** (`turbo run build`), each package built by **Vite** to `dist/`, then MSBuild copies `dist/` → `wwwroot/App_Plugins/` (see `Directory.Build.targets`). Built JS is gitignored. **9 workspace packages total.**
- `src/frontend/build-config` (`@repo/build-config`) — shared Vite/TS config; extend tsconfig **by name** (`@repo/build-config/tsconfig[-react]`).
- `src/frontend/backoffice-core` (`@n3oltd/backoffice-core`) — shared Lit/TS (auth-fetch + the workspace-visibility condition). **Externalized** and shipped as its own App_Plugins bundle (resolved at runtime via the import map). Built to `dist/N3O.Umbraco.BackofficeCore/` and **shipped by the ReactRuntime project** (via `N3OFrontendExtraDistDir`) into `App_Plugins/N3O.Umbraco.BackofficeCore`.
- `src/frontend/backoffice-ui` (`@n3oltd/backoffice-ui`) — **shared React wrappers over UUI** (see below). **Bundled** into each consuming app (not externalized).
- `N3O.Umbraco.ReactRuntime` is its **OWN .NET project** at `src/N3O.Umbraco.ReactRuntime/`, with its frontend at `src/N3O.Umbraco.ReactRuntime/frontend/react-runtime/` (pkg `@n3oltd/react-runtime`). It ships React 19 as a single shared ESM bundle and **publishes the import map** (`react`, `react-dom`, `react-dom/client`, `react/jsx-runtime` are **externalized** and resolved to this one instance; they are `peerDependencies` in every React plugin). It also ships `backoffice-core`'s dist into `App_Plugins/N3O.Umbraco.BackofficeCore`.
- Exemplar plugin: `Data/N3O.Umbraco.Data/frontend/{data-export,data-import,...}` — the template every other plugin follows. (A Lit, non-React example is `src/N3O.Umbraco.Cms/frontend/dynamic-list-views`.)

npm dependency convention for these **private, bundled** workspace packages: build-time inputs (incl. `@types/*`, `@repo/build-config`, sibling `@n3oltd/*`) go in `devDependencies`; only the externalized runtime singletons (`react`/`react-dom`) are `peerDependencies`; `dependencies` is effectively unused.

## ⚠️ The UUI-in-React gotcha (the single most important thing here)

UUI's **form-associated** custom elements — `uui-select`, `uui-checkbox`, `uui-toggle`, `uui-radio-group`, `uui-button`, `uui-input` (and anything else extending `UUIFormControlMixin`) — **set an attribute in their constructor**: the mixin's constructor sets `pristine = true`, whose setter calls `this.setAttribute("pristine", "")` (`@umbraco-ui/uui-base/lib/mixins` — the source even comments that this is a problem). Per the Custom Elements spec that is legal when the **HTML parser** upgrades the element (how Lit's `html\`\`` templates and the native backoffice build them) but **illegal for synchronous `document.createElement`** — which is exactly how **React renders host elements**. So rendering `<uui-select/>` in JSX throws `NotSupportedError: The result must not have attributes`, the element never upgrades, and it renders as an invisible 0-height inert tag. **This is silent — no build/type error; it only shows in a real browser.**

Non-form-associated UUI elements (`uui-box`, `uui-icon`, `uui-loader-bar`, `uui-radio`, `uui-file-dropzone`) do NOT do this and are fine in plain JSX.

**The fix (implemented in `@n3oltd/backoffice-ui`):** form-associated controls are created via the parser path — `useUuiControl(html)` → `createContextualFragment` into a `display:contents` host — and driven via the element with `useProperty` / `useEvent`. Consumers just use plain props (`<UuiSelect options value onChange/>`); they never see this. Note `@lit/react`'s `createComponent` does **not** fix it — it also goes through `React.createElement`.

### Extending the library with a NEW UUI component (read this before adding a wrapper)

1. **Decide JSX vs parser.** Run, in the live backoffice console: `customElements.get('uui-foo')?.formAssociated`. `true` → it WILL throw under React; wrap it with `useUuiControl`. `false` → JSX works, but using `useUuiControl` anyway is fine for a uniform host pattern (that's why `UuiFileDropzone` uses it despite not being form-associated).
2. **Drive state through the element, not attributes.** UUI exposes state as DOM *properties* (`.checked`/`.value`/`.options`) and emits *custom* events — use `useProperty(element, name, value)` and `useEvent(element, type, handler)`. `useUuiControl` returns the element as **state** (not a stable ref) so that if `html` changes and React rebuilds the element, the property/event effects re-run and re-sync it — don't "optimise" that back to a ref.
3. **Per-component traps already hit (check the source before assuming an API):**
   - `uui-radio-group`: applying `.value` after mount can land before the radios register via the async `slotchange`, selecting nothing — set the matching `uui-radio`'s `.checked` directly (see `UuiRadioGroup`).
   - `uui-file-dropzone`: no built-in click-to-browse → call `.browse()` on host click; no `disabled` property → gate click AND drop on the host; files arrive on `change` as `event.detail.files`.
   - `uui-button`: it's form-associated (submit/reset); with a slotted icon the visible text must be in the slot, while the `label` property remains the accessible name.
   - Components are auto-registered by the backoffice at boot — don't import them to register. Inside a property editor, let `umb-property-layout` own the label; don't repeat it on the control.
4. **Feedback uses the native notification system,** not custom panels: consume `UMB_NOTIFICATION_CONTEXT` and `peek('positive'|'warning'|'danger', { data: { headline, message } })` (see the host `.ts` elements). `message` is required.

### How these gotchas were found (debugging playbook — reuse it)

The createElement gotcha cost real time because **types and the build were green while the control rendered blank.** The playbook that cracked it:
- **Symptom → suspicion:** a control is invisible / 0-height but no console error, OR a `NotSupportedError: The result must not have attributes` from `react-dom`. That signature = a custom element React tried to `createElement` whose constructor set an attribute.
- **Drive the real backoffice** (the Playwright MCP — `.mcp.json` wires `@playwright/mcp` with `--browser chrome --ignore-https-errors`). Build/type-check is NOT enough; you must look in a browser.
- **Detect un-upgraded elements:** in the page, `el.shadowRoot === null` / `el.offsetHeight === 0` on a defined custom element means its upgrade threw. Confirm the cause with the A/B test: `document.createElement('uui-foo')` (throws/blank) vs `host.innerHTML = '<uui-foo>'` (parser path — upgrades fine). Then `customElements.get('uui-foo').formAssociated` and grep the `@umbraco-ui/*` source for the constructor `setAttribute`.
- **Caveat when testing file uploads under automation:** Playwright *intercepts* file-chooser dialogs, so a working file picker will NOT visibly open in the automation browser (and repeated clicks queue phantom "file chooser" modal states). That's an automation artifact, not an app bug — verify the picker visually in a normal browser, or assert the file reached state another way.

## Build / verify

- Frontend only: `cd src && npm ci && npx turbo run build --env-mode=loose` (9 packages; each app runs `tsc --noEmit` then Vite).
- The MSBuild ↔ npm seam lives in BOTH root `Directory.Build.props` (NEW; sets `N3OHasFrontend` for any project dir containing a `frontend/` folder, and `StaticWebAssetBasePath`) AND root `Directory.Build.targets` (targets `RestoreFrontendDependencies`, `BuildFrontendWorkspace` = a single `npx turbo run build --env-mode=loose` for the whole workspace, and `BuildFrontend` = per-project copy of `dist/` → `wwwroot/App_Plugins/`). A project opts into the ReactRuntime ProjectReference via `src/build/N3O.Umbraco.ReactPlugin.props`, auto-imported when it sets `<N3OReactPlugin>true</N3OReactPlugin>`.
- Version consistency across the workspace is enforced by `src/.syncpackrc.json` (`npm run lint:versions`).
- Lockfile must keep `esbuild`/`@rollup/rollup-*` platform optional-deps (~26/~25 entries) or `npm ci` fails — regenerate with a plain `npm install` (no `omit=optional`).
- **Type/build pass ≠ working.** The form-associated-UUI bug only shows in a real browser. Verify UI changes in a running backoffice (drive it with the Playwright MCP). Caveat: an automation-controlled browser **intercepts file-chooser dialogs**, so file pickers won't visibly open there — that's expected, not a bug.

## ⚠️ Known stale coexistence (cleanup pending)

The merge kept some `v17-Talha`-only files that the old layout used; they are STILL git-tracked but are NOT part of the Turbo workspace and no project builds them. Treat as legacy/to-delete:
- `src/N3O.Umbraco.Cms/Extensions/N3O.Umbraco.BackofficeCore` (old pkg `@n3o/backoffice-core`)
- `src/N3O.Umbraco.Cms/Extensions/N3O.Umbraco.DynamicListViews`
- `src/N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig` (old pkg `@n3o/build`)
- `src/N3O.Umbraco.Cms/Build/N3O.Umbraco.ReactRuntime`
- `src/Data/N3O.Umbraco.Data.StaticAssets/Extensions/*` (+ its `wwwroot/App_Plugins` copies)

## Pointers

- The ReactRuntime foundation branch — the React-client foundation + Data exemplar (verify PR numbers against `MIGRATION_PR_TRACKER.md`).
- The UUI-wrapper refactor of the Data apps, stacked on the foundation branch (verify PR numbers against `MIGRATION_PR_TRACKER.md`).
- Work issue: n3oltd/work#729 (Umbraco 17 / .NET 10 migration).
