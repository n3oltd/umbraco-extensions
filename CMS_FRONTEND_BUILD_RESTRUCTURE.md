# CMS / backoffice client-app build restructure

> ⚠️ SUPERSEDED (2026-06-24). The intermediate layout described below (`@n3o/build`, `Cms/Build/`, `Cms/Extensions/`, BackofficeCore extracted under Cms) was replaced by the `src/frontend/` workspace when `origin/v17` was merged. For the CURRENT authoritative layout see AGENTS.md and the summary immediately below. The historical detail is retained for context.

## Current layout (2026-06-24)

The frontend is a single npm + Turborepo workspace rooted at `src/` (root manifest `@n3oltd/client-root`, private). It contains **9 workspace packages**.

**Three shared packages at top-level `src/frontend/`:**
- `src/frontend/build-config` → **`@repo/build-config`** — shared Vite/TS preset. Exposes `./tsconfig` (base), `./tsconfig-react` (adds `jsx: react-jsx`), and `.` → `n3oPluginConfig()`. Consumers extend the tsconfig **by name**.
- `src/frontend/backoffice-core` → **`@n3oltd/backoffice-core`** — auth-fetch (`createAuthFetch`, `UmbAuthFetchMixin`) + the `WorkspaceVisibilityCondition` (alias `N3O.Condition.WorkspaceVisibility`). Built to `dist/N3O.Umbraco.BackofficeCore/` and **shipped by the ReactRuntime project** (via `N3OFrontendExtraDistDir`) into `App_Plugins/N3O.Umbraco.BackofficeCore`.
- `src/frontend/backoffice-ui` → **`@n3oltd/backoffice-ui`** — React wrappers over UUI controls; React externalized, peerDep `react ^19`.

**Shared React runtime is its own .NET project:** `src/N3O.Umbraco.ReactRuntime/` (`Microsoft.NET.Sdk.Razor`, `net10.0`, PackageId `N3O.Umbraco.ReactRuntime`). Its frontend lives at `src/N3O.Umbraco.ReactRuntime/frontend/react-runtime/` → **`@n3oltd/react-runtime`** (real react/react-dom 19.2.7; two-pass Vite build emitting the singleton `react.js` / `react-dom.js` / `react-jsx-runtime.js`). Its `wwwroot/App_Plugins/N3O.Umbraco.ReactRuntime/umbraco-package.json` publishes the **import map**; the sibling `N3O.Umbraco.BackofficeCore/umbraco-package.json` registers the condition + maps `@n3oltd/backoffice-core`.

**Per-plugin apps** live at `<Project>/frontend/<app>/` and build via `n3oPluginConfig()` — e.g. `src/Data/N3O.Umbraco.Data/frontend/{data-export,data-import,data-import-data-editor,data-import-notices-viewer}` and `src/N3O.Umbraco.Cms/frontend/dynamic-list-views` (Lit, no React). React 19 is a self-hosted ESM singleton via the import map; every React plugin externalizes `react`/`react-dom`/`react-dom/client`/`react/jsx-runtime` (and `@n3oltd/backoffice-core` where used).

**Build seam (MSBuild ↔ npm):** root `Directory.Build.props` (sets `N3OHasFrontend` for any project with a `frontend/` folder) + `Directory.Build.targets` with targets `RestoreFrontendDependencies` (`npm ci` once in `src`), `BuildFrontendWorkspace` (a single `npx turbo run build --env-mode=loose`), and `BuildFrontend` (per-project copy of `frontend/*/dist/**` + optional `N3OFrontendExtraDistDir` into `wwwroot/App_Plugins`). These replace the old `BuildClientApp` / `BuildClientApps` / `BuildReactRuntime` targets. Version consistency is enforced by `src/.syncpackrc.json` (`syncpack lint`); root `overrides` pin react `19.2.7`, vite `^6`, typescript `~5.7`, `@umbraco-cms/backoffice` `17.3.5`.

> **Stale coexistence (cleanup pending):** the old `src/N3O.Umbraco.Cms/Build/*` and `src/N3O.Umbraco.Cms/Extensions/*` dirs (and `src/Data/N3O.Umbraco.Data.StaticAssets/Extensions/*`) are **still git-tracked and pending deletion**. They are not part of the Turbo workspace and no project builds them.

---

> **Status (historical, 2026-06-15):** implemented + **build-verified (full `dotnet build` → 0 errors)**, **UNCOMMITTED** on `v17-Talha`
> (parent commit `5d98eacbd`). 97 changed paths. `npm install` clean; all three `@n3o/*` workspace packages resolve.
> Nothing pushed. This doc is the handoff so the work can be reviewed / committed / PR'd from here.

## Why this was done (4 maintainability problems + 1 conflation)

1. Every app `tsconfig.json` did `extends "../../../../tsconfig.base.json"` — a **depth-varying** relative path (`../../../` for 3-deep apps, `../../../../` for 4-deep).
2. Every app `vite.config.ts` did `import { n3oPluginConfig } from '../../../../build/vite-config'` — same fragile relative path.
3. Every app `package.json` re-declared the same build/type deps (react, react-dom, @types/*, @umbraco-cms/backoffice, typescript, vite) → version drift over time.
4. `src/vite-env.d.ts` (the `*.css?inline` ambient type) was copy-pasted into 11 apps.
5. `N3O.Umbraco.ReactRuntime` conflated **three** concerns: the React ESM shim, the `@n3o/backoffice-core` auth-fetch runtime, and the `N3O.Condition.WorkspaceVisibility` condition.

The fix was deliberated via a multi-agent workflow (verified empirically against TS 5.7.3 / Vite 6.4.3 / Node 22) and then implemented in three phases (A: `@n3o/build`; B: BackofficeCore split; C: folder consolidation).

## What changed

### A. New `@n3o/build` workspace package (config, referenced BY NAME)
A single config-only workspace package now hosts the shared TS base, the Vite preset, and the shared ambient type. Apps reference it **by name** (`@n3o/build`), so the depth-varying relative paths are gone.

- `@n3o/build/base.json` — the old `tsconfig.base.json` content **+ `"files": ["vite-env.d.ts"]"`** (the shared ambient type rides into every app through `extends`).
- `@n3o/build/vite-config.js` — the preset as **plain ESM** (Vite's config bundler externalizes the resolved bare import, so Node loads the `.js`; it can't load `.ts` on that path). `vite-config.d.ts` is hand-authored IDE-only types — **keep the two in sync**.
- `@n3o/build/vite-env.d.ts` — the single `*.css?inline` ambient declaration.
- `package.json` exports map: `"./tsconfig" → base.json`, `"./vite-env" → vite-env.d.ts`, `"." → { types: vite-config.d.ts, import: vite-config.js }`. The `"."` entry **needs the `import` condition** (Vite's config bundler ignores a top-level `main`).

Per app (all 16): `tsconfig.json` → `extends "@n3o/build/tsconfig"`; `vite.config.ts` → `import { n3oPluginConfig } from '@n3o/build'`. **Edits were surgical** (only the extends/import value swapped — e.g. Cells' `compilerOptions.paths.handsontable` override is preserved).

### B. Shared deps root-hoisted
The 7 shared deps moved to the **root `src/package.json`** `devDependencies` + a matching `overrides` block (single version truth: `@umbraco-cms/backoffice 17.3.5`, `typescript ~5.7.0`, `vite ^6.0.0`, `react`/`react-dom`/`@types/react`/`@types/react-dom` `^19.0.0`). Each app `package.json` was stripped of those (kept only app-unique deps + `@n3o/backoffice-core` + `@n3o/build`). The 11 duplicated `vite-env.d.ts` and the old `tsconfig.base.json` + `build/vite-config.ts` were deleted.

### C. `N3O.Umbraco.BackofficeCore` extracted from ReactRuntime
auth-fetch + the workspace-visibility condition were moved (git-tracked) into a new app that **keeps the `@n3o/backoffice-core` package name** (zero consumer churn). ReactRuntime was slimmed to just the React shim and **renamed `@n3o/react-runtime`**.
- Consumers unchanged: apps still `import … from '@n3o/backoffice-core'`; the condition is still referenced by alias `N3O.Condition.WorkspaceVisibility`.
- The `@n3o/backoffice-core` import-map entry + the condition registration moved to BackofficeCore's own `umbraco-package.json`; ReactRuntime's manifest now carries only the `react*` import-map.

### D. Cms folder consolidation (this is the final layout) (SUPERSEDED — see Current layout above)
```
N3O.Umbraco.Cms/
  Extensions/
    N3O.Umbraco.BackofficeCore/      auth-fetch (@n3o/backoffice-core) + workspace-visibility-condition
    N3O.Umbraco.DynamicListViews/
  Build/
    N3O.Umbraco.BuildConfig/         @n3o/build (shared tsconfig / vite preset / ambient types)
    N3O.Umbraco.ReactRuntime/        @n3o/react-runtime (React ESM shim)
```
- `@n3o/build` moved out of the IDE-invisible `src/build/` into `Cms/Build/N3O.Umbraco.BuildConfig/` (under the Cms project → shows in the IDE). The package name stays `@n3o/build`; the folder is named for the `N3O.Umbraco.*` sibling convention.
- `ReactRuntime` moved from the Cms root into `Cms/Build/`.
- ReactRuntime vite `outDir` adjusted `../wwwroot` → `../../wwwroot` (one level deeper). **Built output path is unchanged** — still ships from `Cms/wwwroot/App_Plugins/N3O.Umbraco.ReactRuntime/`.

## Key invariants / mechanics (don't break these)
- **By-name resolution is location-independent.** `@n3o/build`, `@n3o/build/tsconfig`, `@n3o/backoffice-core` all resolve through the npm-workspace symlink — moving the package folders needs **no** change to any app's tsconfig/vite/imports.
- **`Cms/Build/**` is excluded from Content** via `<Content Remove="Build\**" />` in `N3O.Umbraco.Cms.csproj` — the Razor SDK auto-includes `**/*.json` as Content, so without this `@n3o/build/base.json` would leak into the NuGet package. The built artifacts ship separately from `wwwroot` as static web assets.
- **Build target:** `N3O.Umbraco.Cms.csproj` builds `Extensions\*\package.json` (BackofficeCore + DynamicListViews) + `Build\N3O.Umbraco.ReactRuntime\package.json`. `@n3o/build` (`Build\N3O.Umbraco.BuildConfig`) has **no build script** and is intentionally **not** in the build target (it ships raw `.json`/`.js`/`.d.ts`, consumed via the symlink).
- **Workspaces** (`src/package.json`): `["**/Extensions/**", "N3O.Umbraco.Cms/Build/*", "!**/bin/**", "!**/obj/**"]`. The `Build/*` glob covers both `N3O.Umbraco.BuildConfig` and `N3O.Umbraco.ReactRuntime`.
- **Hoisting** lets apps omit react/vite/typescript; reliable for npm workspaces (would only break under a pnpm strict-node_modules migration).
- ReactRuntime still declares `react`/`react-dom` (it's the one package that *bundles* React) — same `^19`, pinned by root `overrides`.

## Files changed (97 total; categories)
- **New:** `N3O.Umbraco.Cms/Build/N3O.Umbraco.BuildConfig/` (@n3o/build: package.json, base.json, vite-config.js, vite-config.d.ts, vite-env.d.ts); `N3O.Umbraco.Cms/Extensions/N3O.Umbraco.BackofficeCore/` (package.json, tsconfig.json, vite.config.ts, src/*); `N3O.Umbraco.Cms/wwwroot/App_Plugins/N3O.Umbraco.BackofficeCore/umbraco-package.json` (+ built js).
- **Moved (git-tracked renames):** ReactRuntime → `Cms/Build/N3O.Umbraco.ReactRuntime/`; auth-fetch.ts + workspace-visibility-condition.ts → BackofficeCore.
- **Edited:** 16× `tsconfig.json` + 16× `vite.config.ts` + 16× `package.json` (apps); `src/package.json` (workspaces + root devDeps + overrides); `N3O.Umbraco.Cms.csproj`; ReactRuntime `package.json` (rename + drop `types`) + `vite.config.*.ts` (entries + outDir); both `umbraco-package.json` manifests (ReactRuntime slimmed, BackofficeCore added); `package-lock.json` regenerated.
- **Deleted:** 11× per-app `src/vite-env.d.ts`; `src/tsconfig.base.json`; `src/build/vite-config.ts`.

## Verification done
- `npm install` (clean) — exactly one each of `@n3o/build`, `@n3o/backoffice-core`, `@n3o/react-runtime`.
- Full `dotnet build N3O.Umbraco.sln` → **0 errors** after every phase (each app's `tsc --noEmit` + vite build resolved the by-name config, the inherited ambient type, the hoisted deps; the moved import-map + condition registered).
- ReactRuntime output confirmed still at `Cms/wwwroot/App_Plugins/N3O.Umbraco.ReactRuntime/`.

## To continue from here
1. **Commit / push.** All of the above is uncommitted on `v17-Talha`. It is **largely solution-wide** (`src/package.json`, the Cms project, and every app's config across many projects), so it most naturally belongs in the **final solution-wide PR**, not the per-project slices. The `@n3o/build` migration + the BackofficeCore/ReactRuntime restructure can't be cleanly sliced per-project (it depends on all apps being present).
2. **`@n3o/build` preset is plain `.js` + hand-authored `.d.ts`** — if you change the preset, update both. (Alternative considered + rejected: a TS source with an install-time `tsc` prepare step.)
3. **BOM:** some edited config files may have had their UTF-8 BOM normalized by the bulk tooling — cosmetic, compiles fine. Normalize if you want clean diffs.
4. **Naming (resolved):** the `@n3o/build` package folder was renamed `Cms/Build/build/` → `Cms/Build/N3O.Umbraco.BuildConfig/` to match the `N3O.Umbraco.*` sibling convention. The package name stays `@n3o/build`. The rename touched only the folder on disk + the `node_modules` symlink (refreshed via `npm install`) — no glob edit (the `Build/*` wildcard still matches) and no by-name reference changed; one app rebuilt clean to confirm.
5. **Live backoffice smoke-test** is still owed (build verifies compilation, not runtime): confirm the backoffice loads, the React import-map resolves `react`/`@n3o/backoffice-core` from the new paths, and the `N3O.Condition.WorkspaceVisibility` condition still gates the Data Export/Import tabs.

*Generated 2026-06-15. Subject: the frontend build-config refactor (`@n3o/build`) + BackofficeCore extraction + Cms `Build/`+`Extensions/` consolidation. Commits handled by Talha.*
