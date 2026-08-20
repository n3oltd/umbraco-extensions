# NOT Required to Run — Optional / Removable / Dead Items

> **Status:** Findings only — nothing in this document has been changed. It exists so you can evaluate
> what can be stripped, deferred, or made optional. Generated from a read-only 12-agent codebase sweep
> (session 10). Companion documents: [`TECH_DEBT_AND_MODERNIZATION.md`](TECH_DEBT_AND_MODERNIZATION.md)
> (legacy patterns + better approaches) and [`PACKAGING_RCL_RESEARCH.md`](PACKAGING_RCL_RESEARCH.md)
> (the per-plugin `.targets` → RCL question).

This catalogs everything that is **not strictly required for the application to run**, classified so you
can decide what to remove, gate, or keep. "Required to run?" values:

- **No (safe to remove)** — dead/orphaned; deleting has zero runtime effect.
- **No (provides value — keep)** — not load-bearing at runtime, but useful (monitoring, dev host).
- **Conditional** — only needed if a specific feature/integration is used by a consuming site.
- **Build-only** — affects `dotnet build`, not the running app.

---

## Executive summary — the minimal runnable core

A minimal runnable Umbraco 17 site needs: `N3O.Umbraco.Extensions`, `N3O.Umbraco.Cms`, the domain-feature
packages a given client site actually references, and a single host project. Everything below is outside
that core. The **zero-risk removals** (nothing references them, or they already throw/do nothing at runtime):

1. `src/Sync/InspectUsync/` — scratch debug console app, not in the solution.
2. `src/Bundling/N3O.Umbraco.Bundling/` — every method throws `NotSupportedException`; **zero consumers**.
3. ~10 namespace-only / commented-out stub `.cs` files (the real registrations moved to `umbraco-package.json`).
4. `src/DemoSite/DemoSite.Web/uSync/v9/` — 175 superseded files (a `.ignore` marker says it was copied to `v17`).
5. Committed Vite build-output `.js`/`.js.map` across the `*.StaticAssets` plugins (regenerated every build).

The one item worth gating that is currently always-on in production: **`Diplo.GodMode`** (a developer
diagnostic backoffice tool).

---

## Category 1 — Whole projects not needed at runtime

| Item | Location | Required to run? | Notes / removal impact |
|---|---|---|---|
| `InspectUsync` | `src/Sync/InspectUsync/` | **No (safe to remove)** | `OutputType=Exe` scratch tool that probed uSync.Publisher reflection (BLOCKER-05 work). Not in the `.sln`. No consumers. |
| `N3O.Umbraco.Bundling` | `src/Bundling/N3O.Umbraco.Bundling/` | **No (safe to remove)** | All 4 `Bundler`/tag-helper members throw `NotSupportedException` (Smidge removed in U14). No `ProjectReference`/`PackageReference` anywhere. Remnants: 2 sln GUID entries + 2 inert `<n3o-css-bundle/>`/`<n3o-js-bundle/>` literals in `DemoSite/Views/Layout.cshtml` (render as nothing — tag-helper assembly never registered). Tracked as **RR-10**. |
| `N3O.Umbraco.WelcomeDashboard` (C# project) | `src/Plugins/WelcomeDashboard/N3O.Umbraco.WelcomeDashboard/` | **No (safe to collapse)** | `WelcomeDashboard.cs` is an empty namespace stub; `WelcomeDashboardComposer.Compose()` is empty. All registration is in `…WelcomeDashboard.StaticAssets/…/umbraco-package.json`. The C# project contributes nothing at runtime. |
| `DemoSite.Core` + `DemoSite.Web` | `src/DemoSite/` | **No (keep — dev/test host)** | The migration test host + Umbraco smoke-test site. Not packable, not published. Keep in repo; exclude from production builds (already not `IsPackable`). |
| `UIBuilder.StaticAssets`, `Forms.StaticAssets`, `Maps.Google.StaticAssets`, `Workflows.StaticAssets`, `N3O.Umbraco.Sync.Forms` | `src/UIBuilder/…`, `src/Forms/…`, `src/Maps/…`, `src/Workflows/…`, `src/Sync/N3O.Umbraco.Sync.Forms/` | **Conditional** | Empty "shim" packages that exist only to pull a third-party package (UIBuilder / Forms / GMaps / Workflow / uSync.Forms) transitively to consumers. No App_Plugins or C# of their own. Keep as deliberate distribution shims; the `.StaticAssets` naming is misleading since they ship no assets. |

---

## Category 2 — Build-time-only overhead (does not affect the running app)

| Item | Location | Required? | Notes |
|---|---|---|---|
| Committed Vite build-output JS/maps | `src/*/*.StaticAssets/App_Plugins/**/*.js` + `*.js.map` (≈23 `.js`, ≈16 `.js.map` tracked) | **Build-only (gitignore candidate)** | Overwritten by the `BuildClientApp` target on every `dotnet build`. Only `N3O.Umbraco.Cms/App_Plugins`, `Forms`, and `Sync` App_Plugins are gitignored today; the `*.StaticAssets` plugin outputs are not. Committing them creates PR noise, repo bloat, and risk of stale JS diverging from source. **Open decision (SESSION_HANDOFF):** gitignore + `git rm --cached` (requires CI to run Node) vs. keep for no-Node deploys. Vendored, non-generated files (`formstone/*.js`, `cropperjs/cropper.min.css`, `radial-progress.css`) should stay tracked either way. |
| `ExcludeLegacyUmbracoBackOffice` strip target | `src/N3O.Umbraco.Cms/N3O.Umbraco.Cms.csproj` (lines 100–109) + repo-root `Directory.Build.targets` | **Build-only (probably now redundant)** | Strips the v13 `Umbraco.Cms.Web.BackOffice.dll`. It was needed when the old `Our.Umbraco.Community.Contentment.Core` pulled it in transitively; the project now uses `Umbraco.Community.Contentment 6.1.4`, which (per `project.assets.json` inspection) does **not** carry that DLL. The targets currently no-op. Safe to keep as a guard, or delete after confirming with `dotnet nuget why`. Two copies exist (project-local + repo-global) covering the same DLL. |
| Per-plugin copy `build/*.targets` (14 files) | `src/*/build/*.targets` | **Yes (for NuGet consumers) — but legacy mechanism** | These are the asset-delivery mechanism, so they're load-bearing for consumers today — but they are the pre-.NET-6 pattern. See [`PACKAGING_RCL_RESEARCH.md`](PACKAGING_RCL_RESEARCH.md) for the RCL replacement. Listed here because the *mechanism* (not the assets) is removable overhead under the modern approach. |
| `BuildClientApp` / `BuildReactRuntime` npm targets | each `*.StaticAssets.csproj` + `N3O.Umbraco.Cms.csproj` | **Build-only (essential)** | Generate the served plugin JS. Required on a clean build. Listed for completeness — they make `dotnet build` depend on Node/npm (14 install roots). |

---

## Category 3 — Plugins/features that are runtime overhead in their current state

| Item | Location | Required? | Notes |
|---|---|---|---|
| `Diplo.GodMode` | `PackageReference` in `src/N3O.Umbraco.Cms/N3O.Umbraco.Cms.csproj` (line 55) | **No (dev/diagnostic — gate it)** | Developer inspection tool (assemblies, services, routes, models). Auto-registered by Umbraco's composer discovery whenever the DLL is present — i.e. **always on in production** unless removed. Recommend moving behind a dev-only build/feature flag. The single highest-value "make optional" item. |
| Scheduler dashboard (React iframe wrapper) | `src/Scheduler/…StaticAssets/App_Plugins/N3O.Umbraco.Scheduler/scheduler-dashboard.*` | **No (works — but React-as-overhead)** | ~58 lines of React mounting one `<iframe src="…/hangfire/">`. React + Vite for zero dynamic UI. Could be a ~10-line plain Lit element with no npm build. The Scheduler C# core is independent and unaffected. |
| WelcomeDashboard (React static panel) | `src/Plugins/WelcomeDashboard/…StaticAssets/…/welcome-dashboard.*` | **No (works — React-as-overhead)** | ~50 lines of React rendering a static "Support Centre" link. Same as above — a plain Lit component removes the React/Vite footprint. |
| `N3O.Umbraco.Blazor` + `N3O.Umbraco.Blazor.BackOffice` | `src/Blazor/` | **Conditional** | Only needed if a site uses Blazor server components in the backoffice. The loader uses global `$` (jQuery), which the v17 backoffice no longer ships — see TECH_DEBT F-03 (logic is inverted when jQuery is absent). Remove if unused; fix jQuery dependency if kept. |
| Telethon segment-rule UI (placeholder) | `src/Cloud/N3O.Umbraco.Cloud.Platforms.Marketing.StaticAssets/App_Plugins/telethon-on-air-rule/` | **No (non-functional placeholder)** | Lit stub files; the Engage v17 client-side segment-rule API is unresolved (**BLOCKER-04**). The bundle loads but registers nothing. The C# factory side is functional. Keep the placeholder until BLOCKER-04 resolves; mark clearly as non-functional. |
| Telemetry, Sentry, GeoIP, CDN, Auth0, MessageBus, Engage, Payments, Email, Newsletters, Search, Forex, KeyVault, TaxRelief, Captcha, Sync.Extensions | `src/Telemetry/`, `src/Monitoring/`, `src/GeoIP/`, `src/Cdn/`, `src/Authentication/`, `src/MessageBus/`, `src/Cloud/…Engage/`, `src/Payments/`, `src/Email/`, `src/Newsletters/`, `src/Search/`, `src/Forex/`, `src/KeyVault/`, `src/TaxRelief/`, `src/Captcha/`, `src/Sync/N3O.Umbraco.Sync.Extensions/` | **Conditional** | Per-client integrations. Zero startup cost unless a site references and configures them. **Keep.** Sentry + Telemetry + health checks are production-valuable; the rest are feature-gated by reference. |

---

## Category 4 — Stub / placeholder files doing nothing at runtime

All of these are namespace-only or fully-commented files left behind after the AngularJS→Bellissima migration
moved the real registration into `umbraco-package.json`. **Safe to delete** unless noted.

| File | Location | Notes |
|---|---|---|
| `OurBackofficeAntiforgery.cs` | `src/N3O.Umbraco.Extensions/Antiforgery/` | Angular-era CSRF workaround; v17 handles antiforgery natively (`AntiforgeryComposer` does the real work). No type, no consumers. |
| `WelcomeDashboard.cs` | `src/Plugins/WelcomeDashboard/N3O.Umbraco.WelcomeDashboard/` | Empty stub; dashboard is in StaticAssets. |
| `SchedulerDashboard.cs` | `src/Scheduler/N3O.Umbraco.Scheduler/Dashboards/` | Empty stub; dashboard is in StaticAssets. |
| `PlatformsPreviewApp.cs`, `PlatformsContentAppsComposer.cs` | `src/Cloud/N3O.Umbraco.Cloud.Platforms.StaticAssets/` | `IContentAppFactory` removed in U14; replacement is the `workspaceView` in `umbraco-package.json`. Composer body empty. |
| `ImportApp.cs`, `ExportApp.cs` | `src/Data/N3O.Umbraco.Data/ContentApps/` | Same — `IContentAppFactory` stubs; replacements are workspace views. |
| `CampaignSending.cs`, `OfferingSending.cs` | `src/Cloud/N3O.Umbraco.Cloud.Platforms/Notifications/{Campaigns,Offerings}/` | `SendingContentNotification` removed in U14. **Keep as roadmap markers** — the embed-code/URL/tab-hide behavior is a real feature regression awaiting a Bellissima workspace view (**RR-05**). |
| `Bundler.cs` (+ tag helpers) | `src/Bundling/N3O.Umbraco.Bundling/Services/` | Throws at runtime. Delete with the whole project (Category 1). |
| `PageVisibilityFilter.cs`, `HomePageStructuredData.cs` | `src/DemoSite/DemoSite.Core/…` | Entire class bodies are `/*TODO*/` comments. Demo-only dead code. |
| `PropertyBuilder.Nested.cs`, `ContentHelperExtensions.Nested.cs`, `PropertyType.Nested.cs` | `src/N3O.Umbraco.Extensions/…` | **Keep** — these are intentional `[Obsolete(error:true)]` guard-rails that give consuming sites a *compile-time* error if they still call removed Nested Content APIs. Remove only after all consumers have migrated off NC. |

---

## Category 5 — Committed artifacts not needed in source

| Item | Location | Required? | Notes |
|---|---|---|---|
| `uSync/v9/` (175 files) | `src/DemoSite/DemoSite.Web/uSync/v9/` | **No (safe to remove)** | Superseded by `uSync/v17/` (174 files). A `v9/.ignore` marker states it was copied to v17. uSync skips it. The `appsettings-schema.usync.json` default still reads `uSync/v9` (schema hint only, not runtime) — worth an explicit `RootFolder` override to `uSync/v17` for clarity. |
| `<None Remove="Smidge\**" />` | `src/DemoSite/DemoSite.Web/DemoSite.Web.csproj` (line ~10) | **No** | Excludes a `Smidge/` folder that doesn't exist (v13 bundler leftover). Delete the line. |
| Vendored Formstone JS + `cropperjs/cropper.min.css` | `src/Plugins/{Cropper,Uploader}/…/App_Plugins/…/formstone/*`, `…/cropperjs/cropper.min.css` | **Conditional (pending decision)** | Tied to the open Cropper/Uploader "native picker vs keep jQuery" decision. If switching to native pickers, delete all vendored assets; if keeping, they stay. |
| `N3ONestedContentMigrationPlan.cs` | `src/N3O.Umbraco.Extensions/Migrations/` | **Required — but reportedly uncommitted** | NOT removable — it drives the NC→BlockList migration. REVIEW_FINDINGS flagged it as present-on-disk-but-untracked. **Commit it.** (Auto-discovered as a `PackageMigrationPlan` — no composer needed.) |
| `msbuild.binlog`, `node_modules/`, `Cms/App_Plugins/` | various | **Already gitignored** | Confirmed not tracked. No action. |

---

## Cross-references

- The per-plugin `.targets` mechanism (Category 2) is analyzed in full in **`PACKAGING_RCL_RESEARCH.md`**.
- Runtime bugs, security gaps, deprecated-API and async findings are in **`TECH_DEBT_AND_MODERNIZATION.md`**.
- Pre-existing migration trackers: **`MIGRATION_PLAN.md`**, **`MIGRATION_BLOCKERS.md`**, **`REVIEW_FINDINGS.md`**, **`SESSION_HANDOFF.md`**.
