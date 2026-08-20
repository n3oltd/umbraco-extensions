# Bellissima Frontend Migration Log (BLOCKER-07)

*Task started: 2026-06-01 — AngularJS → Bellissima (Lit) migration of all App_Plugins*

Each step appended below. Newest entries at the bottom of each section.

---

## Step 0 — Scoping (complete)

Surveyed the codebase. Reference implementation studied: `DynamicListViews` (already migrated to Bellissima) in:
- `D:\AI Migration Test\Umbraco17Test\Umbraco17Test-1\wwwroot\App_Plugins\N3O.Umbraco.DynamicListViews\` (user's example)
- `src\N3O.Umbraco.Cms\App_Plugins\N3O.Umbraco.DynamicListViews\` (in-repo copy)

### Delivery model
Each plugin lives in a `*.StaticAssets` project under `App_Plugins/<name>/`. A `build/*.targets` file
copies `App_Plugins/<name>/**\*.*` into the consuming site (e.g. DemoSite.Web) at build time
(`BeforeTargets="Build"`). So migrated `umbraco-package.json` + Lit `.js` files placed in the StaticAssets
source folder propagate automatically. Backend API endpoints (`/umbraco/backoffice/api/...`,
`/umbraco/api/...`) are UNCHANGED — only the frontend is rewritten.

### Plugins to migrate (16 areas)

| # | Plugin | StaticAssets source folder | Extension type | Complexity |
|---|---|---|---|---|
| 1 | Blazor.BackOffice | `Blazor/N3O.Umbraco.Blazor.BackOffice/App_Plugins/N3O.Umbraco.Blazor.BackOffice` | bundle/script | Low |
| 2 | Blocks.Preview | `Blocks/N3O.Umbraco.Blocks.StaticAssets/App_Plugins/N3O.Umbraco.Blocks.Preview` | blockEditorCustomView | High |
| 3 | telethon-on-air-rule | `Cloud/N3O.Umbraco.Cloud.Platforms.Marketing.StaticAssets/App_Plugins/telethon-on-air-rule` | Engage segment rule | High (BLOCKED by BLOCKER-04) |
| 4 | Cloud.Platforms.Preview | `Cloud/N3O.Umbraco.Cloud.Platforms.StaticAssets/App_Plugins/N3O.Umbraco.Cloud.Platforms.Preview` | workspaceView | Medium |
| 5 | Data.Export | `Data/N3O.Umbraco.Data.StaticAssets/App_Plugins/N3O.Umbraco.Data.Export` | workspaceView | Medium |
| 6 | Data.Import | `Data/N3O.Umbraco.Data.StaticAssets/App_Plugins/N3O.Umbraco.Data.Import` | workspaceView | Medium |
| 7 | Data.ImportDataEditor | `Data/N3O.Umbraco.Data.StaticAssets/App_Plugins/N3O.Umbraco.Data.ImportDataEditor` | propertyEditorUi | Medium |
| 8 | Data.ImportNoticesViewer | `Data/N3O.Umbraco.Data.StaticAssets/App_Plugins/N3O.Umbraco.Data.ImportNoticesViewer` | propertyEditorUi | Low |
| 9 | Cells | `Plugins/Cells/N3O.Umbraco.Cells.StaticAssets/App_Plugins/N3O.Umbraco.Cells` | propertyEditorUi | High (Handsontable) |
| 10 | Cropper | `Plugins/Cropper/N3O.Umbraco.Cropper.StaticAssets/App_Plugins/N3O.Umbraco.Cropper` | propertyEditorUi | High (Formstone/cropperjs) |
| 11 | EditorJs | `Plugins/EditorJs/N3O.Umbraco.EditorJs.StaticAssets/App_Plugins/N3O.Umbraco.EditorJs` | propertyEditorUi | High (editorjs bundle) |
| 12 | SerpEditor | `Plugins/SerpEditor/N3O.Umbraco.SerpEditor.StaticAssets/App_Plugins/N3O.Umbraco.SerpEditor` | propertyEditorUi | Medium |
| 13 | TextResourceEditor | `Plugins/TextResourceEditor/N3O.Umbraco.TextResourceEditor.StaticAssets/App_Plugins/N3O.Umbraco.TextResourceEditor` | propertyEditorUi | Low |
| 14 | Uploader | `Plugins/Uploader/N3O.Umbraco.Uploader.StaticAssets/App_Plugins/N3O.Umbraco.Uploader` | propertyEditorUi | Medium (Formstone) |
| 15 | WelcomeDashboard | `Plugins/WelcomeDashboard/N3O.Umbraco.WelcomeDashboard.StaticAssets/App_Plugins/N3O.Umbraco.WelcomeDashboard` | dashboard | Low |
| 16 | Scheduler | `Scheduler/N3O.Umbraco.Scheduler.StaticAssets/App_Plugins/N3O.Umbraco.Scheduler` | dashboard | Low (Hangfire iframe) |

Shared guide written to `BELLISSIMA_MIGRATION_GUIDE.md`. Subagents fan out next.

---

## Step 1 — Parallel migration fan-out (in progress)

Launched workflow `bellissima-plugin-migration` (run `wf_7d47214b-631`) — 16 subagents, one per plugin.
Each agent: reads guide + reference + its own source, writes `umbraco-package.json` + Lit component(s),
deletes the AngularJS `package.manifest`/`*.Controller.js`/AngularJS `*.html`, keeps third-party libs,
returns a structured report. Awaiting completion. Per-plugin results logged below when done.

### Results (16 agents, ~9min, 899k subagent tokens) — 15 done, 1 blocked

| Plugin | Status | Ext type | Created | Key risk to verify at runtime |
|---|---|---|---|---|
| Blazor.BackOffice | done | bundle | umbraco-package.json | JS relies on global jQuery `$`; v17 backoffice may not provide it |
| Blocks.Preview | done | blockEditorCustomView | umbraco-package.json, block-preview.js | uses `@umbraco-cms/backoffice/block` (UMB_BLOCK_ENTRY/MANAGER_CONTEXT); BlockGridValue body shape medium-confidence |
| telethon-on-air-rule | **BLOCKED** | bundle (placeholder) | umbraco-package.json | Engage v17 client segment-rule API unknown; package.manifest→.bak; backend factory still empty (BLOCKER-04) |
| Cloud.Platforms.Preview | done | workspaceView | umbraco-package.json, platforms-preview.js | `UMB_DOCUMENT_WORKSPACE_CONTEXT.getData()` model shape (values/contentType.alias/parent.unique) unverified |
| Data.Export | done | workspaceView | umbraco-package.json, data-export.js | endpoints reused verbatim; doc key via context.unique |
| Data.Import | done | workspaceView | umbraco-package.json, data-import.js | "View Import Queue" link uses legacy `/umbraco#/content?dashboard=imports` hash route |
| Data.ImportDataEditor | done | propertyEditorUi | umbraco-package.json, import-data-editor.js | schema alias Umbraco.Plain.Json |
| Data.ImportNoticesViewer | done | propertyEditorUi | umbraco-package.json, import-notices-viewer.js | display-only; schema alias = backend `N3O.Umbraco.Data.ImportNoticesViewer` |
| Cells | done | propertyEditorUi | umbraco-package.json, n3o-cells.js | Handsontable imported as ES side-effect; CSS via shadow-root `<link>`; schema alias `N3O.Umbraco.Cells` |
| Cropper | done | propertyEditorUi ×2 | umbraco-package.json, cropper.js, crop-definitions.js | formstone upload needs global jQuery (absent) → upload may no-op; 2nd UI needs backend config field to point at its alias |
| EditorJs | done | propertyEditorUi | umbraco-package.json, editor-js.js | modal imports (UMB_MODAL_MANAGER_CONTEXT, UMB_MEDIA_PICKER_MODAL, UMB_LINK_PICKER_MODAL) + modal result shapes unverified; shadow-DOM popovers |
| SerpEditor | done | propertyEditorUi | umbraco-package.json, serp-editor.js | per-culture URL lookup dropped (host-origin fallback only); schema alias `N3O.Umbraco.SerpEditor` |
| TextResourceEditor | done | propertyEditorUi | umbraco-package.json, text-resource-editor.js | orphan .css left in folder (inlined); schema Umbraco.Plain.Json |
| Uploader | done | propertyEditorUi | umbraco-package.json, uploader.js | loads jQuery from CDN (code.jquery.com) for formstone; CSP/offline concern; schema alias `N3O.Umbraco.Uploader` |
| WelcomeDashboard | done | dashboard | umbraco-package.json, welcome-dashboard.js | Content section; static panel |
| Scheduler | done | dashboard | umbraco-package.json, scheduler-dashboard.js | Settings section; iframe `/umbraco/backoffice/hangfire/` |

**Cross-cutting risks to validate in build/test:**
- Several `propertyEditorUi` set `meta.propertyEditorSchemaAlias` to the existing backend DataEditor alias
  (Cells, Uploader, SerpEditor, ImportNoticesViewer, Cropper) rather than `Umbraco.Plain.*`. Must confirm v17
  accepts this (the schema alias must resolve to a registered property-editor schema).
- jQuery dependency (Blazor loader, Cropper+Uploader formstone) — backoffice no longer ships jQuery.
- `@umbraco-cms/backoffice/block` and modal/media/link-picker import subpaths — confirm they resolve at runtime.

---

## Step 2 — Build (complete)

`dotnet build -c Debug` on `DemoSite/DemoSite.Web` → **0 Errors**, 300 warnings (all pre-existing NuGet
advisory/version-skew warnings, none from this migration). Verified only frontend assets changed (the 4 C#
files modified are pre-existing from the prior session, not this work).

Build side-effects handled:
- The `.targets` copied each migrated `App_Plugins/<name>/**` into `DemoSite/DemoSite.Web/App_Plugins/`
  (content root) but left the OLD `package.manifest` in the destination (targets don't delete). Deleted 9 stale
  `package.manifest` copies from the DemoSite output (gitignored build artefacts).
- Only 11 of 16 plugins are project-referenced by DemoSite.Web (Data ×4, Cropper, SerpEditor,
  TextResourceEditor, Uploader, WelcomeDashboard, Scheduler). The other 5 (Blazor.BackOffice, Blocks.Preview,
  Cells, EditorJs, Cloud.Platforms.Preview, telethon-on-air-rule) are NOT referenced → not copied to DemoSite,
  so they can't be runtime-tested in this site without adding references.

## Step 3 — Static validation (complete, all green)

- **JS syntax** (`node --check`) on all 17 migrated component files → all OK.
- **JSON validity** on all 16 `umbraco-package.json` → all OK.

## Step 4 — Runtime smoke test (app booted)

Run: `ASPNETCORE_ENVIRONMENT=Development dotnet run --no-build -c Debug` (PowerShell). App URLs:
`http://localhost:6000` / `https://localhost:6001`. (Note: Chrome blocks :6000 as an unsafe port → use :6001.)

- App boots clean; `serverStatus: "Run"` (DB already installed — SQL Server `DemoSite.Web`).
- **No manifest/package parse errors** in the boot log (Umbraco logs invalid `umbraco-package.json`; none seen).
- **Asset serving** — all migrated assets return **HTTP 200** with correct content-type from content-root
  `/App_Plugins/...` (confirmed serp-editor.js, text-resource-editor.js, data-import.js, welcome-dashboard.js,
  scheduler-dashboard.js, uploader.js, cropper.js, and each `umbraco-package.json`). **Discovery/serving location
  = content-root `App_Plugins` (confirmed).**
- **Backoffice login page** (`https://localhost:6001/umbraco`) renders correctly in Chrome (DevTools MCP) with
  **zero console errors/warnings** → Bellissima shell + entry-point bundles load fine.

### BLOCKER for deeper UI testing: backoffice credentials
The DB is fully installed (status `Run`) so the backoffice requires login. There are **no unattended-install
credentials** in config, **no Umbraco MCP API client** configured for the DemoSite (token endpoint returns 400),
and self-serving a login would mean modifying the user's database. Authenticated per-plugin testing (extension
registration, component render, dashboards visible, property editors functional) is **paused pending credentials**.

What's been proven without auth: builds, valid JS/JSON, clean boot with no manifest errors, all assets served,
backoffice shell loads error-free. What still needs a login: that each of the 16 extensions actually registers
and renders in the running backoffice, plus the runtime risks flagged in Step 1 (jQuery deps, block/modal
import paths, workspace-context data shapes).

## Step 5 — Authenticated backoffice testing (credentials provided)

Logged in via Chrome DevTools MCP (`https://localhost:6001/umbraco`). Backoffice booted with **only one
console message** — a generic Umbraco core deprecation warning (`umb-entity-actions-bundle`), **none from N3O
plugins**.

### Confirmed WORKING (rendered live, no errors)
- **WelcomeDashboard** (dashboard, Content section) — "Welcome" tab renders the Help & Support panel + Support
  Centre link. ✓ Screenshot captured.
- **Scheduler** (dashboard, Settings section) — "Scheduler" tab renders the full Hangfire Dashboard iframe
  (Overview, job counts, realtime graph). ✓ Screenshot captured.
- **Data.ImportDataEditor** (propertyEditorUi) — data type resolves to "N3O Import Data Editor". ✓ (after fix below)
- **Data.ImportNoticesViewer** (propertyEditorUi) — data type resolves to "N3O Import Notices Viewer". ✓ (after fix below)

### BUG FOUND + FIXED — property editor UI alias mismatch (critical, systematic)
Opening the `N3O.Umbraco.Data.ImportDataEditor` data type showed **"Property Editor UI not found"**. Root cause:
the migration agents registered each custom `propertyEditorUi` under a NEW alias (`N3O.PropertyEditorUi.<X>`),
but existing data types (and all consuming sites) reference the **original v13 property-editor alias** as their
stored `editorUiAlias`. In Umbraco 13 the property-editor alias and data-type editor alias were one string; in
v17 the data type stores `editorUiAlias` separately and it must match a registered `propertyEditorUi` alias.

**Fix applied** — set each custom property editor's `propertyEditorUi.alias` (and `meta.propertyEditorSchemaAlias`)
to its **backend `[DataEditor]` alias**:

| Plugin | UI alias (now) | schema alias (now) |
|---|---|---|
| Data.ImportDataEditor | `N3O.Umbraco.Data.ImportDataEditor` | `N3O.Umbraco.Data.ImportDataEditor` |
| Data.ImportNoticesViewer | `N3O.Umbraco.Data.ImportNoticesViewer` | `N3O.Umbraco.Data.ImportNoticesViewer` |
| Cells | `N3O.Umbraco.Cells` | `N3O.Umbraco.Cells` |
| Cropper | `N3O.Umbraco.Cropper` | `N3O.Umbraco.Cropper` |
| EditorJs | `N3O.Umbraco.EditorJs` | `N3O.Umbraco.EditorJs` |
| SerpEditor | `N3O.Umbraco.SerpEditor` | `N3O.Umbraco.SerpEditor` |
| TextResourceEditor | `N3O.Umbraco.TemplateTextEditor` (NB backend alias ≠ folder name) | `N3O.Umbraco.TemplateTextEditor` |
| Uploader | `N3O.Umbraco.Uploader` | `N3O.Umbraco.Uploader` |

(Cropper's secondary config UI keeps `N3O.PropertyEditorUi.CropperCropDefinitions` — referenced by a config
field, not a data type's editorUiAlias.) Rebuilt (0 errors), restarted, re-tested: **both Data data types now
resolve their property editor UI correctly.** Screenshots captured.

> Process note: an initial PowerShell `Set-Content -Encoding utf8` pass corrupted 4 manifests (quotes → `N`);
> detected via `node` JSON validation and rewritten cleanly. All 16 manifests re-validated as valid JSON.

### NOT yet live-rendered (needs test fixtures — no content/data types in demo DB)
The demo DB has **no content nodes** (Content tree = Recycle Bin only) and data types exist only for the two
Data editors. These still need a doctype+content (and media for some) fixture to render their Lit component in
a real workspace and exercise the Step-1 runtime risks:
- Property editors: Cells, Cropper, EditorJs, SerpEditor, TextResourceEditor, Uploader (create a data type per
  editor → add to a doctype → create content → open).
- Workspace views: Data.Import, Data.Export, Cloud.Platforms.Preview (open any document workspace → tab appears).
- blockEditorCustomView: Blocks.Preview (needs a Block Grid/List property with blocks).
- Blazor.BackOffice bundle (loads at boot — verify the jQuery-dependent loader; flagged Step 1).
- telethon-on-air-rule — BLOCKED (BLOCKER-04); placeholder only.

Registration + alias + serving + dashboards are proven; the one systematic bug found has been fixed and verified.
App left running on `https://localhost:6001` for continued testing.
