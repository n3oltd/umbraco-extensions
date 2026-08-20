# Migration Blockers

> ✅ **CURRENT STATE (2026-06-29) — authoritative; audit re-verified these against current code.** The originally-numbered blockers are unchanged from the 2026-06-15 banner below: **genuinely still open = all EXTERNAL or per-site OPERATIONAL** — BLOCKER-02 (Perplex stable v4, upstream), BLOCKER-04 (telethon-on-air *client* UI, needs Engage v17 segment-rule API), BLOCKER-05 (uSync Publisher remote-server E2E), BLOCKER-06 (NC→BlockList per-site offline CLI run + value-transform dry-run), BLOCKER-08 (Umbraco Forms subscription license). All earlier numbered code blockers (01,03,04-C#,05-code,07,09,10,11) remain RESOLVED.
>
> **⚠️ The repeated "build 0 errors" claims below are now stale** (updated 2026-06-30) — `dotnet build N3O.Umbraco.sln` on `v17-Talha` now has **2 root causes** remaining; the Auth0 SDK bump (one of the two *new* ones) is **resolved**:
> - ✅ **RESOLVED — Auth0.ManagementApi 8.5.0 SDK bump** in `N3O.Umbraco.Authentication.Auth0` (`UserDirectory.cs`/`.I.cs`): v8 dropped the `Auth0.ManagementApi.Models` namespace + the single `User` type; ticket/user/connection APIs renamed. Migrated to the v8 API (PR **#883**: `Auth0User` model + async `ToAuth0UserAsync` mapping; `IUserDirectoryConnections` caches federated domain aliases per `UserDirectoryType`) — the project builds 0 errors. Follow-up to the merged Authentication PR (#876). API mapping: [[auth0-managementapi-v8-migration]].
> - **NEW — DemoSite.Web** has a stale `Import` of `N3O.Umbraco.Cms\build\N3O.Umbraco.Cms.targets` (Cms is now an RCL — drop the import).
> - `npm ci` environmental failure in `Directory.Build.targets`.
>
> Per-project migration/PR status now lives in **GitHub issue [n3oltd/work#729](https://github.com/n3oltd/work/issues/729)** + the `MIGRATION_PR_TRACKER.md` 2026-06-29 banner.
>
> ⚠️ **SUPERSEDED (2026-06-10, session 14):** a full read-only audit re-verified every item here against
> the current code — see **[`MIGRATION_AUDIT_2026-06-10.md`](MIGRATION_AUDIT_2026-06-10.md)** (the current
> source of truth). Many items below are now DONE (Content Apps→workspaceViews, dashboards, Hangfire auth
> (BLOCKER-10a), uSync ctor (BLOCKER-05), NC-migration removed + DataComposer (BLOCKER-06/11)). Still open:
> BLOCKER-02 (Perplex rc.3), BLOCKER-04 (Engage TelethonOnAir client registration), BLOCKER-07 (orphaned
> Bundling), BLOCKER-08 (Forms/Engage license), BLOCKER-10 #1/#2 (CampaignSending handlers). **The NEW
> Block List/Grid Data Export crash is FIXED + runtime-verified (2026-06-11); BLOCKER-10 #3 (Platforms-
> Preview gating) + Export/Import content-app gating RESTORED (2026-06-11) via `N3O.Condition.WorkspaceVisibility`.**
> Defer to the audit doc for current status.

> **Session 18 update (2026-06-15):** every remaining **code-actionable** item in this doc is now resolved.
> RR-10 Bundling → **deleted** (orphaned; build-time Vite is the v17 pattern). Cropper/Uploader → **switched to
> native `Umbraco.MediaPicker3`** framework-side (6 plugin projects deleted, consumers on `MediaWithCrops`; build 0
> errors). BLOCKER-02 action #3 (dead `GetOrCreateDataTypeContainer`) → confirmed already removed. BLOCKER-11
> follow-up (`EditorUiAlias`) → confirmed already set. **The only genuinely-open items left are EXTERNAL or
> per-site OPERATIONAL, none fixable in this repo's code:** BLOCKER-02 (await Perplex stable v4 *upstream release*),
> BLOCKER-08 (purchase Umbraco Forms *subscription license*), BLOCKER-06 (per-site NC→BlockList *offline CLI run*),
> Cropper/Uploader per-site data migration (*offline CLI*, runbook `CROPPER_UPLOADER_NATIVE_MIGRATION.md`), and the
> BLOCKER-05 uSync-Publisher *remote-server E2E test*.

*Last updated: 2026-06-15 (session 18)*

> **Session 7:** Merged **`origin/main`** (latest stable, DonationFormState-aligned) into `v17-Talha` — bringing Sentry/health-checks/telemetry/CI — and ran a codebase-wide CS0618 deprecated-API sweep (76→5 warnings). `origin/v17` was NOT merged (it is `main` + 6 unfinished WIP commits). All pending items now carry a searchable **`TODO Migration Review`** code comment (`git grep "TODO Migration Review"`). Build 0 errors; pushed to `origin/v17-Talha`. Details: `SESSION_HANDOFF.md`. **Newly relevant to blockers below:** BLOCKER-10 (Hangfire auth) is marked in `SchedulerComposer.cs`; RR-10 Bundling marked in `Bundler.cs`; 5 CS0618 flags remain (no safe v17 replacement — all "removal in v18/19", harmless on v17).

> **Session 6:** all 16 Bellissima plugin areas converted to **TypeScript + Vite** (see `TYPESCRIPT_MIGRATION_GUIDE.md` + `SESSION_HANDOFF.md`); **BLOCKER-11 resolved** (boot-crash fix, verified live); live backoffice smoke-test passed.

Items that require external action, design decisions, or significant implementation effort before full production readiness.

> **Session 5 branch review (14-agent deliberation, verdict MAKE-SENSE-WITH-FIXES) — full findings tracker: `REVIEW_FINDINGS.md`.** Four review-found defects were fixed this session (CRITICAL duplicate `Umbraco.BlockList` property converter → deleted; NC migration empty-path JSON key + non-transactional steps; `UrlInfo.AsUrl` arg order in `TryGetRelocatedUrl`), and the NestedContent→BlockList code replacement was completed. The review also surfaced new production blockers — see BLOCKER-10 and BLOCKER-11 below.

---

## RESOLVED

### ~~BLOCKER-01: Contentment~~ — RESOLVED
- **Old state:** No Umbraco 17 release; `Our.Umbraco.Community.Contentment.Core 4.7.0` crashed TypeFinder via `Umbraco.Web.BackOffice v13` IL reference.
- **Resolution:** Package renamed to `Umbraco.Community.Contentment`. Upgraded to **6.1.4** (targets Umbraco 17, net10.0). `.AddContentment()` restored. Contentment API updated: `ContentmentConfigurationField` (in `Umbraco.Cms.Core.PropertyEditors`), `PagedViewModel<T>` (in `Umbraco.Cms.Api.Common.ViewModels.Pagination`), `GetItems()` sync method added.
- **MSBuild strip target** in `Directory.Build.targets` confirmed no longer needed for Contentment; kept as safety net only.

### ~~BLOCKER-03: Konstrukt → Umbraco.UIBuilder~~ — RESOLVED
- **Old state:** `Konstrukt.Startup 1.6.7` renamed to `Umbraco.UIBuilder` in v14+; full API port needed.
- **Resolution:** All Konstrukt → UIBuilder API ports complete:
  - `UIBuilderConfigurator` → uses `UIBuilderConfigBuilder`, `WithSectionConfigBuilder`
  - `UIBuilderComposer` → `builder.AddUIBuilder(cfg => { ... })` with auto-discovery of `IConfigurator`
  - `KonstruktValueMapper` → `ValueMapper` (`Umbraco.UIBuilder.Mapping`)
  - `KonstruktDataViewsBuilder<T>` → `DataViewsBuilder<T>`; `KonstruktDataViewSummary` → `DataViewSummary`
  - `KonstruktEntitySaved/SavingNotification` → `EntitySaved/SavingNotification`
  - Data UIBuilder `ImportsConfigurator` fully ported.

---

## ACTIVE BLOCKERS

### BLOCKER-02: Perplex.ContentBlocks — Pre-release RC

**Status:** Pre-release — v4.0.0-rc.3 in use  
**Package:** `Perplex.ContentBlocks 4.0.0-rc.3`  
**Projects:** `N3O.Umbraco.Blocks.Perplex`, `N3O.Umbraco.Data.PerplexBlocks`

#### Current state
The v4.0.0-rc.3 package has been integrated and the solution compiles. API changes adopted:
- `PerplexBlockDefinition` implements `ElementTypeKey` (returns `Id`) and `BlockNameTemplate` (returns `null`)
- `ContentBlocksModelValue` → `ContentBlocksValue`; namespace `Perplex.ContentBlocks.PropertyEditor.Value`
- `DataType.Configuration` property removed; `CreateDataTypes` method removed from `PerplexBlockTypesService`
- Dead code: `GetOrCreateDataTypeContainer` private method in `PerplexBlockTypesService` is now unreachable — **safe to delete**.

#### Issue
RC releases should not be used in production. A stable v4.x release is needed.

**Note:** Data types that were previously created by code (via `CreateDataTypes`) must now be managed via the Umbraco backoffice or uSync. Communicate this to all site teams.

#### Action required
1. Monitor https://github.com/PerplexDigital/Perplex.ContentBlocks for stable v4 release **(EXTERNAL — upstream; cannot be resolved in code. rc.3 is functional.)**
2. When available: upgrade from rc.3 to stable; verify API compatibility
3. ~~Delete dead `GetOrCreateDataTypeContainer` method from `PerplexBlockTypesService.cs`~~ **DONE (already removed; verified gone 2026-06-15)**
4. Document for site teams: Perplex data types are no longer auto-created; manage via backoffice/uSync

---

### BLOCKER-04: Engage Cockpit Segment Rule Factory (RESOLVED 2026-06-02 — C# side)

**Status:** ✅ C# RESOLVED. `TelethonOnAirCockpitSegmentRuleFactory` implemented and BOTH DI registrations re-enabled in `PlatformsMarketingComposer`. v17.2.2 API verified by MetadataLoadContext reflection: `ICockpitSegmentRuleFactory`/`CockpitSegmentRule` in `Umbraco.Engage.Web.Cockpit.Segments`; `ISegmentRule`/`ISegmentRuleFactory`/`BaseSegmentRule` moved into `Umbraco.Engage.Infrastructure.Personalization.Segments.Rules` (the `.Rules` sub-namespace — the old `...Segments` using would NOT compile). Signature: `bool TryCreate(ISegmentRule, bool, out CockpitSegmentRule?)`. Build verified 0 errors. **The 3 AngularJS `segment-rule-telethon-on-air*.js` editor files remain blocked** (need Engage v17 client-side segment-rule UI API). *(Original notes below for reference.)*  
**Status (original):** Engage package upgraded; namespaces confirmed valid; Cockpit factory implementation missing  
**Package:** `Umbraco.Engage 17.2.2` (installed)  
**Projects:** `N3O.Umbraco.Cloud.Platforms.Marketing`

#### Current state (what IS working)
The Engage package upgrade to v17.2.2 is complete. DLL inspection confirms all three namespaces used by the codebase are **unchanged** in v17.2.2:
- `Umbraco.Engage.Infrastructure.Personalization.Segments` → `ISegmentRepository`, `Segment` ✅
- `Umbraco.Engage.Infrastructure.Personalization.Segments.Rules` → `ISegmentRuleFactory`, `ISegmentRule`, `BaseSegmentRule`, `SegmentRuleValidationMode` ✅
- `Umbraco.Engage.Infrastructure.Personalization.PersonalizationProfile` → `IPersonalizationProfile` ✅

`UmbracoEngageSegmentsDataSource.cs` (Marketing project) builds and runs correctly.

#### What is broken
`PlatformsMarketingComposer.cs` has both factory registrations commented out:
```csharp
// TODO (BLOCKER-04): Umbraco.Engage namespaces changed in v17
// builder.WithCollectionBuilder<SegmentRuleCollectionBuilder>().Add<TelethonOnAirSegmentRuleFactory>();
// builder.WithCollectionBuilder<CockpitSegmentRuleCollectionBuilder>().Add<TelethonOnAirCockpitSegmentRuleFactory>();
```

`TelethonOnAirCockpitSegmentRuleFactory.cs` is **an empty file** — the class body needs implementing.

The Cockpit factory interface in v17.2.2 is at: `Umbraco.Engage.Web.Cockpit.Segments.ICockpitSegmentRuleFactory`

#### Action required
1. Read `ICockpitSegmentRuleFactory` interface definition (in `Umbraco.Engage.Web.Cockpit.Segments` namespace)
2. Implement `TelethonOnAirCockpitSegmentRuleFactory` — specifically update `TryCreate` to the v17 signature (nullable out parameter)
3. Re-enable both `Add<>()` registrations in `PlatformsMarketingComposer.cs`
4. Rewrite the 3 AngularJS segment rule editor JS files (`segment-rule-telethon-on-air*.js`) as Bellissima web components

#### AngularJS UI still blocked
The three JS files in `App_Plugins/telethon-on-air-rule/` are AngularJS and will not run in Umbraco 17 backoffice. These depend on `umsSegmentRuleRepository` (an Engage Angular service). Check what the Engage v17.2.2 client-side API provides for custom segment rule UIs. *(Note: when ported, the source for this plugin would live under the per-plugin `<Project>/frontend/<app>/` Turbo workspace convention like the other migrated plugins; the blocker itself remains open.)*

---

### BLOCKER-05: uSync Publisher — `SyncContentHandler`

**Status:** ✅ RESOLVED (2026-06-02) — reimplemented against the real uSync.Publisher v17 API discovered by reflection (no public docs exist; the installed assemblies were the authoritative source).

`IPublisherStateService` (uSync.Publisher.Client) is gone — that client assembly now exports only `PublisherManifest`. The push pipeline was rebuilt on `Jumoo.Processing` (the real defining assembly is `jumoo.processing.core.dll`; the `Jumoo.Processing.dll` the old TODO named is an empty placeholder). The new handler injects **`uSync.Publisher.Strategies.Processor.PublisherProcessor`** (which internally wraps `Jumoo.Processing.Core.Pipelines.IPipelineService` + `IUserService`) and calls `Process(PublisherActionRequest, PublisherProcessingOptions)` → `SyncPublishResponse`. It builds the same Document-UDI `SyncItem` (`ChangeType.Create` + `DependencyFlags.PublishedDependencies`), pushes in `PublishMode.Push` to `ServerAlias`, and throws on `!result.Success` — preserving the original contract. The old `HasProcess` idempotency guard was dropped (the dispatcher `SyncOnPublish` mints a fresh `RequestId` per publish and enqueues fire-and-forget, so it guarded nothing). `PublisherProcessor` registered via `AddTransient` in `SyncExtensionsComposer`. Build 0 errors (solution + DemoSite.Web); app boots clean; all API verified by `MetadataLoadContext` reflection against the installed 17.3.x DLLs.

⚠️ **Runtime caveat (not statically verifiable):** whether `PublisherProcessor.Process` drives the multi-step push pipeline to completion in a single call could not be byte-confirmed without decompiling the method body. It returns `SyncPublishResponse` directly (strongly implying it runs to completion), and it executes only inside the N3O background-job scheduler, so a partial completion would surface as a job failure, not a publish/editing outage. **Needs a true end-to-end test with a configured remote uSync server + a `[SyncOnPublish]` content type** (not present on the demo site). Documented fallback if needed: drive `IPipelineService` directly (CreatePipeline → UpdateOptions → loop Process until `PipelineStatus.Completed/Failed`).

**Status (original):** Blocked on Jumoo — v17 API not publicly documented  
**Package:** uSync.Publisher (separate from `uSync.Complete 17.3.6`)  
**File:** `src/Sync/N3O.Umbraco.Sync.Extensions/Handlers/SyncContentHandler.cs`

#### What is broken
`IPublisherStateService` was removed in uSync.Publisher v17. The `SyncContentHandler.Handle()` method currently throws `NotSupportedException` at runtime. Any attempt to sync content via uSync Publisher will crash.

```csharp
public void Handle(ContentSavedNotification notification) {
    throw new NotSupportedException("IPublisherStateService removed in uSync.Publisher v17. Needs reimplementation against Jumoo.Processing v17 API.");
}
```

#### Action required
1. Monitor https://jumoo.co.uk and https://github.com/PerplexDigital for uSync Publisher v17 documentation
2. When available: reimplement `SyncContentHandler` against the `Jumoo.Processing` v17 API
3. Until then: consider feature-flagging the handler so it is only registered in environments where Publisher is not needed

---

### BLOCKER-06: Nested Content Database Migration

**Status (session 10, 2026-06-02):** The Nested Content → Block List migration will be done by a separate **CLI app**. The on-startup `PackageMigrationPlan` (`N3ONestedContentMigrationPlan` + `NestedContentToBlockListMigration`) is **commented out** and **no longer runs at startup** (commit `0d6c2aede`); the per-site data migration is now an explicit step run by the CLI app. The per-site checklist below still applies.

**Earlier (session 4):** the migration was registered as a startup `PackageMigrationPlan`; smoke-testing fixed its SQL for the v17 schema (below).

**Smoke-test finding (2026-06-02):** registering it surfaced that `NestedContentToBlockListMigration.cs` was NOT written for the v17 DB schema and crashed every startup (`SqlException: Invalid column name 'id'`). Fixed against the live v17 schema:
- `umbracoDataType` has `nodeId` (PK) + `propertyEditorUiAlias` — NOT `id`/`pk`. Query/UPDATE now use `nodeId`, and the UPDATE also sets `propertyEditorUiAlias = 'Umb.PropertyEditorUi.BlockList'` (verified value in 17.3.5 static assets) so the migrated Block List data type resolves its editor.
- There is no `umbracoContentType` table in v17 — content types are `cmsContentType`, and the GUID key is `umbracoNode.uniqueId`. The alias→key lookup now joins `cmsContentType ct INNER JOIN umbracoNode n ON n.id = ct.nodeId`.
- `cmsPropertyType` (id, dataTypeId) and `umbracoPropertyData` (id, propertyTypeId, textValue) were already correct — unchanged.
- Verified: app boots clean, plan completes and advances state (`database contains 2026-NestedContent-v1`).

⚠️ STILL UNVALIDATED: the NC-JSON → Block List **value transform shape** (`TransformNestedContentToBlockList`) is unverified against the v17 Block List value format — the demo DB has 0 Nested Content data types so the transform path never executed. Must dry-run on a real legacy DB copy with NC content before any live run. The per-site checklist below (backup, dry-run on copy, verify rendered output) STILL applies.  
**Status (original):** Migration code written; not registered; not run on any site  
**File:** `src/N3O.Umbraco.Extensions/Migrations/NestedContentToBlockListMigration.cs`

#### What is needed
For every existing client site, Nested Content property values must be converted to Block List JSON before the site can render content correctly on v17.

#### Registration (not yet done)
```csharp
public class N3ONestedContentMigrationPlan : PackageMigrationPlan {
    public N3ONestedContentMigrationPlan() : base("N3O.Umbraco.NestedToBlockList") { }
    protected override void DefinePlan() {
        To<NestedContentToBlockListMigration>("2026-NestedContent-v1");
    }
}
```
This plan needs to be registered in a Composer via `builder.PackageMigrationPlans().Add<N3ONestedContentMigrationPlan>()`.

#### Pre-migration checklist per site
- [ ] Back up the database
- [ ] Verify all Nested Content data types have a corresponding Block List data type configured with the same element types
- [ ] Run migration on a database copy first
- [ ] Verify rendered output before and after in staging
- [ ] Regenerate uSync XML after migration (run `uSync export`, commit the result)

---

### BLOCKER-07: Bellissima Frontend — All AngularJS Plugins

**Status:** DONE + modernized to TypeScript (2026-06-02). 15 of 16 plugin areas migrated to `umbraco-package.json` + Lit (only `telethon-on-air-rule` blocked, depends on BLOCKER-04). **Session 6:** all 16 areas (13 build units) further converted from plain-JS to **TypeScript + Vite** — per-project `ClientApp/` (`package.json`/`tsconfig.json`/`vite.config.ts`/`src/*.ts`) with an MSBuild `BuildClientApp` target running `npm ci`/`npm run build`; `@umbraco/*` kept external, code + npm libs bundled (Handsontable, cropperjs, @editorjs/* now npm deps; formstone/jQuery kept vendored + flagged). Full solution build 0 errors. *(2026-06-24: the per-project `ClientApp/`+`BuildClientApp` approach was later replaced by the `src/frontend/` Turbo workspace + shared `Directory.Build.targets`; see AGENTS.md.)* **Live smoke-test passed:** app boots, WelcomeDashboard + Scheduler render, 6 referenced property-editor UIs register & are selectable, zero N3O console errors. Recipe: `TYPESCRIPT_MIGRATION_GUIDE.md`; AngularJS→Lit history: `BELLISSIMA_MIGRATION_LOG.md`/`BELLISSIMA_MIGRATION_GUIDE.md`. **Still pending:** live property-editor render *inside a content node* (needs doctype/content fixtures), and the 5 plugins not referenced by DemoSite.Web (EditorJs, Cells, Blocks.Preview, Cloud.Platforms.Preview, Blazor.BackOffice).

**Critical fix discovered during testing:** each custom `propertyEditorUi.alias` (and `propertyEditorSchemaAlias`) must equal the backend `[DataEditor]` alias (NOT a new `N3O.PropertyEditorUi.*`), else existing data types show "Property Editor UI not found". Applied to all custom editors (TextResourceEditor's backend alias is `N3O.Umbraco.TemplateTextEditor`).

**Original impact:** All custom property editors, content apps, and dashboards were invisible or non-functional in the Umbraco 17 backoffice.

#### Summary
Umbraco 14+ (Bellissima) replaced the entire AngularJS backoffice with Lit-based web components. None of the N3O custom property editors, content apps, or dashboards currently work.

#### Plugin areas and effort

| Plugin | Extension type | Complexity | AngularJS APIs used |
|---|---|---|---|
| Block Preview | `blockEditorCustomView` | High | `$scope`, `$sce`, `$timeout`, `editorState`, `umbRequestHelper` |
| Data Export | `contentApp` | Medium | `$scope`, `editorState`, `assetsService` |
| Data Import | `contentApp` | Medium | `$scope`, `editorState`, `assetsService` |
| Data ImportDataEditor | `propertyEditorUi` | Medium | `$scope`, `assetsService` |
| Data ImportNoticesViewer | `propertyEditorUi` | Low | Simple display |
| Platforms Preview | `contentApp` | Medium | `$scope`, `editorState` |
| Cells (Handsontable) | `propertyEditorUi` | High | `$scope`, `assetsService`, Handsontable lib |
| Cropper | `propertyEditorUi` | High | `$scope`, `assetsService`, `$timeout`, Formstone |
| EditorJs | `propertyEditorUi` | High | `$scope`, `assetsService`, EditorJs bundle |
| SerpEditor | `propertyEditorUi` | Medium | `$scope`, `assetsService`, `editorState` |
| TextResourceEditor | `propertyEditorUi` | Low | Simple text editor |
| Uploader | `propertyEditorUi`/`dashboard` | Medium | `$scope`, `assetsService`, Formstone |
| WelcomeDashboard | `dashboard` | Low | Empty controller — almost no logic |
| Scheduler Dashboard | `dashboard` | Low | HTML iframe wrapping Hangfire UI |
| Blazor BackOffice | `script` | Low | Non-AngularJS; just needs manifest format update |
| TelethonOnAir segment rule | Engage extension | High | Engage-specific Angular services — also blocked by BLOCKER-04 |

#### Approach
For each plugin:
1. Create a Lit `customElement` class (TypeScript recommended)
2. Replace `$scope` state with Lit reactive properties (`@property()`, `@state()`)
3. Replace `assetsService.loadJs/Css` with ES module `import` or bundled dependencies
4. Replace `editorState` access with Umbraco's `UmbDocumentWorkspaceContext`
5. Register in `umbraco-package.json` with the correct extension type alias
6. Delete the old `.js` controller, `.html` view, and `package.manifest`

#### Resources
- Umbraco backoffice package docs: https://docs.umbraco.com/umbraco-cms/extending/backoffice-setup
- Umbraco UI Library (Lit components): https://uui.umbraco.com
- Extension type reference: https://docs.umbraco.com/umbraco-cms/extending/backoffice-setup/extension-types

---

### BLOCKER-08: Umbraco Forms Subscription License

**Status:** Procurement action required  
**Package:** `Umbraco.Forms 17.0.x`

From Umbraco v17, the perpetual license model for Umbraco Forms is no longer valid. A subscription license is required per site.

#### Action required
1. Contact Umbraco sales: https://umbraco.com/products/umbraco-forms/
2. Obtain a subscription license per client site using Forms
3. Configure the license key in `appsettings.json` before deploying v17

---

### BLOCKER-09: Assembly Version Tags (`13.0.0`)

**Status:** ✅ DONE as placeholder (2026-06-02) — bulk find/replace set `<Version>`/`<AssemblyVersion>`/`<FileVersion>` from `13.0.0` to `17.0.0` across 114 csproj (6 had no tag). Still must re-stamp the real CalVer (`YYYY.M.D.Build`) before any NuGet publish.  
**Scope:** All 120 `.csproj` files

Every project has `<Version>13.0.0</Version>`, `<AssemblyVersion>13.0.0</AssemblyVersion>`, `<FileVersion>13.0.0</FileVersion>`. These are CalVer-style and need updating to `17.x.x.x` before publishing any packages.

#### Action required
```powershell
# Bulk update all csproj version tags
Get-ChildItem "D:\AI Migration Test\N3O.Umbraco\src" -Recurse -Filter "*.csproj" |
    ForEach-Object {
        $content = Get-Content $_.FullName -Raw
        $updated = $content -replace '<Version>13\.0\.0</Version>', '<Version>17.0.0</Version>'
        $updated = $updated -replace '<AssemblyVersion>13\.0\.0</AssemblyVersion>', '<AssemblyVersion>17.0.0</AssemblyVersion>'
        $updated = $updated -replace '<FileVersion>13\.0\.0</FileVersion>', '<FileVersion>17.0.0</FileVersion>'
        if ($updated -ne $content) { Set-Content $_.FullName $updated }
    }
```
Use CalVer format `YYYY.M.D.Build` for the actual release version.

---

### BLOCKER-10: Access-control regressions (Bellissima migration) — RESOLVED (#1/#2 session 8; #3 + content-app gating 2026-06-11)

**Status:** ✅ All three resolved. #1 (Hangfire) + #2 (Export/Import API gating) server-side in session 8; #3 (Platforms-Preview content-type gating) restored 2026-06-11 via the shared `N3O.Condition.WorkspaceVisibility` condition (Preview compile-verified only). The same condition also restored client-side per-node/per-user content-app gating for Export/Import (REVIEW_FINDINGS BLOCKER-5, verified end-to-end).

Three server-side gating losses from the AngularJS→Bellissima move:
1. ✅ **RESOLVED** — Hangfire dashboard. `SchedulerComposer.AddAuthorizedUmbracoDashboard` now requires Umbraco's built-in **`AuthorizationPolicies.SectionAccessSettings`** alongside the back-office-scheme `HangfireDashboard` policy (`.RequireAuthorization(HangfireDashboard, AuthorizationPolicies.SectionAccessSettings)`). `SectionRequirement` was removed in v17; `SectionAccessSettings` is its maintained equivalent (Settings-section ⇒ admin-equivalent). Only Settings-section users can reach the dashboard again. Build 0 errors.
2. ✅ **RESOLVED (server-side, stronger than v13)** — Export/Import. New reusable `[RequireUserGroup(...)]` authorization filter (`src/Data/N3O.Umbraco.Data/Security/RequireUserGroupAttribute.cs`) applied to `ExportsController` (`exportUsers`) and `ImportsController` (`importUsers`); admin group always allowed. Enforced at the **API boundary** (resolves `IBackOfficeSecurityAccessor.CurrentUser.Groups`), so it holds even though the back-office tab still renders. `DataConstants.SecurityGroups.*.Alias/.Name` changed `static readonly`→`const` (needed for the attribute arg). In v13 this gating was UI-only; it is now enforced server-side.
3. ✅ **RESOLVED (2026-06-11)** — the Platforms-Preview `workspaceView` is now gated by a **custom shared condition `N3O.Condition.WorkspaceVisibility`** (in `N3O.Umbraco.ReactRuntime`, registered once) that reads the document key and calls an authed `GET {endpoint}/{key}` → `{permitted}`; Preview's endpoint is `PlatformsPreviewController` (N3O.Umbraco.Cloud.Platforms), which permits only content types composing `PlatformsConstants.Offerings.CompositionAlias`. The same condition also restored the Export/Import content-app gating (BLOCKER-5 in `REVIEW_FINDINGS.md`). Builds 0 errors; **Preview is compile-verified only** (the test site doesn't reference Cloud.Platforms — runtime verification pending). See `MIGRATION_AUDIT_2026-06-10.md` "Resolved 2026-06-11".

**Remaining follow-ups (lower priority):** ~~(a) the custom content-type condition for Preview (#3);~~ **DONE 2026-06-11.** ~~(b) optionally hide the Export/Import tabs for non-authorized users client-side via a custom user-group condition;~~ **DONE 2026-06-11** — the shared `N3O.Condition.WorkspaceVisibility` condition now gates the Export/Import tabs client-side (endpoints `ExportVisibilityController`/`ImportVisibilityController` check Admin OR Export/ImportUsers + the `IExport/ImportContentFilter`), in addition to the existing server-side `[RequireUserGroup]` API gate. Still optional: (c) re-check `IExport/ImportContentFilter.AllowExports/AllowImports` server-side inside the export/import commands themselves (per-content refinement — in v13 it only hid the tab). **Pending:** runtime verification of the Preview gating (compile-verified only; test site doesn't reference Cloud.Platforms).

---

### BLOCKER-11: `DataComposer.EnsureDataTypeExists` lookup by alias — RESOLVED (2026-06-02, session 6)

**Status:** ✅ RESOLVED. The existence check now looks up by the deterministic Key (the same key it sets on create) via the async API: `_dataTypeService.GetAsync(UmbracoId.Generate(IdScope.DataType, dataEditor.Alias)).GetAwaiter().GetResult()` (blocking is safe — runs once at startup, no sync context, like the existing sync `Save()`). `GetDataType(Guid)` does NOT exist as a sync overload in v17 (only `GetDataType(string)`, which matches by Name); `GetAsync(Guid)` is the v17 key-based lookup. Verified live: app boots clean, both N3O data types (`N3O Import Data Editor`, `N3O Import Notices Viewer`) exist **once each — no duplicates** — across restarts.

**How it surfaced:** restarting the app against a DB already containing the data types (created on a prior boot) crashed every startup at `DataComposer.cs:155` — `Cannot insert duplicate key row in 'umbracoNode' (IX_umbracoNode_UniqueId)` for the deterministic key `9cba68d8-…` (ImportNoticesViewer). The first-ever boot succeeded (empty DB → insert), every subsequent boot crashed (lookup-by-Name missed the row → re-insert → unique-index violation).

**Original (now fixed):** `EnsureDataTypeExists` called `IDataTypeService.GetDataType(dataEditor.Alias)` and set `dataType.Name = Alias`. `GetDataType(string)` matches by **Name**, so the existing data type row was never found → duplicate created on every startup.

⚠️ **Follow-up (separate, minor):** the in-code data types persist `EditorAlias` but **not** `EditorUiAlias`, so the v17 data-type editor screen shows an empty "Select a property editor" picker (the editors themselves ARE registered & selectable — confirmed live). Consider setting `dataType.EditorUiAlias = dataEditor.Alias` in `EnsureDataTypeExists`. Not a TS-migration regression.

---

## Summary Table

| # | Blocker | Status | Urgency |
|---|---|---|---|
| ~~01~~ | ~~Contentment~~ | **RESOLVED** (6.1.4) | — |
| 02 | Perplex.ContentBlocks RC | Functional RC; waiting for stable | Medium |
| ~~03~~ | ~~Konstrukt → UIBuilder~~ | **RESOLVED** (port complete) | — |
| ~~04~~ | ~~Engage Cockpit factory missing~~ | **RESOLVED** (2026-06-02) — `TelethonOnAirCockpitSegmentRuleFactory` implemented + registrations re-enabled; API verified by reflection. AngularJS telethon UI still blocked. | — |
| ~~05~~ | ~~uSync Publisher v17~~ | **RESOLVED** (2026-06-02) — reimplemented on uSync.Publisher v17 `PublisherProcessor`/`Jumoo.Processing` (API found by reflection; no public docs). E2E test w/ remote server still advised. | — |
| 06 | Nested Content DB migration | **To be done by a separate CLI app** (session 10) — on-startup plan disabled (`0d6c2aede`); per-site data migration is now an explicit offline step. | Critical |
| 07 | Bellissima frontend (all AngularJS) | **Done + TypeScript** (15/16 migrated then converted to TS+Vite; telethon blocked on 04; in-content live-render fixtures pending) | Low |
| 08 | Forms subscription license | Procurement | High |
| ~~09~~ | ~~Assembly version `13.0.0`~~ | **DONE (placeholder)** (2026-06-02) — bulk-set to `17.0.0` across 114 csproj; re-stamp CalVer at publish | Low |
| ~~10~~ | Access-control regressions (Hangfire / Export / Import / Preview) | **RESOLVED** — Hangfire→`SectionAccessSettings` ✅ (s8); Export/Import→server-side `[RequireUserGroup]` ✅ (s8) + client-side content-app gating ✅ (2026-06-11, verified); Preview content-type condition ✅ (2026-06-11, compile-verified) — all via shared `N3O.Condition.WorkspaceVisibility` | — |
| ~~11~~ | ~~`DataComposer.EnsureDataTypeExists` lookup-by-alias~~ | **RESOLVED** (2026-06-02, session 6) — lookup by deterministic Key via `GetAsync`; verified live (no dup data types, app boots). Minor follow-up: also set `EditorUiAlias`. | — |
| — | Bundling throws at render (RR-10) | **RESOLVED (2026-06-15, session 18)** — orphaned `N3O.Umbraco.Bundling` deleted (sln + dir + 2 inert `Layout.cshtml` literals). Smidge has no v17 runtime replacement; build-time Vite is the v17 pattern (already used repo-wide). Build 0 errors | — |
| — | Cropper/Uploader → native pickers | **DONE framework-side (2026-06-15, session 18)** — 6 plugin projects deleted; consumers on native `MediaWithCrops`; DemoSite data types → `Umbraco.MediaPicker3`; build 0 errors. Per-site data migration = offline CLI (`CROPPER_UPLOADER_NATIVE_MIGRATION.md`) | — |

*Session-5 fixed defects (build 0 errors, boot clean) and the full review finding list live in `REVIEW_FINDINGS.md`.*
