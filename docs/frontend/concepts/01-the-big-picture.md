# 01 — The Big Picture: C# Backend + JS Frontend

This document explains how the .NET backend and the JavaScript frontend fit together in this repo. Read this first; the later concept docs drill into each piece.

---

## Why is there JavaScript at all?

The old Umbraco backoffice (v13 and earlier) was server-rendered: the C# backend produced HTML pages. In Umbraco 17 ("Bellissima") the backoffice is a **client-side web application** — it is a single-page app made entirely of browser-native [web components](./06-web-components-and-shadow-dom.md) that runs in the browser. The server delivers one HTML shell page; everything else is loaded as JavaScript modules and renders itself entirely in the browser.

This means custom UI (property editors, dashboards, workspace views) must also be written in JavaScript (or TypeScript). There is no way to inject server-rendered HTML into this backoffice. The C# side still handles all business logic and data — but the UI layer is pure client-side JS.

---

## Repo layout

The repository is a standard .NET solution (`src/N3O.Umbraco.sln`). Almost every plugin consists of **two C# projects** side by side:

```
src/
  Plugins/
    WelcomeDashboard/
      N3O.Umbraco.WelcomeDashboard/          ← the C# library (business logic, API controllers, …)
      N3O.Umbraco.WelcomeDashboard.StaticAssets/  ← the frontend host (UI assets)
```

The `*.StaticAssets` project uses `Microsoft.NET.Sdk.Razor` (the same SDK used by Razor Class Libraries). The Razor SDK knows how to serve **static web assets** — pre-built files under `wwwroot/` — from a NuGet package, which is how the compiled JavaScript reaches the application at runtime. The `*.StaticAssets` project itself contains no C# business logic; it is purely a packaging shell for the frontend.

Inside the `*.StaticAssets` project:

```
N3O.Umbraco.WelcomeDashboard.StaticAssets/
  Apps/                          ← frontend source (TypeScript, edited by developers)
    package.json
    tsconfig.json
    vite.config.ts
    src/
      welcome-dashboard.ts       ← entry point
      welcome-dashboard-app.tsx  ← React component
      welcome-dashboard-app.css
  wwwroot/                       ← committed output (checked into git)
    App_Plugins/
      N3O.Umbraco.WelcomeDashboard/
        umbraco-package.json     ← tells Umbraco what to load
        welcome-dashboard.js     ← compiled output (single file)
        welcome-dashboard.js.map ← source map (for debugging)
  N3O.Umbraco.WelcomeDashboard.StaticAssets.csproj
```

The `Apps/` folder is the source you edit. The `wwwroot/` folder is the compiled output you commit. Both live in the same project; the build step (Vite) converts one into the other.

There are two slightly different layouts depending on the plugin:

| Layout | When used | Example |
|--------|-----------|---------|
| Single `Apps/package.json` at the root of `Apps/` | One app per plugin (most plugins) | `WelcomeDashboard.StaticAssets/Apps/` |
| Multiple sub-apps under `Apps/<AppName>/package.json` | Several independent apps in one plugin | `N3O.Umbraco.Cms/Apps/N3O.Umbraco.BackofficeCore/` |

`N3O.Umbraco.Cms` is special: it hosts the foundation apps (`BackofficeCore`, `DynamicListViews`) plus the React runtime build in `Build/`.

---

## The `src/` npm workspace

All frontend apps across the entire repo are part of **one npm workspace** rooted at `src/` (the same directory that holds the .NET solution). The file `src/package.json` (a JSON file analogous to a `.sln` — it groups projects) defines this:

```json
// src/package.json
{
  "name": "n3o-umbraco-clientapps",
  "private": true,
  "workspaces": [
    "**/Apps/**",
    "N3O.Umbraco.Cms/Build/*",
    "!**/bin/**",
    "!**/obj/**"
  ],
  "devDependencies": {
    "@umbraco-cms/backoffice": "17.3.5",
    "typescript": "~5.7.0",
    "vite": "^6.0.0",
    "react": "^19.0.0",
    "react-dom": "^19.0.0"
  }
}
```

"npm workspace" (analogous to a .NET solution file) means: one shared `node_modules/` folder at `src/node_modules/` holds packages for all apps. You run `npm ci` once from `src/` and every app has its dependencies. Each individual app's `package.json` declares only what is specific to it; shared tools (`vite`, `typescript`, `react`) are inherited from the workspace root.

---

## How `dotnet build` triggers the frontend build

There is a `Directory.Build.targets` file at the repo root. In .NET, this file is automatically imported by every `.csproj` in the tree — it is the equivalent of a global `<Import>` in MSBuild. It defines two targets:

**`RestoreClientWorkspace`** — runs `npm ci` in `src/` exactly once (keyed on `N3O.Umbraco.Extensions`, the first project in the dependency graph). This installs all npm packages. It is incremental: it only re-runs when `src/package-lock.json` changes.

**`BuildClientApp`** — for every `*.StaticAssets` project that has an `Apps/` folder, finds all `package.json` files under `Apps/` (at any depth) and runs `npm run build` in each one. This invokes Vite, which compiles the TypeScript and writes the output to `wwwroot/App_Plugins/<id>/`.

```xml
<!-- Directory.Build.targets (repo root) — simplified excerpt -->
<Target Name="BuildClientApp"
        Condition="Exists('$(MSBuildProjectDirectory)\Apps') And '$(MSBuildProjectName)' != 'N3O.Umbraco.Cms'"
        BeforeTargets="BeforeBuild">
    <ItemGroup>
        <_N3OClientAppPackage Include="$(MSBuildProjectDirectory)\Apps\**\package.json"
                              Exclude="$(MSBuildProjectDirectory)\Apps\**\node_modules\**" />
    </ItemGroup>
    <Exec Command="npm run build"
          WorkingDirectory="%(_N3OClientAppPackage.RootDir)%(_N3OClientAppPackage.Directory)" />
</Target>
```

`N3O.Umbraco.Cms` is excluded from the generic target because it also builds the React runtime (`Build/`) and has its own `BuildClientApps` target to handle that extra step.

The net result: **running `dotnet build` on the solution is all you need to do.** The JavaScript is built as part of the normal .NET build. There is no separate npm build step to remember.

---

## Walkthrough: the WelcomeDashboard plugin end to end

Here is the full lifecycle for one concrete, minimal plugin.

### Step 1 — Write TypeScript in `Apps/src/`

The entry point is `Apps/src/welcome-dashboard.ts`. It defines a custom HTML element (`<n3o-welcome-dashboard>`) using a web-component API and mounts a React app inside it:

```typescript
// src/Plugins/WelcomeDashboard/N3O.Umbraco.WelcomeDashboard.StaticAssets/Apps/src/welcome-dashboard.ts
import { customElement } from '@umbraco-cms/backoffice/external/lit';
import { createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { WelcomeDashboardApp } from './welcome-dashboard-app';

const elementName = 'n3o-welcome-dashboard';

@customElement(elementName)
export class N3oWelcomeDashboardElement extends HTMLElement {
    #root?: Root;
    #mount: HTMLDivElement;

    constructor() {
        super();
        const shadow = this.attachShadow({ mode: 'open' });
        this.#mount = document.createElement('div');
        shadow.appendChild(this.#mount);
    }

    connectedCallback(): void {
        this.#root ??= createRoot(this.#mount);
        this.#root.render(createElement(WelcomeDashboardApp));
    }

    disconnectedCallback(): void {
        this.#root?.unmount();
        this.#root = undefined;
    }
}
```

The React component itself (`welcome-dashboard-app.tsx`) is plain JSX returning static markup — no network calls, no props.

### Step 2 — Vite bundles it

`Apps/vite.config.ts` uses the shared `n3oPluginConfig` helper (from `@n3o/build`):

```typescript
// Apps/vite.config.ts
import { n3oPluginConfig } from '@n3o/build';

export default n3oPluginConfig({
    entries: { 'welcome-dashboard': 'src/welcome-dashboard.ts' },
    outDir: '../wwwroot/App_Plugins/N3O.Umbraco.WelcomeDashboard',
    react: true,
});
```

`react: true` tells the config to mark `react`, `react-dom`, and `react/jsx-runtime` as **external** — they are NOT bundled into the output. The Vite build emits a single file: `wwwroot/App_Plugins/N3O.Umbraco.WelcomeDashboard/welcome-dashboard.js`.

### Step 3 — Output lands in `wwwroot/App_Plugins/`

```
wwwroot/
  App_Plugins/
    N3O.Umbraco.WelcomeDashboard/
      umbraco-package.json     ← already there; hand-authored manifest
      welcome-dashboard.js     ← freshly emitted by Vite
      welcome-dashboard.js.map ← source map
```

The `welcome-dashboard.js` file is checked into git alongside the source. This is intentional — it makes the package self-contained and means consuming applications do not need the npm toolchain just to serve the plugin.

### Step 4 — Umbraco reads `umbraco-package.json`

At startup, Umbraco 17 scans every `App_Plugins/**/umbraco-package.json` it can find (delivered either from `wwwroot/App_Plugins/` directly or via the Razor static-web-assets pipeline from a NuGet package):

```json
// wwwroot/App_Plugins/N3O.Umbraco.WelcomeDashboard/umbraco-package.json
{
  "$schema": "http://json.schemastore.org/umbraco-package.json",
  "id": "N3O.Umbraco.WelcomeDashboard",
  "name": "N3O Welcome Dashboard",
  "version": "17.0.0",
  "extensions": [
    {
      "type": "dashboard",
      "alias": "N3O.Dashboard.Welcome",
      "name": "N3O Welcome Dashboard",
      "element": "/App_Plugins/N3O.Umbraco.WelcomeDashboard/welcome-dashboard.js",
      "weight": -10,
      "meta": { "label": "Welcome", "pathname": "welcome" },
      "conditions": [
        { "alias": "Umb.Condition.SectionAlias", "match": "Umb.Section.Content" }
      ]
    }
  ]
}
```

This tells Umbraco: "there is a dashboard extension; load the JS file at the `element` URL; show it in the Content section."

### Step 5 — The backoffice loads the JS

When a user opens the backoffice, it fetches the manifest, loads `welcome-dashboard.js` as an ES module, registers the `<n3o-welcome-dashboard>` custom element, and renders it. The browser resolves `import 'react'` to the shared React runtime via an **import map** (explained in [04 — ES Modules and Import Maps](./04-es-modules-and-import-maps.md)).

---

## Serving the assets in production

When the `*.StaticAssets` project is packed as a NuGet package, the `wwwroot/` tree becomes **static web assets** — a .NET feature where a Razor Class Library can contribute files under `wwwroot/` to the consuming application's web root. The consuming app (the customer's Umbraco site) references the NuGet package; at runtime ASP.NET Core serves the files as if they were in the site's own `wwwroot/`. No manual copying required.

---

## Concept-to-C# analogy table

| Frontend concept | C# / .NET equivalent |
|-----------------|----------------------|
| `package.json` | `.csproj` — declares the package identity and dependencies |
| `src/package.json` (workspace root) | `N3O.Umbraco.sln` — groups multiple projects |
| npm | NuGet — the package manager |
| `node_modules/` | The NuGet packages folder (`~/.nuget/packages`) |
| `package-lock.json` | `packages.lock.json` — the exact locked dependency tree |
| TypeScript (`.ts`/`.tsx`) | C# (`.cs`) — the source language you write |
| Vite | MSBuild + Roslyn — the build tool |
| `tsc` (TypeScript compiler) | The C# compiler (`csc`/`dotnet build`) — type-checks and transpiles |
| ES module (`.js` output) | A compiled assembly (`.dll`) |
| `import` statement | `using` + assembly reference |
| `umbraco-package.json` | An Umbraco plugin registration (like a `IComposer` wiring up services) |
| `wwwroot/App_Plugins/` | The output of `dotnet publish` — artefacts served to clients |
| npm workspace (`workspaces` in `package.json`) | A solution file grouping multiple projects |
| `@n3o/build` (`BuildConfig` package) | A shared `.props`/`.targets` file imported by every project |
| `@n3o/react-runtime` (ReactRuntime) | A shared utility assembly referenced by multiple projects |
| Import map (browser runtime wiring) | Assembly binding redirects / shared runtime references |
| `devDependencies` in `package.json` | `<PackageReference>` in `.csproj` |
| Vite external (`react` not bundled) | `ExternalsAlias` / GAC / shared runtime — a dependency not copied into the output |
| `.d.ts` declaration file | XML doc / reference assembly — provides types without implementation |
| `tsconfig.json` | `<LangVersion>`, `<Nullable>`, `<TreatWarningsAsErrors>` in `<PropertyGroup>` |

---

## Summary

1. Backend plugins have a `*.StaticAssets` sibling that hosts the UI.
2. The UI source lives in `Apps/`; the compiled output lives in `wwwroot/App_Plugins/<id>/`.
3. A single `npm ci` + per-app `npm run build` runs automatically as part of `dotnet build`, driven by `Directory.Build.targets`.
4. Umbraco discovers the UI via `umbraco-package.json` manifests at startup and loads the JS in the browser.
5. All apps share one npm workspace, one React copy, and one set of Vite/TS config helpers.

Continue to [02 — JavaScript and TypeScript for C# Developers](./02-javascript-typescript-for-csharp-devs.md).
