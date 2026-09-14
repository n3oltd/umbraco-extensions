# N3O.Umbraco Frontend — A Backend Developer's Guide

> **Who this is for:** a .NET/C# backend developer who is new to JavaScript, TypeScript, React, Vite, and the Umbraco 17 ("Bellissima") backoffice. It explains **every concept** used by the frontend code in this repo, then walks **every app, file by file**.
>
> **How to read it:** start with `concepts/` top to bottom (they build on each other), then read any `apps/` doc — the app docs assume you've read the concepts and link back to them instead of repeating.

---

## The 30-second mental model

- Every backend plugin in this repo (e.g. `N3O.Umbraco.Blocks`) that needs custom backoffice UI has a sibling `*.StaticAssets` project. Inside it is an **`Apps/`** folder — a tiny **frontend project**.
- That frontend is written in **TypeScript** (a typed superset of JavaScript), built by **Vite** (a bundler) into plain JavaScript that lands in `wwwroot/App_Plugins/<plugin>/`.
- The Umbraco 17 backoffice (itself a web app made of **web components**) loads those JavaScript files at runtime, told what to load by a **manifest** (`umbraco-package.json`).
- All the frontends are one **npm workspace** rooted at `src/`. They share **one React copy** and shared build/auth helpers via a small set of foundation packages (`ReactRuntime`, `BuildConfig`/`@n3o/build`, `BackofficeCore`).
- The recurring architecture (the **"bridge pattern"**): a small **web-component "shell"** talks to Umbraco, then **mounts a React app** inside itself. Concepts doc `10` explains this — it's the single most important idea here.

---

## Concepts (read first, in order)

| # | Doc | What it covers |
|---|-----|----------------|
| 01 | [the-big-picture](concepts/01-the-big-picture.md) | How the C# backend and the frontend fit together; the `Apps/` → `wwwroot/App_Plugins` flow; the repo layout. |
| 02 | [javascript-typescript-for-csharp-devs](concepts/02-javascript-typescript-for-csharp-devs.md) | JS/TS for a C# dev: types, `async`/`await`, arrow functions, destructuring, `null`/`undefined`, the DOM. |
| 03 | [node-npm-and-the-workspace](concepts/03-node-npm-and-the-workspace.md) | Node, npm, `package.json`, `node_modules`, devDependencies, the single `src/` workspace. |
| 04 | [es-modules-and-import-maps](concepts/04-es-modules-and-import-maps.md) | `import`/`export`, ESM vs CommonJS, and the browser **import map** that resolves `@umbraco-cms/*` and `@n3o/*` at runtime. |
| 05 | [vite-and-the-build](concepts/05-vite-and-the-build.md) | What Vite does, **library mode**, **externals**, sourcemaps, the shared `@n3o/build` preset, and how `dotnet build` triggers `npm run build`. |
| 06 | [web-components-and-shadow-dom](concepts/06-web-components-and-shadow-dom.md) | Custom elements, lifecycle callbacks, Shadow DOM, slots — the technology the whole backoffice is built on. |
| 07 | [lit](concepts/07-lit.md) | Lit: `LitElement`, `html\`\``, reactive properties, and Umbraco's `UmbElementMixin`. |
| 08 | [react](concepts/08-react.md) | React: components, JSX, props, `useState`, `useEffect`, the rules of hooks, `createRoot`, refs. |
| 09 | [umbraco-backoffice-extensions](concepts/09-umbraco-backoffice-extensions.md) | The v17 extension model: `umbraco-package.json`, extension types (property editor UI, workspace view, dashboard, condition), contexts, the `uui` library. |
| 10 | [the-n3o-bridge-pattern](concepts/10-the-n3o-bridge-pattern.md) | The recurring N3O architecture: web-component shell mounts a React app; shared React runtime + import map; `auth-fetch`; `uui-react.d.ts`; `?inline` CSS. |

## Apps (file-by-file walkthroughs)

**Foundation (shared by everything — read these right after the concepts):**
- [reactruntime](apps/reactruntime.md) — the shared single copy of React, exposed via the import map.
- [buildconfig](apps/buildconfig.md) — the `@n3o/build` shared Vite + TypeScript config.
- [backofficecore](apps/backofficecore.md) — shared `auth-fetch` (authenticated API calls) + the reusable visibility condition.

**Property editors & previews:**
- [blocks](apps/blocks.md) · [cloud-platforms](apps/cloud-platforms.md) · [cells](apps/cells.md) · [editorjs](apps/editorjs.md) · [serpeditor](apps/serpeditor.md) · [textresourceeditor](apps/textresourceeditor.md)

**Workspace views & dashboards:**
- [data-export](apps/data-export.md) · [data-import](apps/data-import.md) · [data-import-data-editor](apps/data-import-data-editor.md) · [data-import-notices-viewer](apps/data-import-notices-viewer.md) · [dynamiclistviews](apps/dynamiclistviews.md) · [welcomedashboard](apps/welcomedashboard.md) · [scheduler](apps/scheduler.md)

**Other:**
- [blazor-backoffice](apps/blazor-backoffice.md) — a plain-TypeScript (no framework) loader.

## Reference
- [GLOSSARY](GLOSSARY.md) — every term/acronym in one place.

---

*Generated 2026-06-16. Covers all 16 backoffice client apps under `src/**/Apps/` + the two `src/N3O.Umbraco.Cms/Build/*` foundation packages.*
