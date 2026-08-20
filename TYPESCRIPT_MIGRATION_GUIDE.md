# Bellissima Plugin → TypeScript + Vite Migration Guide

*Shared reference for all plugin-migration subagents. Created 2026-06-02 (session 6).*

You are converting **one Umbraco backoffice plugin project** from plain-JS Lit components to **TypeScript +
Vite**, the modern Umbraco-recommended build. The backend C# APIs and the `umbraco-package.json` registration
are **unchanged** — you only add a typed build pipeline and rewrite the component(s) in TypeScript.

A complete, verified-building reference already exists: **`src/N3O.Umbraco.Cms/frontend/dynamic-list-views`**
(a Lit, no-React `workspaceView`). **Read it first and mirror it exactly.** Each plugin app now lives at
`<Project>/frontend/<app>/` inside the single npm + Turborepo workspace rooted at `src/`, and builds via the
shared root `Directory.Build.targets` (turbo) — no per-project MSBuild target. It emits to `dist/` and the
build seam copies it to `App_Plugins/...`.

---

## The recipe (mirror dynamic-list-views)

For each plugin, create a `frontend/<app>/` folder under its project (e.g.
`src/N3O.Umbraco.Cms/frontend/dynamic-list-views`). The config files are tiny because they extend / call the
shared `@repo/build-config` preset (`src/frontend/build-config`). It contains:

### `frontend/<app>/package.json`
```json
{
    "name": "@n3oltd/<app>",
    "version": "1.0.0",
    "private": true,
    "type": "module",
    "scripts": {
        "build": "tsc --noEmit && vite build",
        "watch": "vite build --watch"
    },
    "devDependencies": {
        "@repo/build-config": "*"
    }
}
```
Add any **third-party libs** the plugin uses as real dependencies here (see "Third-party libraries" below).
`@umbraco-cms/backoffice`, `typescript` and `vite` come from the shared workspace — don't re-pin them per app.

### `frontend/<app>/tsconfig.json`
Extend the shared base preset by name (no per-app compilerOptions boilerplate):
```json
{
    "extends": "@repo/build-config/tsconfig",
    "include": ["src"]
}
```

### `frontend/<app>/vite.config.ts` — SINGLE-entry plugin
Import `{ n3oPluginConfig }` from `@repo/build-config` and call it. The entry **key** is the
`App_Plugins/<folder>/<file>` sub-path; `outDir: 'dist'` and the `BuildFrontend` target maps `dist/**` →
`wwwroot/App_Plugins/...`:
```ts
import { n3oPluginConfig } from '@repo/build-config';

export default n3oPluginConfig({
    entries: {
        'N3O.Umbraco.DynamicListViews/dynamic-list-view': 'src/dynamic-list-view.ts',
    },
    outDir: 'dist',
});
```
- The entry key's `<file>.js` MUST match the filename the `umbraco-package.json` `element`/`js` path points at.
- The preset keeps `@umbraco-cms/backoffice/*` imports as bare external specifiers (Umbraco import-maps them at
  runtime). Everything else (your code + npm libs) is bundled into the output file.
- Output goes to `dist/<folder>/<file>.js`; the build seam copies it into `App_Plugins/<folder>/`.

### `frontend/<app>/vite.config.ts` — MULTI-entry project (e.g. Data has 4 plugins in one project)
Add one entry per plugin to the `entries` map (key = its `App_Plugins/<folder>/<file>` path):
```ts
import { n3oPluginConfig } from '@repo/build-config';

export default n3oPluginConfig({
    entries: {
        'N3O.Umbraco.Data.Import/data-import': 'src/data-import.ts',
        'N3O.Umbraco.Data.Export/data-export': 'src/data-export.ts',
        'N3O.Umbraco.Data.ImportDataEditor/import-data-editor': 'src/import-data-editor.ts',
        'N3O.Umbraco.Data.ImportNoticesViewer/import-notices-viewer': 'src/import-notices-viewer.ts',
    },
    outDir: 'dist',
});
```
(Each key resolves to `dist/<folder>/<file>.js`, then copied to `App_Plugins/<folder>/<file>.js`.) Confirm
each path matches that plugin's `umbraco-package.json` `element`. (For a React app, also pass `react: true` and
`additionalExternals: ['@n3oltd/backoffice-core']` — see the React guide.)

---

## Build wiring (no per-project MSBuild target)

There is **no per-project `BuildClientApp` csproj target** anymore. The shared root `Directory.Build.targets`
drives the whole workspace and is gated on `N3OHasFrontend` (auto-set by `Directory.Build.props` for any
project containing a `frontend/` folder):

- `RestoreFrontendDependencies` — `npm ci` once in `src`.
- `BuildFrontendWorkspace` — a single `npx turbo run build --env-mode=loose` for the whole workspace.
- `BuildFrontend` — per-project copy of `frontend/*/dist/**` into `wwwroot/App_Plugins/...`, re-registered for
  the static-web-assets pipeline.

So you do **not** edit the `.csproj` to add a build step or `<Content>` excludes — creating the
`frontend/<app>/` folder is enough for the shared targets to pick it up. (A React plugin opts into the
ReactRuntime ProjectReference by setting `<N3OReactPlugin>true</N3OReactPlugin>`, which auto-imports
`src/build/N3O.Umbraco.ReactPlugin.props`.)

---

## Writing the TypeScript component

Port the **existing** `App_Plugins/.../<file>.js` faithfully into `frontend/<app>/src/<file>.ts`. Same API calls,
same fields, same UX, same behaviour. **Do not add features or abstractions.** Modernize only the language:

- Use **Lit decorators**: `@customElement('n3o-...')`, `@property({...})`, `@state()` — imported from
  `@umbraco-cms/backoffice/external/lit` (it re-exports Lit + its decorators).
- Add **types**: implement the right Umbraco interface and type all params/returns/fields. Common ones:
  - Property editor UI: `implements UmbPropertyEditorUiElement`; `value` getter/setter; `set config(c: UmbPropertyEditorConfigCollection | undefined)`; dispatch `new UmbPropertyValueChangeEvent()` on change. Imports from `@umbraco-cms/backoffice/property-editor`.
  - Workspace view / content app: consume `UMB_DOCUMENT_WORKSPACE_CONTEXT` from `@umbraco-cms/backoffice/document`; `this.observe(...)`.
  - Block custom view: `implements UmbBlockEditorCustomViewElement` (or the element shape it had); imports from `@umbraco-cms/backoffice/block` / `.../block-custom-view`.
  - Dashboard / bundle / script: a plain `LitElement` (or the existing loader) — no special interface.
- Type DOM events: `(event.target as HTMLInputElement).value`, etc.
- Type `fetch` responses with a small `interface` for the JSON shape.
- Keep `export default <Class>` (Umbraco loads the default export) plus a named export.
- `UmbElementMixin(LitElement)` base, as the reference does.

`strict` is on. Prefer real Umbraco types. If a specific Umbraco generic is genuinely hard to satisfy, a
**narrow, commented** `as`-cast or a single `// @ts-expect-error <reason>` is acceptable rather than blocking —
keep it minimal and note it in your report. `skipLibCheck` is already on so the dependency's own types won't fail you.

---

## Third-party libraries

| Lib | Guidance |
|---|---|
| Available as a clean ESM npm package (`cropperjs`, `handsontable`, `@editorjs/*`, `editorjs-*`) | Add to `frontend/<app>/package.json` `dependencies`, `import` it in the `.ts`, let Vite bundle it. Delete the old vendored copy from `App_Plugins` (it becomes dead). Match the version the vendored copy used if discernible. |
| jQuery / Formstone (Cropper upload, Uploader) | **Pending product decision (do NOT rewrite to native pickers).** Keep the existing vendored files and the existing load mechanism; just port the wrapper to TS. If you bundle them, fine, but don't change behaviour. **Flag this in your report.** |
| Blazor loader (`N3O.Umbraco.Blazor.BackOffice.js`) | It's a non-Lit loader that relies on global `$`. Port to TS minimally (typed), keep it a `bundle`/`script` entry. Don't Lit-ify it. Flag the jQuery dependency. |

If a CSS file is imported (not inlined as `css\`\``), Vite emits a `style.css` — load it from the
`umbraco-package.json` or import it in the `.ts`; note any such change in your report.

---

## What you MUST NOT change
- `umbraco-package.json` contents — especially each `propertyEditorUi.alias` and `meta.propertyEditorSchemaAlias`,
  which **must stay equal to the backend `[DataEditor]` alias** (a hard-won fix; changing it breaks existing data
  types). You may keep the file exactly as-is.
- The output filename/path the manifest points at.
- Backend endpoints, request/response shapes, behaviour, UX.
- Don't rewrite Cropper/Uploader to Umbraco native media/image pickers (pending Talha's decision).

## Verify before reporting done (REQUIRED)
1. `cd frontend/<app> && npm run build` → must succeed (tsc strict passes + Vite emits to `dist/`).
   (Run `npm ci` once in `src` first if the workspace isn't restored.)
2. Confirm the output `.js` landed at `dist/<FolderName>/<file>.js` and its `import`s of
   `@umbraco-cms/backoffice/*` are still **bare external specifiers** (grep the output).
3. Confirm the element is registered (the `@customElement('n3o-...')` tag name appears in the output) and
   `export ... default` is present.
4. `dotnet build <project>.csproj -c Debug` → **0 errors** (this exercises the shared frontend build seam
   end-to-end: turbo build + copy of `dist/**` into `App_Plugins/`).
5. Old hand-written `App_Plugins/.../<file>.js` is overwritten by the build seam (expected) — don't hand-delete it.

## Report back
Plugin/project name; files created (`frontend/<app>/*`); component(s) ported; third-party libs (npm-bundled vs kept
vendored, with versions); any `as`/`@ts-expect-error` casts used and why; CSS handling; `npm run build` result;
`dotnet build` result (errors/warnings count); anything uncertain or flagged.
