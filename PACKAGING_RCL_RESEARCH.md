# Packaging Research — Per-plugin `build/*.targets` vs. the RCL Standard

> **Question investigated:** *"Do we need a `.targets` file in a `build/` folder for each plugin? How does
> Umbraco do it? Is it the standard way?"*
>
> **Method:** 6 Sonnet web/Context7 research agents → Opus synthesis (earlier session), plus a full
> repo enumeration of every `.targets` file (session-10 build-packaging agent). **Documentation only —
> nothing changed.** Companions: [`NOT_REQUIRED_TO_RUN.md`](NOT_REQUIRED_TO_RUN.md) ·
> [`TECH_DEBT_AND_MODERNIZATION.md`](TECH_DEBT_AND_MODERNIZATION.md).

---

## ✅ Implemented (2026-06-24)

The RCL rollout described below as "deferred/pending/next" is **DONE**. Current reality:

- Backoffice plugins ship as **Razor Class Libraries** (`Microsoft.NET.Sdk.Razor`) serving their
  `wwwroot/App_Plugins/**` through the ASP.NET Core **static web assets** pipeline, with
  `StaticWebAssetBasePath=/`. There are **no hand-written `build/<id>.targets`** asset-copy files and
  no consumer-side `<Import>`s.
- The frontend is built by the **shared root `Directory.Build.targets`** (a single
  `npx turbo run build --env-mode=loose` over the `src/` Turborepo workspace) followed by the
  per-project `BuildFrontend` copy of `frontend/*/dist/**` into `wwwroot/App_Plugins/...`. The old
  `BuildClientApp` / `BuildClientApps` / `BuildReactRuntime` targets are gone.
- The shared React runtime is now its **own .NET project** `N3O.Umbraco.ReactRuntime`
  (`src/N3O.Umbraco.ReactRuntime/`), with its frontend at `frontend/react-runtime/` and the import map
  at `src/N3O.Umbraco.ReactRuntime/wwwroot/App_Plugins/N3O.Umbraco.ReactRuntime/umbraco-package.json`.
- `N3O.Umbraco.Data.StaticAssets` and `N3O.Umbraco.Bundling` were **deleted**; `N3O.Umbraco.Data`
  itself is now the RCL (it hosts its own `frontend/` apps). The remaining `*.StaticAssets` projects
  are RCLs too.

The research/rationale below is retained for context; names, paths and status are updated to match.

---

## STATUS — PILOTED (s11), FOLDED INTO `N3O.Umbraco.Cms` (s12), CLEANED UP (s13)

**UPDATE (session 13, 2026-06-10):** `N3O.Umbraco.Cms` packaging finalised. **Deleted** the legacy `build/N3O.Umbraco.Cms.targets` copy mechanism (and `DemoSite.Web`'s `<Import>` of it), the dead v13 `App_Static/ace-builds` files (Ace is gone from the v17 backoffice), and the stale v13 `App_Plugins/Contentment` source (Contentment 6.1.4 self-ships its v17 assets transitively). Dropped `<Content App_Static>` / `<None build>` from the csproj. **NETSDK1152 root cause found + fixed:** the conflict was NOT (only) the copy-targets — the Umbraco SDK regenerates `appsettings-schema.json` / `appsettings-schema.Umbraco.Cms.json` / `umbraco-package-schema.json` into the RCL root, which then dual-publish into consumers at the same relative path; excluded them via `<Content Remove>`. `dotnet publish` of the test site now succeeds with **0 errors**. Also renamed the shared React runtime `N3O.Umbraco.React` → **`N3O.Umbraco.ReactRuntime`** and moved it to its own project at `src/N3O.Umbraco.ReactRuntime/` (frontend at `frontend/react-runtime/`; import map at `wwwroot/App_Plugins/N3O.Umbraco.ReactRuntime/umbraco-package.json`). The rollout checklist (other `*.StaticAssets` plugins) is unchanged.

**UPDATE (session 12):** the separate `N3O.Umbraco.Extensions.StaticAssets` project was **removed** and its contents folded **into `N3O.Umbraco.Cms`** (which is itself `Microsoft.NET.Sdk.Razor`). Cms now carries its per-plugin `frontend/` apps and `wwwroot/App_Plugins/`, plus `<StaticWebAssetBasePath>/</StaticWebAssetBasePath>` and the `node_modules` exclude; the frontend is built by the shared root `Directory.Build.targets` (turbo) + `BuildFrontend` copy, not a per-project `BuildClientApps` target. Verified: `dotnet build` 0 errors and the test site serves `/App_Plugins/**` (200) from Cms with the DLV condition + workspace view registered. **Consequence: building `N3O.Umbraco.Cms` now requires Node/npm.** The RCL mechanics below are unchanged — just hosted by Cms rather than a dedicated project. The rollout checklist (other `*.StaticAssets` plugins) still stands; use the Cms setup as the reference instead of the now-deleted Extensions.StaticAssets.

The RCL approach below is **no longer just research — it has been piloted and verified** (originally on the now-removed `N3O.Umbraco.Extensions.StaticAssets`, then `N3O.Umbraco.Cms`) (`Microsoft.NET.Sdk.Razor`, `StaticWebAssetBasePath=/`). The shared React runtime is now its own RCL project, `N3O.Umbraco.ReactRuntime` (frontend `frontend/react-runtime/` → `wwwroot/App_Plugins/N3O.Umbraco.ReactRuntime`); the DynamicListViews frontend lives at `src/N3O.Umbraco.Cms/frontend/dynamic-list-views` → `wwwroot/App_Plugins/N3O.Umbraco.DynamicListViews`. The frontend is built by the shared root `Directory.Build.targets` (`npx turbo run build --env-mode=loose`) + per-project `BuildFrontend` copy, **no `build/*.targets`, no consumer `<Import>`** — `DemoSite.Web` just `<ProjectReference>`s it and the assets flow via static web assets. `dotnet build` of the RCL and of `DemoSite.Web` are **0 errors**, and the static-web-assets manifest serves both plugins at the expected `/App_Plugins/<Name>/...` URLs. Findings confirmed against the installed 17.3.5 binaries + the official `dotnet new umbraco-extension` template.

**One thing the pilot proved empirically:** the OLD copy-`.targets` plugins **dual-deliver** `App_Plugins` (as published `<Content>` *and* a copy target), which breaks `dotnet publish` with `NETSDK1152` ("multiple publish output files with the same relative path"). The RCL plugin is conflict-free. So the rollout below is not just modernization — it **unblocks `dotnet publish`**.

### Rollout checklist (DONE 2026-06-24) — every remaining `*.StaticAssets` converted to RCL
This is now complete. Each project: switched SDK to `Microsoft.NET.Sdk.Razor`, added `StaticWebAssetBasePath=/`, moved `App_Plugins/<Name>` → `wwwroot/App_Plugins/<Name>`, deleted `build/*.targets` + the `<None Include="build/**">` pack, and removed the project's `<Import>` line from `DemoSite.Web.csproj` (keeping the `<ProjectReference>`). The Vite/`outDir` step is superseded — the frontend is now built by the shared root `Directory.Build.targets` (turbo) + per-project `BuildFrontend` copy into `wwwroot/App_Plugins/...`. (`N3O.Umbraco.Data.StaticAssets` and `N3O.Umbraco.Bundling` were deleted instead of converted; `N3O.Umbraco.Data` itself is now the RCL.)

- [ ] `Plugins/SerpEditor` (simplest standalone — do first; validate dev + `dotnet publish`)
- [ ] `Plugins/Cells`, `Plugins/Cropper`, `Plugins/EditorJs`, `Plugins/TextResourceEditor`, `Plugins/Uploader`, `Plugins/WelcomeDashboard`
- [ ] `Data/N3O.Umbraco.Data.StaticAssets` (multi-app), `Scheduler/N3O.Umbraco.Scheduler.StaticAssets`
- [ ] `Cloud/N3O.Umbraco.Cloud.Platforms.StaticAssets` (+ `.Marketing.StaticAssets`)
- [ ] `Blocks/N3O.Umbraco.Blocks.StaticAssets`, `Blazor/N3O.Umbraco.Blazor.BackOffice`
- [ ] `N3O.Umbraco.Cms` — Contentment App_Plugins delivery (verify it still ships once the others move; Cms keeps `App_Static`)
- [ ] After all converted: confirm a full `dotnet publish` of `DemoSite.Web` succeeds (no `NETSDK1152`), and drop the now-empty `build/` import block from `DemoSite.Web.csproj`.

---

## Conclusion (short)

**No — a per-plugin copy-`.targets` file is not required and is not the modern standard.** It is the
pre-.NET-6 / Umbraco v8–v13 asset-delivery pattern. The de-facto and officially-documented standard for
Umbraco 14–17 packages is a **Razor Class Library** (`Microsoft.NET.Sdk.Razor`) that serves its
`App_Plugins` assets through the ASP.NET Core **static web assets** pipeline, with:

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <StaticWebAssetBasePath>App_Plugins/N3O.Umbraco.SerpEditor</StaticWebAssetBasePath>
  </PropertyGroup>
</Project>
```

Assets live under `wwwroot/`; the SDK packs and delivers them to consumers automatically. There is **no
hand-written `<Copy>` target, no `buildTransitive/*.targets`, and no consumer-side `<Import>`.** Umbraco's
own first-party packages (e.g. `Umbraco.Forms.StaticAssets`) use this RCL model.

---

## Why the current `.targets` pattern is legacy

The repo ships **14** per-plugin `build/<id>.targets` files, each containing a `<Copy>` task that runs on the
**consuming** project's build to copy `App_Plugins/**` out of the NuGet package cache into the consumer's
`App_Plugins` folder:

| # | Project (under `src/`) |
|---|---|
| 1 | `Blazor/N3O.Umbraco.Blazor.BackOffice` |
| 2 | `Blocks/N3O.Umbraco.Blocks.StaticAssets` |
| 3 | `Cloud/N3O.Umbraco.Cloud.Platforms.Marketing.StaticAssets` |
| 4 | `Cloud/N3O.Umbraco.Cloud.Platforms.StaticAssets` |
| 5 | `Data/N3O.Umbraco.Data.StaticAssets` |
| 6 | `N3O.Umbraco.Cms` |
| 7 | `Plugins/Cells/N3O.Umbraco.Cells.StaticAssets` |
| 8 | `Plugins/Cropper/N3O.Umbraco.Cropper.StaticAssets` |
| 9 | `Plugins/EditorJs/N3O.Umbraco.EditorJs.StaticAssets` |
| 10 | `Plugins/SerpEditor/N3O.Umbraco.SerpEditor.StaticAssets` |
| 11 | `Plugins/TextResourceEditor/N3O.Umbraco.TextResourceEditor.StaticAssets` |
| 12 | `Plugins/Uploader/N3O.Umbraco.Uploader.StaticAssets` |
| 13 | `Plugins/WelcomeDashboard/N3O.Umbraco.WelcomeDashboard.StaticAssets` |
| 14 | `Scheduler/N3O.Umbraco.Scheduler.StaticAssets` |

Problems with this mechanism:

- **It pre-dates the SDK feature that replaces it.** Static Web Assets (.NET 6+) does delivery + publish +
  content-hashing + cleanup natively; the manual `<Copy>` does none of that and isn't incrementally cached.
- **Dual delivery / redundancy.** Each `.StaticAssets` csproj *also* packs `App_Plugins/**` as `<Content>`
  (`ContentTargetFolders=.`). So assets are shipped twice — once as NuGet content, once copied by the
  target. The content path is effectively dead weight under the targets approach.
- **It doesn't work transitively for ProjectReferences.** `DemoSite.Web.csproj` has to manually `<Import>`
  each of 8 `.targets` files (a `ProjectReference` doesn't trigger `buildTransitive`). Every new plugin needs
  two edits (reference + import). Under RCL this is automatic.
- **No isolation / no cleanup.** Stale files left in a consumer's `App_Plugins` after a package downgrade
  aren't removed; the static-web-assets manifest handles this.

---

## Migration plan (if/when you choose to do it)

Per `.StaticAssets` project:

1. Change the SDK: `<Project Sdk="Microsoft.NET.Sdk">` → `<Project Sdk="Microsoft.NET.Sdk.Razor">`.
2. Move `App_Plugins/<name>/**` → `wwwroot/App_Plugins/<name>/**` (or set `StaticWebAssetBasePath` to
   `App_Plugins/<name>` and keep a `wwwroot` root).
3. Delete the `build/<id>.targets` file and its `<None Include="build\**" PackagePath="buildTransitive">` item.
4. Remove the redundant `<Content Include="App_Plugins\**" Pack="true" ...>` items (the RCL packs `wwwroot`).
5. Frontend build is no longer a per-project Vite target — the shared root `Directory.Build.targets` runs `npx turbo run build --env-mode=loose` over the `src/` workspace and the per-project `BuildFrontend` copies `frontend/*/dist/**` into `wwwroot/App_Plugins/<name>/`.
6. In `DemoSite.Web.csproj`, delete the 8 manual `<Import>` lines — RCL assets flow via `ProjectReference`.

**Verification:** `dotnet build DemoSite.Web` and confirm `bin/.../wwwroot/App_Plugins/<name>/*.js` is present
and served at `/App_Plugins/<name>/...`; `dotnet pack` and inspect the `.nupkg` (`staticwebassets` manifest +
`wwwroot` content, no `buildTransitive`).

---

## Gotchas

- **`StaticWebAssetBasePath` casing must exactly match** the `App_Plugins/<name>` Umbraco expects
  (Umbraco scans App_Plugins 2 levels deep). Get it wrong and the plugin silently doesn't load.
- **Production runtime mode disables `UseStaticWebAssets()`.** Umbraco's package discovery uses a composite
  file provider (WebRoot + ContentRoot); confirm assets resolve in `Production` runtime mode, not just dev —
  this is the single most important thing to test, because it's the historical reason teams kept the copy
  pattern. (If RCL assets don't surface in Production for a given host config, that's the deciding factor.)
- **`umbraco-package.json` / import-map** (the React runtime in `N3O.Umbraco.ReactRuntime`, at
  `wwwroot/App_Plugins/N3O.Umbraco.ReactRuntime/umbraco-package.json`) must live under the same
  `wwwroot/App_Plugins/<name>/` so its served path is unchanged.
- Mixed approach is fine during transition — RCL and copy-targets projects can coexist while you migrate
  one plugin at a time (start with `SerpEditor.StaticAssets`, the simplest, as a proof).

---

## Recommendation

Treat this as a **deferred, opt-in modernization**, not a migration blocker — the current `.targets` mechanism
**works** and is load-bearing for NuGet consumers today. When you do it:

1. **Prove it on one plugin** (`SerpEditor.StaticAssets`) end-to-end including a **Production-runtime-mode**
   check before converting the rest.
2. Pair it with the `Directory.Build.props` work (TECH_DEBT D-02) and the shared frontend-build target
   extraction (D-04) — they touch the same files. (Both are now done: a root `Directory.Build.props` +
   `Directory.Build.targets` drive the turbo build.)
3. Net result across all 14 projects: delete 14 `.targets` files + 8 `DemoSite` `<Import>` lines + the
   redundant `<Content>` packing, and gain incremental builds, content-hashing, and automatic
   ProjectReference flow.
