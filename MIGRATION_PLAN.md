# N3O.Umbraco: Umbraco 13 → 17 Migration Plan

> ✅ **CURRENT STATE (2026-06-29) — authoritative; reconciles this plan with the full status audit.** Per-project migration/PR status now lives in **GitHub issue [n3oltd/work#729](https://github.com/n3oltd/work/issues/729)** (single living checklist) + the 2026-06-29 banner in `MIGRATION_PR_TRACKER.md`. Almost every project is migrated (merged to `v17`, open PR, or migrated-on-`v17-Talha`-awaiting-PR).
>
> **⚠️ Build is NOT 0 errors right now** (updated 2026-06-30; the "0 errors" / "App starts HTTP 200" claims below are stale): full `dotnet build N3O.Umbraco.sln` on `v17-Talha` now fails from **2 root causes** — (1) `npm ci` env failure in `Directory.Build.targets`; (3) `DemoSite.Web` stale `Import` of `N3O.Umbraco.Cms\build\N3O.Umbraco.Cms.targets` (Cms is now an RCL). **Root cause (2) is resolved:** `N3O.Umbraco.Authentication.Auth0` was migrated to the **Auth0.ManagementApi 8.5.0** API and builds 0 errors (PR **#883** — see [[auth0-managementapi-v8-migration]] / #729). Item (3) is a quick known fix; (1) is environmental. *(A full `N3O.Umbraco.Data` restore also still fails until **#856** lands — Scheduler is `net8.0` on `v17` and Data references it.)*
>
> **Definition-of-Done reconciliation** (the checklist at the bottom is internally out of date — these match the "Remaining Runtime Issues" section above, which is correct): ✅ TelethonOnAir **C#** factory (RR-02), `GetNestedPropertySchemaHandler` removed (RR-03), `GetPreviewUrlAsync` (RR-07), `SaveAndPublish` (RR-09) are **done**; `<Version>` tags are **placeholder-done** (`17.0.0`; CalVer re-stamp still due at publish). **Genuinely still open:** Campaign/Offering workspace views (never ported — `(decision)`), Perplex stable v4 `(external)`, Forms license `(external)`, NC→BlockList per-site run + value-transform dry-run, and uSync Publisher remote-server E2E `(external)`. *(The Auth0 8.5.0 SDK finish is now done — PR #883.)*
>
> **New since this doc:** Authentication + Auth0 migrated & merged (#876); the Auth0 ManagementApi 8.5.0 SDK migration follow-up is **done and open as PR #883** (builds 0 errors, awaiting merge into `v17`).
>
> ⚠️ **CURRENT STATE (2026-06-24):** `origin/v17` was merged into `v17-Talha`, **RESTRUCTURING** the frontend/build layout. The authoritative current layout is in **AGENTS.md**. Dated session summaries / entries below referencing `ClientApp/`, `Apps/`, `Cms/Build/`, `Cms/Extensions/`, `@n3o/build`, `@n3o/auth-fetch`, `@n3o/backoffice-core`, `N3O.Umbraco.React`, `BuildClientApp`/`BuildReactRuntime`, or `KonstruktConfigurator` reflect earlier intermediate states and are retained as history. **Current names:** shared packages `@repo/build-config` + `@n3oltd/backoffice-core` + `@n3oltd/backoffice-ui` under `src/frontend/`; React runtime is the standalone `N3O.Umbraco.ReactRuntime` project; per-plugin apps at `<Project>/frontend/<app>/`; build via root `Directory.Build.props`/`.targets` + Turbo; `KonstruktConfigurator`→`UIBuilderConfigurator`. **NOTE:** an old parallel layout (`Cms/Build/*`, `Cms/Extensions/*`, `Data.StaticAssets/Extensions/*`) is still git-tracked pending deletion.

> 🔎 **Migration follow-ups:** run **`git grep "TODO Migration Review"`** to find every migration follow-up in the code — deferred work, deprecated-API flags, stubs, and decisions a reviewer should verify (31 markers as of session 7).

> **Session 18 summary (2026-06-15) — closed the two open code decisions in this doc + `MIGRATION_BLOCKERS.md`:** (1) **RR-10 Bundling** — deleted the orphaned `N3O.Umbraco.Bundling` project (Smidge has no v17 runtime replacement; the v17 pattern is build-time Vite, already used repo-wide). (2) **Cropper/Uploader → native** (decision: Talha) — hard-replaced both custom editors with native `Umbraco.MediaPicker3`/`MediaWithCrops`: deleted the 6 plugin projects, refactored all in-repo consumers, removed Cropper from the `N3O.Umbraco.Data` import/export pipeline, switched DemoSite Uploader data types to native. **Full solution build 0 errors (verified).** The per-site existing-data migration is an **offline CLI** (media-node creation needs the Umbraco runtime) — runbook in `CROPPER_UPLOADER_NATIVE_MIGRATION.md`, same model as NC→BlockList. Remaining open items in these docs are now **all external/operational**, not code: BLOCKER-02 (await Perplex stable v4 upstream release), BLOCKER-08 (purchase Forms subscription license), and the per-site offline data migrations (NC→BlockList, Cropper/Uploader) + uSync-Publisher remote-server E2E.

> 📋 **Evaluation findings (session 10, read-only audit — nothing changed):** a 12-agent codebase sweep produced three companion documents for later evaluation:
> - **`NOT_REQUIRED_TO_RUN.md`** — optional / removable / dead items (orphaned projects, stubs, committed build outputs, dev-only tools like Diplo.GodMode) classified by whether they're needed to run.
> - **`TECH_DEBT_AND_MODERNIZATION.md`** — legacy patterns & better-approach opportunities: confirmed bugs (incl. a verified ContentPicker crash + v17 Block List export crash), security gaps (CORS wildcard, committed HMAC key, unauth'd upload), ~15 sync-over-async hot paths, deprecated APIs, dependency hygiene, the zero-tests/.NET-8-CI gaps, and U17/.NET10 modernization.
> - **`PACKAGING_RCL_RESEARCH.md`** — conclusion of the per-plugin `build/*.targets` vs. RCL (`Microsoft.NET.Sdk.Razor` + `StaticWebAssetBasePath`) question: the copy-targets pattern is legacy; RCL is the v17 standard (deferred, opt-in).

*Last updated: 2026-06-02 (session 10) — read-only audit (3 evaluation docs above); NC→BlockList migration to be done by a separate CLI app (BLOCKER-06)*

> **Session 10 summary:** (1) Ran a read-only 12-agent audit → the three evaluation docs above (committed `eba71cb88`); no code changed by the audit. (2) The Nested Content → Block List migration will be handled by a separate **CLI app**; the on-startup `PackageMigrationPlan` is disabled (commit `0d6c2aede`).

> **Session 9 summary:** Migrated all backoffice plugins from **Lit → React 19 + TypeScript + Vite** (except Cropper/Uploader — skipped pending the jQuery/native-picker decision — and the blocked telethon UI). Each plugin is a web-component shell mounting a React root; **React is shared via a self-hosted ESM runtime + import map** in `N3O.Umbraco.Cms/App_Plugins/N3O.Umbraco.React` (built by the `BuildReactRuntime` MSBuild target), kept external per plugin. Hybrid UI (UUI chrome + custom React for bespoke surfaces). 7 parallel subagents; full build 0 errors. Reference + recipe: `REACT_MIGRATION_GUIDE.md`. ⚠️ Runtime (in-browser import-map resolution + render) not yet verified — needs content fixtures; Blocks.Preview iframe→`dangerouslySetInnerHTML` divergence to eyeball; Scheduler/WelcomeDashboard React is overhead (kept for uniformity), Blazor.BackOffice left a vanilla loader. Detail: `SESSION_HANDOFF.md`.

> **Session 8 summary:** Restored the BLOCKER-10 access-control regressions (the "A-2" remaining-work item) — commit `c93028178`. **Hangfire dashboard** re-gated to Settings-section/admin via Umbraco's built-in `AuthorizationPolicies.SectionAccessSettings` (the v17 replacement for the removed `SectionRequirement`). **Export/Import** APIs gated server-side with a new reusable `[RequireUserGroup(...)]` filter on `ExportsController`/`ImportsController` (admin or the `exportUsers`/`importUsers` group; 403 otherwise) — enforced at the API boundary, stronger than v13's UI-only gate. The third item (Platforms-Preview → offering-composition content types only) is **deferred** — display-only and needs a custom Bellissima condition (no built-in "composes composition X" condition exists); marked `TODO Migration Review (BLOCKER-10 #3)`. BLOCKER-10 downgraded **High → Low**. Build 0 errors.

> **Session 7 summary:** Merged **`origin/main`** into `v17-Talha` (commit `d13c2d78a`) — NOT `origin/v17`, which is merely `main` + 6 unfinished WIP commits (a half-done Offering/Elements refactor). `main` is the canonical latest stable and is **DonationFormState-aligned** with our branch, so it merged cleanly (114 csproj → our v17 packages; one code union in `StagingMiddleware`). This brought in main's production hardening: **Sentry, health checks & readiness, HomepageWarmup, telemetry, concurrency tuning, CI**. Incoming v13 APIs migrated to v17 (Humanizer 3.0 namespace; `GetAtRoot`→`TryGetRootKeys`). Then a **codebase-wide CS0618 deprecated-API sweep** (commit `203a1199b`, 11 subagents) cut "removal in Umbraco 18/19" warnings **76 → 5**. All pending/deferred items (and migration-decision notes a reviewer should verify) now carry a searchable **`TODO Migration Review`** comment (`git grep "TODO Migration Review"` → 31 markers). Full solution builds **0 errors**. The 6 unfinished `origin/v17` WIP commits were deliberately **not** merged.

---

## Executive Summary

N3O.Umbraco is a **shared Umbraco package framework** (120 projects) consumed by multiple client sites. The migration target is **Umbraco 17.3.5 on .NET 10**.

**Current state:** The solution **builds with 0 errors** and **the app starts successfully (HTTP 200 confirmed)**. All compile-time API breakages and the blocking runtime startup crashes have been resolved. **The AngularJS → Bellissima frontend migration (BLOCKER-07) is now largely done** — all 16 plugin areas migrated to `umbraco-package.json` + Lit (15 done, telethon blocked); dashboards + Data property editors verified live in the running backoffice; a critical property-editor alias bug was found and fixed.

**Session 6 update:** All 16 Bellissima plugin areas (13 build-unit projects) were converted from plain-JS Lit to **TypeScript + Vite** (the Umbraco-official package build) — each project gains a `ClientApp/` (`package.json`/`tsconfig.json`/`vite.config.ts`/`src/*.ts`) and an MSBuild `BuildClientApp` target that runs `npm ci`/`npm run build` on every `dotnet build`; `@umbraco-cms/backoffice/*` stays external (runtime import-mapped), own code + npm libs are bundled (Handsontable/cropperjs/@editorjs npm-bundled; formstone+jQuery kept vendored + flagged). Done via 12 parallel Sonnet subagents off a verified reference (SerpEditor). **Full solution build 0 errors.** Restarting the app surfaced **BLOCKER-11** (a hard boot-crash: `DataComposer.EnsureDataTypeExists` re-inserting an existing data type) — **fixed** (lookup by deterministic Key via `GetAsync`). **Live backoffice smoke-test passed** (logged in): WelcomeDashboard + Scheduler render, 6 referenced property-editor UIs register & are selectable, no duplicate data types, zero N3O console errors. New build prerequisite: **Node on every build/CI machine**. Recipe guide: `TYPESCRIPT_MIGRATION_GUIDE.md`. Remaining frontend gap: in-content live-render (needs fixtures) + the 5 plugins not referenced by DemoSite.Web.

**Session 5 update:** uSync Publisher `SyncContentHandler` reimplemented on the v17 `PublisherProcessor`/`Jumoo.Processing` API (RR-01/BLOCKER-05 — *resolved in code, needs remote-server E2E*). A **14-agent branch deliberation review** (verdict: make-sense-with-fixes) ran over the whole `v17-Talha` diff — full tracker `REVIEW_FINDINGS.md`. Four review-found defects were fixed (CRITICAL duplicate `Umbraco.BlockList` converter → deleted; NC migration empty-path JSON + non-transactional steps; `UrlInfo.AsUrl` arg order), and the **NestedContent→BlockList code replacement** was completed (lookup re-key, `[Obsolete(error:true)]` redirects, a live `DonationItemReceiver` caller fixed). New production blockers surfaced: **BLOCKER-10 access-control regressions** and **BLOCKER-11 `EnsureDataTypeExists` dup-data-types** (see `MIGRATION_BLOCKERS.md`). Remaining: **live-render testing (needs content fixtures), the deferred review blockers, public-API obsolete/guide work, and per-site data migration (+ real-data dry-run of the NC→BlockList value transform)**. See `BELLISSIMA_MIGRATION_LOG.md` and `REVIEW_FINDINGS.md`.

---

## Migration Progress

| Area | Status | Notes |
|---|---|---|
| Target framework (all 120 projects) | **Done** | All on `net10.0` |
| Umbraco core packages | **Done** | `Umbraco.Cms` 17.3.5 everywhere |
| Solution compiles (0 errors) | **Done** | Verified full solution build |
| Startup pipeline | **Done** | `AddBackOffice/AddWebsite/AddDeliveryApi/AddComposers/AddContentment` |
| `UmbracoApiController` → `ControllerBase` | **Done** | Both base classes + all downstream controllers |
| `SectionRequirement` → `RequireAuthenticatedUser` | **Done** | SchedulerComposer |
| `IPublishedCache` abstraction (Locator) | **Done** | Split into typed `IPublishedContentCache`/`IPublishedMediaCache` |
| `IPublishedSnapshotAccessor` removal | **Done** | All files updated |
| `IPublishedCache.GetAtRoot()` | **Done** | `IDocumentNavigationQueryService.TryGetRootKeys` |
| Block List API (`BlockGridValue`, `BlockItemData.Values`) | **Done** | BlockValueExtensions, BlockItemDataExtensions |
| Block preview controller | **Done** | Cache lookups, language service, controller base |
| `ILocalizationService` → `ILanguageService` | **Done** | Block preview, localization accessor, cloud |
| Auth0 BackOffice security namespace | **Done** | `Umbraco.Cms.Api.Management.Security`; `SchemeForBackOffice` now static |
| Auth0 removed options (`Icon`, `AutoRedirectLoginToExternalProvider`) | **Done** | Both removed |
| URL providers (Blog, Events, Vacancies) | **Done** | `NewDefaultUrlProvider`, abstract `Alias`, virtual `GetPreviewUrlAsync` |
| `Link.Content` → `Link.Url` | **Done** | |
| `DataEditorAttribute` 1-arg constructor | **Done** | All custom data editors |
| `ConfigurationEditor` `ioHelper`-only constructor | **Done** | |
| `ContentTypeSort.Id` → `.Key` | **Done** | |
| `DataEditor.Name` → `.Alias` | **Done** | |
| `DateTimeConfiguration.Format` removal | **Done** | Separate DateOnly/DateTime aliases |
| `Aliases.TinyMce` → `Aliases.RichText` | **Done** | |
| `MultiNodePicker.StartNodeId` → `Guid?` | **Done** | `.ToId()` removed |
| Nested Content extension stubs (throw `NotSupportedException`) | **Done** | Guard rails in place |
| `PropertyBuilder.Nested.cs` stubs | **Done** | Throws `NotSupportedException` |
| DynamicListViews → Bellissima | **Done** | `umbraco-package.json` + Lit `workspaceView` + `condition` |
| **Contentment** → `Umbraco.Community.Contentment 6.1.4` | **Done** | Package renamed; `AddContentment()` restored; API updated |
| **Konstrukt → Umbraco.UIBuilder 17.2.0** | **Done** | All value mappers, configurator, composer, data builders ported |
| Engage infrastructure namespaces | **Confirmed unchanged** | `Umbraco.Engage.Infrastructure.*` unchanged in v17.2.2 |
| Perplex.ContentBlocks → v4.0.0-rc.3 | **Done** | `ElementTypeKey`, `BlockNameTemplate`, `ContentBlocksValue` API updated |
| uSync → 17.3.6 | **Done** | |
| Umbraco.Workflow → 17.0.2 | **Done** | |
| Azure Blob Storage → 17.0.0 | **Done** | |
| GMaps → 17.0.0 | **Done** | |
| Umbraco.Forms → 17.0.x | **Done** | Aligned to 17.0.1 (Forms + StaticAssets bumped to match Forms.Core; verified on nuget.org) (2026-06-02) |
| Umbraco.Engage → 17.2.2 | **Done** | Package upgraded; namespaces confirmed valid |
| `Directory.Build.targets` runtime stripping | **Done** | Strips `Umbraco.Web.BackOffice.dll` (v13) from output |
| `SaveAndPublish` → `Save` + `Publish` | **Done** | |
| Marketing `UmbracoEngageSegmentsDataSource` | **Done** | `ContentmentConfigurationField` return type fix |
| Contentment `IDataPickerSource` v6 API | **Done** | `GetItems()`, `SearchAsync` → `PagedViewModel<T>`, `Fields` type |
| **App starts + HTTP 200** | **Done** | Verified: Umbraco boots, Hangfire starts, OpenIddict running |
| `NumberFormatter` eager DB hit at startup | **Done** | `NumberFormat` property made lazy (`??=`); DB only hit on first format call |
| `DateTimeFormatter` eager DB hit at startup | **Done** | `DateFormat`/`TimeFormat`/`Timezone` properties made lazy (`??=`) |
| `UIBuilderComposer` double-registration crash | **Done** | Removed manual configurator loop; UIBuilder auto-discovers `IConfigurator` |
| `UIBuilderConfigurator.GetContentSection` | **Done** | `ConditionalWeakTable` ensures `WithSection("content")` called once per builder |
| `launchSettings.json` `dotnetRunMessages` type | **Done** | Fixed `"true"` string → `true` boolean |
| **Assembly `<Version>` tags** | **Done (placeholder, 2026-06-02)** | Bulk-set `13.0.0`→`17.0.0` across 114 csproj; re-stamp CalVer before NuGet publish |
| **All App_Plugins AngularJS → Bellissima** | **Done (2026-06-02)** | 15/16 areas migrated to `umbraco-package.json` + Lit (telethon blocked on RR-02). Property-editor UI alias must = backend `[DataEditor]` alias. Detail: `BELLISSIMA_MIGRATION_LOG.md` |
| **All Bellissima plugins → TypeScript + Vite** | **Done (2026-06-02, session 6)** | 16 areas / 13 build units. Per-project `ClientApp/` + MSBuild `BuildClientApp` (npm ci/build); `@umbraco/*` external, libs bundled. Build 0 errors; smoke-tested live. Needs Node on build machines. Guide: `TYPESCRIPT_MIGRATION_GUIDE.md` |
| `DataComposer.EnsureDataTypeExists` dup data types (BLOCKER-11) | **Done (2026-06-02, session 6)** | Lookup by deterministic Key via `GetAsync` (was `GetDataType(alias)` by Name → boot-crash on restart). Verified live: no dup data types |
| Content app registration (Import/Export/Preview) | **Done (migrated)** | Now `workspaceView` extensions (Data.Import, Data.Export, Cloud.Platforms.Preview); not yet live-rendered (need a content node) |
| Dashboard registration (Scheduler, Welcome) | **Done + verified live** | `dashboard` extensions; both render in the backoffice (Welcome=Content, Scheduler=Settings/Hangfire iframe) |
| Property editor UI alias = backend `[DataEditor]` alias | **Done** | Critical fix: data types store `editorUiAlias`; all 8 custom editors re-aliased to backend alias (TextResourceEditor → `N3O.Umbraco.TemplateTextEditor`) |
| Cropper / Uploader native-picker migration | **Done (framework side, 2026-06-15 session 18)** | Decision (Talha): switch to native, hard-replace types, adopt native behavior. Deleted the 6 Cropper/Uploader plugin projects; in-repo consumers now use native `MediaWithCrops`; Cropper removed from the `N3O.Umbraco.Data` import/export pipeline; DemoSite Uploader data types → `Umbraco.MediaPicker3`. Build 0 errors. **Per-site existing-data migration = offline CLI** (runbook: `CROPPER_UPLOADER_NATIVE_MIGRATION.md`) |
| Campaign/Offering backoffice notifications | **Not started** | `SendingContentNotification` stubs; Bellissima workspace views needed |
| TelethonOnAir Cockpit factory | **Done (2026-06-02)** | `TelethonOnAirCockpitSegmentRuleFactory` implemented + registrations re-enabled; v17.2.2 API verified by reflection. AngularJS telethon UI still blocked. |
| `GetNestedPropertySchemaHandler` | **Done — removed (2026-06-02)** | Was dead (zero callers); handler + query deleted. `NestedSchemaRes`/mapping kept (still live). |
| Content data migration (Nested Content → Block List) | **Not started (per-site run)** | Migration class written + now registered; still needs per-site backup/dry-run/run |
| Nested Content DB migration registration | **Done (2026-06-02)** | `N3ONestedContentMigrationPlan` added; auto-discovered via `IDiscoverable`. Runs on startup; no-ops without NC data types. |
| uSync Publisher v17 (`SyncContentHandler`) | **Blocked** | `IPublisherStateService` removed; Jumoo API not yet documented |
| Perplex stable release | **Blocked** | On v4.0.0-rc.3; waiting for stable v4 |

---

## AngularJS → Bellissima Migration (Largest Remaining Work)

All `package.manifest` files and AngularJS controllers must be replaced. In Umbraco 14+ (Bellissima), the backoffice uses Lit-based web components registered via `umbraco-package.json`. AngularJS APIs (`angular.module`, `$scope`, `$http`, `editorState`, `assetsService`) are completely gone.

### Extension type mapping

| Old AngularJS pattern | Bellissima replacement |
|---|---|
| Property editor controller + `package.manifest` `propertyEditors` | `propertyEditorUi` extension in `umbraco-package.json` + Lit `UmbPropertyEditorUiElement` |
| Content App (`IContentAppFactory` + controller) | `contentApp` extension in `umbraco-package.json` + Lit web component |
| Dashboard (`IDashboard` + controller + view) | `dashboard` extension in `umbraco-package.json` + Lit web component |
| Section/Tree registration | `section`, `menuItem`, `treeItem` extensions |
| Workspace view | `workspaceView` extension (replaces `SendingContentNotification` tab injection) |
| Notification/tab injection (`SendingContentNotification`) | `workspaceView` or `workspaceAction` extension |
| `assetsService.loadJs/Css` | Bundle dependencies into the Lit component or import as ES module |

### Files to migrate (per project area)

| Project / Plugin | Files | Extension type | Notes |
|---|---|---|---|
| `Blocks.StaticAssets` / `N3O.Umbraco.Blocks.Preview` | `block-preview.controller.js`, `bind-compile.directive.js`, `block-preview.html`, `package.manifest` | `blockEditorCustomView` | Uses `editorState`, `$sce`, `umbRequestHelper`; backend endpoint unchanged |
| `Cloud.Platforms.StaticAssets` / `N3O.Umbraco.Cloud.Platforms.Preview` | `N3O.Umbraco.Cloud.Platforms.Preview.Controller.js`, `package.manifest` | `contentApp` | |
| `Cloud.Platforms.Marketing.StaticAssets` / `telethon-on-air-rule` | `segment-rule-telethon-on-air.js`, `-editor.js`, `-display.js`, `package.manifest` | Engage segment rule UI | **Blocked on Engage BLOCKER-04 (cockpit factory empty)** |
| `Data.StaticAssets` / `N3O.Umbraco.Data.Export` | Controller.js + `package.manifest` | `contentApp` | Uses `editorState`, `assetsService` |
| `Data.StaticAssets` / `N3O.Umbraco.Data.Import` | Controller.js + `package.manifest` | `contentApp` | |
| `Data.StaticAssets` / `N3O.Umbraco.Data.ImportDataEditor` | Controller.js + `package.manifest` | `propertyEditorUi` | |
| `Data.StaticAssets` / `N3O.Umbraco.Data.ImportNoticesViewer` | Controller.js + `package.manifest` | `propertyEditorUi` | |
| `Plugins/Cells` / `N3O.Umbraco.Cells` | Controller.js + `package.manifest` | `propertyEditorUi` | Wraps Handsontable; load via ES import |
| `Plugins/Cropper` / `N3O.Umbraco.Cropper` | Controller.js + `package.manifest` | `propertyEditorUi` | Uses Formstone; reuse as ES module |
| `Plugins/EditorJs` / `N3O.Umbraco.EditorJs` | Controller.js + `package.manifest` | `propertyEditorUi` | Uses editorjs bundle; load via ES import |
| `Plugins/SerpEditor` / `N3O.Umbraco.SerpEditor` | Controller.js + `package.manifest` | `propertyEditorUi` | |
| `Plugins/TextResourceEditor` | Controller.js + `package.manifest` | `propertyEditorUi` | |
| `Plugins/Uploader` / `N3O.Umbraco.Uploader` | Controller.js + `package.manifest` | `propertyEditorUi` or `dashboard` | |
| `Plugins/WelcomeDashboard` | Controller.js (empty controller) + `package.manifest` | `dashboard` | |
| `Scheduler.StaticAssets` / `N3O.Umbraco.Scheduler` | `N3O.Umbraco.Scheduler.html` (iframe) + _(no JS controller)_ | `dashboard` | Wrap Hangfire iframe in Lit component |
| `Blazor.BackOffice` | `package.manifest` only (JS is non-AngularJS Blazor loader) | `script` extension | Low risk; JS reusable; just update manifest format |

**Already migrated:** `N3O.Umbraco.DynamicListViews` — fully Bellissima with `umbraco-package.json`, `LitElement`, `UmbElementMixin`.

---

## Remaining Runtime Issues

### RR-01 — ✅ DONE (2026-06-02) — `SyncContentHandler` (uSync Publisher v17)
- `src/Sync/N3O.Umbraco.Sync.Extensions/Handlers/SyncContentHandler.cs` + `SyncExtensionsComposer.cs`
- Reimplemented against the real uSync.Publisher v17 API (discovered by reflection — Jumoo publishes no docs for it). Injects `PublisherProcessor` (wraps `Jumoo.Processing.Core.Pipelines.IPipelineService`), calls `Process(PublisherActionRequest, PublisherProcessingOptions)` → `SyncPublishResponse`, same Document-UDI push w/ published deps, throws on `!Success`. Build 0 errors; boots clean. See BLOCKER-05 for the API map + the one runtime caveat (multi-step pipeline completion needs an end-to-end test with a remote uSync server).

### RR-02 — ✅ DONE (2026-06-02) — `PlatformsMarketingComposer` + `TelethonOnAirCockpitSegmentRuleFactory` (Engage)
*Implemented: factory written against verified v17.2.2 API (`.Rules` sub-namespace; `out CockpitSegmentRule?`), both DI registrations re-enabled, build 0 errors. AngularJS telethon UI still blocked (BLOCKER-04 client side).*
- `src/Cloud/N3O.Umbraco.Cloud.Platforms.Marketing/PlatformsMarketingComposer.cs` — both `ICockpitSegmentRuleFactory` and `ISegmentRuleFactory` registrations are commented out.
- `src/Cloud/N3O.Umbraco.Cloud.Platforms.Marketing/Services/Campaigns/TelethonOnAirCockpitSegmentRuleFactory.cs` — empty file.
- **Fix:** Implement `TelethonOnAirCockpitSegmentRuleFactory` against `ICockpitSegmentRuleFactory` (namespace: `Umbraco.Engage.Web.Cockpit.Segments`); update `TryCreate` to the v17 signature (nullable out param); re-enable both DI registrations in the Composer.
- **Note:** `ISegmentRuleFactory`, `ISegmentRule`, `BaseSegmentRule`, `ISegmentRepository`, `IPersonalizationProfile` namespaces are all **unchanged** in Engage 17.2.2.

### RR-03 — ✅ DONE (2026-06-02, removed) — `GetNestedPropertySchemaHandler` throws `NotImplementedException`
*Verified dead (zero callers solution-wide); handler + `GetNestedPropertySchemaQuery` deleted. `NestedSchemaRes`/`NestedSchemaResMapping` kept (still live via `NestedValueResMapping`).*
- `src/Data/N3O.Umbraco.Data/Handlers/Content/GetNestedPropertySchemaHandler.cs` line 13.
- **Fix:** Either implement for Block List schema export, or remove the handler and its associated query/response types if no longer needed.

### RR-04 — Content Apps not registered (Import, Export, Platforms Preview)
- C# stubs exist (namespace-only files with TODO comments). No `umbraco-package.json` created.
- **Fix:** Create `umbraco-package.json` `contentApp` extension entry + Lit web component for each. The backend API endpoints in their controllers are functional.

### RR-05 — Campaign/Offering backoffice notifications absent
- `src/Cloud/N3O.Umbraco.Cloud.Platforms/Notifications/Campaigns/CampaignSending.cs` and `OfferingSending.cs` — stub files.
- **Fix:** Implement as Bellissima `workspaceView` or `workspaceAction` extensions in `umbraco-package.json`.

### RR-06 — Dashboards not registered (Scheduler, WelcomeDashboard)
- Both `SchedulerDashboard.cs` and `WelcomeDashboard.cs` are namespace-only stubs.
- **Fix:** Create `umbraco-package.json` `dashboard` extension + Lit component for each.

### RR-07 — ✅ DONE (2026-06-02) — `GetPreviewUrlAsync` returns null
*Base `UrlProvider.GetPreviewUrlAsync` now mirrors core `NewDefaultUrlProvider` using `this.Alias` (key-based preview); covers Blog/Events/Vacancies, no per-subclass overrides needed.*
- `src/N3O.Umbraco.Extensions/UrlProviders/UrlProvider.cs` base implementation returns `null`.
- **Fix:** Implement per subclass or provide a base implementation deriving from `GetUrl`.

### RR-08 — ✅ DONE (2026-06-02) — Data controllers unauthenticated (pre-existing, security risk)
*Added `[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]` to ContentController/ContentTypesController/DataTypesController (base `ApiController` kept; route stays `/umbraco/api/...`). Unauthenticated callers now get 401.*
- `ContentController.cs`, `ContentTypesController.cs`, `DataTypesController.cs` — all have `// TODO Add authentication`.
- **Fix:** Add `[Authorize]` attribute or equivalent. Pre-existing, but must resolve before any internet-facing deployment.

### RR-09 — ✅ ALREADY DONE — `SaveAndPublish` bare exception (pre-existing)
*Current code throws `InvalidOperationException` surfacing `EventMessages` + `StatusType` (`Result`) for both save and publish failures. No action needed.*
- `src/N3O.Umbraco.Extensions/Extensions/ContentServiceExtensions.cs` — throws bare `Exception` discarding `PublishResult` detail.
- **Fix:** Surface `EventMessages`/`StatusType` in the exception.

### RR-10 — ✅ DONE (2026-06-15, session 18) — `Bundling` orphaned project deleted
*Decision (research-backed): Smidge was removed in Umbraco 14 and **has no runtime replacement** — the v17 pattern is **build-time bundling via Vite** with static `<link>`/`<script>` references (the exact pattern this repo already uses for every backoffice ClientApp; see [Umbraco docs](https://docs.umbraco.com/umbraco-cms/get-started/upgrading-and-migrating/version-specific)). `N3O.Umbraco.Bundling` was fully orphaned (zero ProjectReferences anywhere, no `IAssetBundle` impls, tag helpers never `@addTagHelper`-registered), so there was nothing to reimplement. **Actioned:** deleted the project via `dotnet sln remove` (cleaned the GUID/config/folder entries) + removed the directory; removed the two inert `<n3o-css-bundle/>`/`<n3o-js-bundle/>` literals from DemoSite `Layout.cshtml`. Build 0 errors. Any consuming site needing front-end bundling does it at build time with Vite — no runtime service required.

---

## Package Status

| Package | Version | Status | Action |
|---|---|---|---|
| `Umbraco.Cms` | 17.3.5 | OK | — |
| `Umbraco.Community.Contentment` | 6.1.4 | **Done** | Was BLOCKER-01; resolved with package rename |
| `Umbraco.UIBuilder` | 17.2.0 | **Done** | Was BLOCKER-03 (Konstrukt); port complete |
| `Umbraco.Engage` | 17.2.2 | **Done (package)** | Namespaces confirmed valid; Cockpit factory needs implementing |
| `Umbraco.Forms` | 17.0.0 / 17.0.1 | OK (minor skew) | Align both projects to same patch version |
| `uSync.Complete` | 17.3.6 | OK | — |
| `Umbraco.Workflow` | 17.0.2 | OK | — |
| `Umbraco.StorageProviders.AzureBlob` | 17.0.0 | OK | — |
| `Our.Umbraco.GMaps` | 17.0.0 | OK | API changes unverified — test in backoffice |
| `Perplex.ContentBlocks` | 4.0.0-rc.3 | **Pre-release** | Upgrade to stable v4.x when released |
| `Umbraco.Code` | 2.4.0 | **Unconfirmed** | Verify v17 compat; drop if only design-time |
| `Diplo.GodMode` | 17.0.0 | OK | — |
| `uSync.Publisher` | n/a | **Blocked** | `IPublisherStateService` removed; v17 API not documented |
| All `<Version>` tags | 13.0.0 | **Must fix** | Update to `17.x.x.x` before any NuGet publish |

---

## Content Data Migration

Per consuming site — code-only package migration is insufficient.

**CM-01 — Nested Content → Block List (Critical, blocking for live data)**
- Migration class: `src/N3O.Umbraco.Extensions/Migrations/NestedContentToBlockListMigration.cs`
- **Still needed:** Register in an `AsyncPackageMigrationBase` / `IMigrationPlan`. Example:
```csharp
public class N3ONestedContentMigrationPlan : PackageMigrationPlan {
    public N3ONestedContentMigrationPlan() : base("N3O.Umbraco.NestedToBlockList") { }
    protected override void DefinePlan() {
        To<NestedContentToBlockListMigration>("2026-NestedContent-v1");
    }
}
```
- Checklist per site: back up DB → verify Block List data types exist → dry-run on copy → run → regenerate uSync XML.

**CM-02 — uSync XML for Nested Content data types**
- Regenerate after running CM-01: `uSync export` → commit to source control → verify clean import.

**CM-03 — Engage analytics/personalisation data**
- Follow Umbraco Engage v13→v17 upgrade guide; run Engage DB migrations in order.

**CM-04 — Block Editor v15 format + UTC dates (auto)**
- v15 auto-migrates Block Editor data; v17 auto-migrates dates to UTC. Both run on first startup — schedule a maintenance window and back up first.

**CM-05 — RTE: TinyMCE → TipTap (review)**
- v16 auto-migrates data types; review RTE-dependent Razor/email/feed output.

---

## Recommended Work Order

**Phase 1 — Bellissima frontend (largest remaining effort, can parallelise)**
1. Migrate all 15 AngularJS plugin areas to Lit web components + `umbraco-package.json` (see AngularJS table above).
2. Register content apps (Import, Export, Platforms Preview) via `umbraco-package.json`.
3. Register dashboards (Scheduler, WelcomeDashboard) via `umbraco-package.json`.
4. Implement Campaign/Offering workspace views via `umbraco-package.json`.

**Phase 2 — Runtime correctness**
5. RR-02: Implement `TelethonOnAirCockpitSegmentRuleFactory` + re-enable Composer registrations.
6. RR-03: Implement or remove `GetNestedPropertySchemaHandler`.
7. RR-07: Implement `GetPreviewUrlAsync` in URL provider subclasses.
8. RR-09: Fix `SaveAndPublish` exception handling.
9. RR-08: Add authentication to Data API controllers.
10. RR-10: Evaluate Bundling — implement or delete.

**Phase 3 — Package cleanup**
11. Align `Umbraco.Forms` versions (17.0.0 → 17.0.1 everywhere).
12. Upgrade `Perplex.ContentBlocks` to stable v4.x when released.
13. Verify `Umbraco.Code` 2.4.0 / `Our.Umbraco.GMaps` 17.0.0 in backoffice.
14. Bump all `<Version>` tags from `13.0.0` to `17.x.x.x` before any NuGet publish.

**Phase 4 — Content data migration (per site)**
15. CM-01: Register + run `NestedContentToBlockListMigration`.
16. CM-02: Regenerate uSync XML post-migration.
17. CM-03 / CM-04 / CM-05: Engage data, Block format, UTC dates, TipTap review.

**Phase 5 — Blocked items (when unblocked)**
18. RR-01: Implement `SyncContentHandler` against new uSync.Publisher v17 API (Jumoo.Processing) when documented.

---

## Risk Register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Bellissima frontend rewrite scope underestimated | High | High | 15+ AngularJS controllers; each needs Lit + TypeScript; start early with the highest-traffic plugins (Data Import/Export, Block Preview) |
| uSync Publisher v17 API never documented / breaking | Medium | High | Monitor Jumoo; isolate `SyncContentHandler` behind a feature flag so the rest of the site works without it |
| Perplex ContentBlocks v4 stable not released | Low | Medium | v4.0.0-rc.3 is functional; assess whether RC is acceptable for production |
| Engage Cockpit factory implementation harder than expected | Low | Medium | `ICockpitSegmentRuleFactory` and `IPersonalizationProfile` are confirmed in Engage 17.2.2; spike the implementation first |
| GMaps v17.0.0 API changes break map editors | Medium | Medium | Test map property editor in backoffice before shipping |
| Nested Content content-migration data loss | Medium | High | Idempotent migration + dry-run on DB copy + before/after content review |
| Umbraco.Code 2.4.0 not v17-compatible | Low–Med | Medium | Verify; if design-time only, commit generated code and drop reference |
| Assembly version `13.0.0` causing consumer confusion | High | Low | Global find/replace in csproj before any package publish |

---

## Definition of Done

Migration is complete when:

- [x] All 120 projects target `net10.0`
- [x] Solution builds with 0 errors
- [x] No removed C# APIs used (UmbracoApiController, IPublishedCache, IPublishedSnapshotAccessor, NestedContentConfiguration, abstract BlockValue, BlockItemData.PropertyValues, etc.)
- [x] All third-party packages on v17-compatible versions (Contentment 6.1.4, UIBuilder 17.2.0, Engage 17.2.2, Forms 17.x, uSync 17.3.6, Workflow 17.0.2, AzureBlob 17.0.0, GMaps 17.0.0, Perplex 4.x)
- [x] All AngularJS `package.manifest` + controllers replaced with `umbraco-package.json` + Lit web components *(15/16; telethon blocked on BLOCKER-04)*
- [x] All Bellissima plugins converted to **TypeScript + Vite** (session 6) — build 0 errors; `BuildClientApp` MSBuild target wires npm into `dotnet build`
- [~] Content apps (Import, Export, Platforms Preview) registered as `workspaceView` — **migrated; live-render test pending (needs content node)**
- [x] Dashboards (Scheduler, WelcomeDashboard) registered and **verified rendering** in backoffice
- [~] Property editors — **registered & selectable in the live backoffice (6 referenced UIs verified session 6)**; render *inside a content workspace* still pending content/data-type fixtures (Cells, EditorJs, SerpEditor, TextResourceEditor). **Cropper & Uploader removed (session 18)** — replaced by native `Umbraco.MediaPicker3`
- [x] **Decision:** Cropper/Uploader — **switch to Umbraco native pickers** (Talha, 2026-06-15). Framework-side editor removal done + build-verified; per-site data migration is an offline CLI (`CROPPER_UPLOADER_NATIVE_MIGRATION.md`)
- [ ] Campaign/Offering workspace views implemented
- [ ] `TelethonOnAirCockpitSegmentRuleFactory` implemented and registered
- [ ] `GetNestedPropertySchemaHandler` implemented or removed
- [ ] `GetPreviewUrlAsync` implemented for all URL provider subclasses
- [x] Data controllers authenticated (RR-08)
- [~] Access-control gating restored (BLOCKER-10) — Hangfire (`SectionAccessSettings`) + Export/Import (`[RequireUserGroup]`, server-side) **done**; Platforms-Preview content-type condition **deferred** (display-only, needs custom Bellissima condition)
- [x] Bundling service implemented or removed — **removed** (orphaned `N3O.Umbraco.Bundling` deleted; v17 = build-time Vite, no runtime service)
- [ ] `SaveAndPublish` has production-grade error handling
- [ ] All `<Version>` tags updated from `13.0.0` to `17.x`
- [ ] `uSync.Publisher` `SyncContentHandler` reimplemented (when Jumoo docs available)
- [ ] `Perplex.ContentBlocks` on stable v4.x
- [ ] Nested Content → Block List data migration registered, tested, run per site
- [ ] uSync XML regenerated post-migration
- [ ] Engage data migration run
- [ ] Auto-migrations (Block format, UTC, TipTap) validated on a DB copy
- [ ] Manual backoffice regression testing: block preview, content apps, dashboards, import/export, segment rules, map editor, Forms, Workflow, uSync round-trip
- [ ] At least one downstream client site builds and runs end-to-end

---

*Effort note: The Bellissima frontend (Phase 1) is **done** — all plugin areas are Lit web components, now on TypeScript + Vite (session 6), build-verified and smoke-tested live. The largest remaining efforts are now **Phase 4 per-site content data migration** (NC→BlockList value-transform dry-run on a real legacy DB) and the **deferred security/decision blockers** (BLOCKER-10 access-control, RR-10 Bundling), plus in-content live-render testing once content fixtures exist.*
