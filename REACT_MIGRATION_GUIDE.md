# Bellissima Plugin → React Migration Guide

*Shared reference for all React-migration subagents. Created 2026-06-02 (session 9).*

You are converting **one Umbraco backoffice plugin** from a **Lit + TypeScript + Vite** component to
**React + TypeScript + Vite**. The backend C# APIs and the `umbraco-package.json` registration are
**UNCHANGED** — you only replace the component implementation and its build deps.

A complete, verified-building reference already exists — **read it first and mirror it exactly**:
`src/Data/N3O.Umbraco.Data/frontend/data-import` (a React `workspaceView`). Each plugin app now lives at
`<Project>/frontend/<app>/` and is part of the single npm + Turborepo workspace rooted at `src/`.

---

## The architecture (non-negotiable)

Umbraco's Bellissima backoffice only loads **custom elements (web components)**. React cannot register
directly. So every plugin keeps a thin **web-component shell** that:
1. owns the Umbraco contract (the property-editor `value`/`config`, workspace context, etc.),
2. mounts a **React root** (`createRoot`) into its shadow root, and
3. bridges data **in** (Umbraco → React props) and changes **out** (React callback → Umbraco event).

**React is shared, not bundled.** `react`, `react-dom`, `react-dom/client`, `react/jsx-runtime` are kept
**external** in every plugin's Vite build and resolved at runtime by the import map declared in
`src/N3O.Umbraco.ReactRuntime/wwwroot/App_Plugins/N3O.Umbraco.ReactRuntime/umbraco-package.json` (a self-hosted
React 19 ESM runtime shipped by its own `N3O.Umbraco.ReactRuntime` project). **Never bundle React into a plugin** —
that would load multiple React instances.

> **Shim gotcha:** the runtime's shim files
> (`src/N3O.Umbraco.ReactRuntime/frontend/react-runtime/src/react.js`,
> `react-dom.js`, `react-jsx-runtime.js`) must re-export the named API **explicitly**
> (`import React from 'react'; export default React; export const { useState, ... } = React;`). A bare
> `export * from 'react'` drops React's CommonJS named exports through the Vite lib build — only `default`
> survives — so `import { useState } from 'react'` and JSX break at runtime. Keep the export list in sync
> with the installed React major.

---

## Build-file changes (mirror data-import's `frontend/<app>/`)

The app lives at `<Project>/frontend/<app>/` (e.g. `src/Data/N3O.Umbraco.Data/frontend/data-import`). Its
config files are tiny because they extend / call the shared `@repo/build-config` preset
(`src/frontend/build-config`). There is **no per-app duplicated** `tsconfig`/`vite` boilerplate and **no
per-project `BuildClientApp` csproj target** — the shared root `Directory.Build.targets`
(`RestoreFrontendDependencies` → `BuildFrontendWorkspace` → `BuildFrontend`) builds the whole workspace via
turbo and copies `frontend/*/dist/**` into `wwwroot/App_Plugins/`.

### `frontend/<app>/package.json`
React/react-dom are **peerDependencies** (resolved by the runtime import map, never bundled); pull in the
shared workspace packages + React types as devDeps:
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
    "peerDependencies": { "react": "^19.0.0", "react-dom": "^19.0.0" },
    "devDependencies": {
        "@repo/build-config": "*",
        "@n3oltd/backoffice-core": "*",
        "@n3oltd/backoffice-ui": "*",
        "@types/react": "^19.0.0",
        "@types/react-dom": "^19.0.0"
    }
}
```

### `frontend/<app>/tsconfig.json`
Extend the shared **react** preset by name (it sets `jsx: react-jsx`); nothing else needed:
```json
{
    "extends": "@repo/build-config/tsconfig-react",
    "include": ["src"]
}
```

### `frontend/<app>/vite.config.ts`
Import `{ n3oPluginConfig }` from `@repo/build-config` and call it. The entry **key** is the
`App_Plugins` sub-path; `outDir: 'dist'` and `BuildFrontend` maps `dist/**` → `wwwroot/App_Plugins/...`.
`react: true` externalizes `react`/`react-dom`/`react-dom/client`/`react/jsx-runtime` and enables JSX:
```ts
import { n3oPluginConfig } from '@repo/build-config';

export default n3oPluginConfig({
    entries: {
        'N3O.Umbraco.Data.Import/data-import': 'src/data-import.ts',
    },
    outDir: 'dist',
    react: true,
    additionalExternals: ['@n3oltd/backoffice-core'],
});
```
For MULTI-entry projects (e.g. Data has 4 plugins), add one entry per plugin in the `entries` map
(key = its `App_Plugins/<folder>/<file>` path); the preset still handles externals + JSX.

### `frontend/<app>/src/uui-react.d.ts` (copy from data-import)
JSX typings so `uui-*` web components compile inside TSX. Add any extra `uui-*` tags you use.

---

## The shell (`src/<plugin>.ts`) — keep it tiny

Port the EXISTING Lit element to a shell that mounts React. The shell file stays `.ts`, the React
component is a separate `.tsx`. The `@customElement('<existing-tag>')` tag name and the file name MUST
stay identical (the `umbraco-package.json` `element` path points at the built `<file>.js`).

**Property editor** (`UmbPropertyEditorUiElement`) — see the data-import-data-editor shell verbatim. Key points:
- `extends HTMLElement implements UmbPropertyEditorUiElement` (no Lit base needed), `attachShadow`, a mount div.
- `get/set value` + `set config` push into React via `#render()`.
- `createRoot` in `connectedCallback`, `unmount` in `disconnectedCallback`.
- on change, set `#value` and `this.dispatchEvent(new UmbPropertyValueChangeEvent())`.
- import `createElement` from `react`, `createRoot` from `react-dom/client` (both external).

**Workspace view / content app** — the shell must consume `UMB_DOCUMENT_WORKSPACE_CONTEXT`, so keep the
Lit base: `extends UmbElementMixin(HTMLElement)` (or `UmbLitElement`). In the constructor/`connectedCallback`,
`this.consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, (ctx) => { this.observe(ctx?.unique, (unique) => { this.#unique = unique; this.#render(); }); })`,
then pass `unique`/data as props into React. (The Lit base is only for context plumbing; React renders the UI.)

**Dashboard / blockEditorCustomView / bundle** — same shell pattern; pass the relevant inputs
(`content`/`settings`/`config` for block custom views) as React props.

---

## The React component (`src/<plugin>-app.tsx`)

- Port the Lit `render()` to JSX. Replace `@state()` with `useState`, `connectedCallback` fetches with
  `useEffect`, `.value=`/`@input=` with controlled `value`/`onChange`.
- The component is **controlled by the host**: `value` comes in as a prop; edits call `props.onChange(next)`,
  the host updates its `#value` and raises `UmbPropertyValueChangeEvent`, which re-renders with the new value.
  Single source of truth = the host element. Do NOT keep a parallel copy of the value in React state.
- **Hybrid UI:** use **UUI web components** (`uui-box`, `uui-button`, `uui-input`, `uui-label`, etc.) for
  backoffice-standard chrome/controls (consistent look), and custom React + the **frontend-design** skill
  only for genuinely bespoke surfaces (e.g. the SERP preview, the EditorJs canvas). Use the
  **vercel-react-best-practices** skill for the React code itself (no components-defined-in-components,
  narrow effect deps, derive don't store, etc.).
- **UUI controlled inputs in React 19:** `uui-input`/`uui-textarea` emit standard bubbling `input`/`change`
  events, so React's `onInput`/`onChange` (`(e) => (e.target as HTMLInputElement).value`) generally works.
  If an event doesn't fire, fall back to a `ref` + `useEffect(() => el.addEventListener('input', ...))`.
  Set string props as attributes; pass non-string props via `ref` (React 19 sets them as properties).
- Styling: a `<style>{css}</style>` in the JSX is rendered inside the shadow root (scoped). `--uui-*` CSS
  custom properties inherit through the shadow boundary, so use them for colors/spacing.

---

## What you MUST NOT change
- `umbraco-package.json` (especially each `propertyEditorUi.alias` / `meta.propertyEditorSchemaAlias`, which
  must equal the backend `[DataEditor]` alias — a hard-won fix), and the output filename/path it points at.
- Backend endpoints, request/response shapes, behaviour, UX.
- Do not bundle React. Do not rewrite Cropper/Uploader (out of scope this round).

## Verify before reporting done (REQUIRED)
1. `cd frontend/<app> && npm run build` → must succeed (tsc strict passes + Vite emits to `dist/`).
   (Run `npm ci` once in `src` first if the workspace isn't restored.)
2. Grep the built `dist/<folder>/<file>.js`: `react`, `react-dom/client`, `react/jsx-runtime`,
   `@umbraco-cms/*` must remain **bare external imports** (NOT inlined). The bundle should NOT contain
   React internals (no `scheduler`, no `react-dom` internals).
3. Confirm the `@customElement('n3o-...')` tag + `export default` are present and unchanged.
4. Do NOT run `dotnet build` (the orchestrator runs the single consolidated solution build).

## Report back
Plugin/project; files changed (shell `.ts`, app `.tsx`, package.json/tsconfig/vite.config.ts/d.ts); UUI components
used vs custom React surfaces; anything flagged/uncertain; `npm run build` result + the external-imports grep.

---

## Scope notes (per Talha)
- **Convert all migrated plugins** EXCEPT **Cropper** and **Uploader** (skipped this round — pending the
  jQuery/native-picker decision).
- ⚠️ **Overhead note (document, don't skip):** React adds no value to **Scheduler** (a Hangfire `<iframe>`
  wrapper), **WelcomeDashboard** (a near-static help panel), and **Blazor.BackOffice** (a non-UI JS loader).
  These are converted for uniformity only; a React root around an iframe / static panel / loader is pure
  overhead and could stay vanilla. Keep their React trees trivial.
- `telethon-on-air-rule` remains blocked (BLOCKER-04) — not in scope.
