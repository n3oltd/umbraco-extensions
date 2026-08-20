# Tech Debt & Modernization Findings

> ⚠️ **Re-verified 2026-06-10 (session 14):** see **[`MIGRATION_AUDIT_2026-06-10.md`](MIGRATION_AUDIT_2026-06-10.md)**. Stale entries to drop: the `MembersAccessControl.cs:39,55` `GetDataType(int)` row (code now uses `GetAsync(DataTypeKey)`) and the `GetPagedChildren(int)` row (now the v17 8-param overload) — both already fixed. `FormatBlockData`/`RawPropertyValues` (CS0618) is dead code → delete the method.

> **Status:** Findings only — no code was changed. Read-only 12-agent sweep of the whole `src/` tree
> (session 10), each agent owning one lens (build, deprecated APIs, dead code, dependencies, frontend,
> async/perf, security, data layer, conventions, tests/CI, "not-required-to-run", U17/.NET10
> modernization). Items already captured in `MIGRATION_PLAN.md` / `MIGRATION_BLOCKERS.md` /
> `REVIEW_FINDINGS.md` are marked **[tracked]**; everything else is new.
>
> Companion docs: [`NOT_REQUIRED_TO_RUN.md`](NOT_REQUIRED_TO_RUN.md) ·
> [`PACKAGING_RCL_RESEARCH.md`](PACKAGING_RCL_RESEARCH.md)

**Severity key:** 🔴 Critical (data loss / crash / security) · 🟠 High · 🟡 Medium · ⚪ Low/cosmetic.
"Req. to run?" = does the app currently run with this as-is (most modernization items run fine; the flag
marks correctness/security risk, not compile status).

---

## 0. Top priorities at a glance

| # | Finding | Sev | Where |
|---|---|---|---|
| 1 | **CORS wildcard** (`AllowAnyOrigin+Method+Header`) applied to the whole pipeline in all environments | 🔴 | §2 S-01 |
| 2 | **Hardcoded HMAC image secret committed to git** | 🔴 | §2 S-02 |
| 3 | **Unauthenticated file upload/download** (`StorageController`) + path not validated | 🔴 | §2 S-03 |
| 4 | **`BlockItemDataExtensions.FormatBlockData` duplicate-key crash** on any ContentPicker block — *verified* | 🔴 | §1 B-01 |
| 5 | ~~**Data export reads `element["udi"]`** (null in v17 key-based Block List) → crash on export~~ **FIXED + runtime-verified 2026-06-11** | ✅ | §1 B-02 |
| 6 | **Open redirect** in logout `returnUrl` | 🟠 | §2 S-12 |
| 7 | **Unauthenticated sync endpoint** + timing-unsafe secret compare | 🟠 | §2 S-05 |
| 8 | **NC→BlockList migration**: value-shape unverified, per-item catch hides partial commits, runs on every startup | 🔴 | §5 + §7 |
| 9 | **~15 sync-over-async sites on per-request hot paths** (block render, forex, cart/checkout, localization) | 🟠 | §3 |
| 10 | **No automated tests** anywhere + **CI pinned to .NET 8** (will fail post-merge) + `checkout@v6` (doesn't exist) | 🔴 | §9 |
| 11 | **`Blazor.BackOffice` load-check logic inverts without jQuery** → Blazor script never injected | 🟠 | §6 F-03 |
| 12 | **Hardcoded local `.NET 5.0.17` HintPath** in `N3O.Umbraco.Clients.csproj` → builds fail off the author's machine | 🟠 | §4 |

> **Cleared during this audit (do NOT spend time on):** the shared React runtime was flagged as a possible
> silent crash (`react-dom/client` mapping, missing vite configs). **Verified false** — `vite.config.react.ts`
> and `vite.config.rest.ts` exist, and `src/N3O.Umbraco.ReactRuntime/frontend/react-runtime/src/react-dom.js`
> re-exports `createRoot`/`hydrateRoot`
> from `react-dom/client`, so the import map (`react-dom/client → react-dom.js`) resolves correctly. The
> in-browser end-to-end smoke test is still outstanding, but the build/runtime wiring is sound.

---

## 1. Confirmed bugs & correctness risks (new)

| ID | Finding | Sev | Req. to run? | Location |
|---|---|---|---|---|
| **B-01** | **ContentPicker duplicate-key crash — *verified*.** When the picker value parses as a GUID, line 23 adds `propertyData.Alias` then line 26 unconditionally adds the same alias again → `ArgumentException: An item with the same key has already been added`. Crashes the block-preview/format path for any block with a ContentPicker property holding a valid GUID. Missing `else`/`continue` after line 23. **[tracked in REVIEW_FINDINGS, but not in main trackers]** | 🔴 | Yes | `Blocks/N3O.Umbraco.Blocks/Extensions/BlockItemDataExtensions.cs:20-26` |
| **B-02** | ✅ **FIXED + runtime-verified (2026-06-11).** `GetContentPropertiesForBlockListOrGrid` now iterates the v17 flat `contentData` array directly; new `GetBlockElementKey` reads element `"key"` (legacy `udi` fallback) + `GetBlockElementValuesByAlias` reads the v17 `"values"` array. Verified end-to-end on the test site via the Management API AND the backoffice UI (CSV contained the block's inner value; export completed, no crash). *(Was: read v13 `element["udi"]`, dead `if (block is JArray)` branch never matched flat v17 `contentData`.)* | ✅ | — | `N3O.Umbraco.Extensions/Content/ContentHelper.cs` |
| **B-03** | **Perplex block parse uses v13 shape.** `GetContentPropertiesForBlockContent` parses Perplex `blocks[i]["content"]` as old NC JSON; Perplex v4 (`Perplex.ContentBlocks 4.0.0-rc.3`) uses `ContentBlocksValue`. Likely NRE / empty results on any Perplex+Data-export site. | 🟠 | Yes (Perplex + export) | `ContentHelper.cs:181-199` |
| **B-04** | **`BlockValueExtensions` static content-type cache never invalidated.** `static readonly ConcurrentHashSet<IContentType>` is populated once and never cleared on `ContentTypeSaved/Deleted` → stale type definitions in block rendering after backoffice edits (especially risky mid-migration). Also a non-atomic check-then-populate (redundant `GetAllElementTypes()` under concurrency). | 🟡 | Yes | `Blocks/N3O.Umbraco.Blocks/Extensions/BlockValueExtensions.cs:16,95-103` |
| **B-05** | **`DateTime.Now` in telethon campaign gate.** Local-time compare against a stored (UTC-migrated) `Telethon.BeginAt` → off-by-timezone on UTC servers. Use `DateTime.UtcNow`. | 🟡 | Yes | `Cloud/N3O.Umbraco.Cloud.Platforms/Notifications/Offerings/OfferingSaving.cs:32` |
| **B-06** | **`Repository.UpdateAsync` has no optimistic-concurrency check** (TODO at line 96). Concurrent edits of the same entity silently lose a write. | 🟡 | Yes | `N3O.Umbraco.Extensions/Entities/Repository.cs:96` |
| **B-07** | **`CartValidator.IsValid` → `catch { return false; }`.** Any exception (DB down, NRE) is reported as "invalid cart", masking real failures as an empty cart. | 🟡 | Yes | `Giving/N3O.Umbraco.Giving.Cart/Services/CartValidator.cs:28` |
| **B-08** | **NC migration per-item `catch { }`** inside the shared transaction logs+counts but does not abort → data-type can be flipped to Block List while some property values remain NC JSON (partial migration, no hard failure). | 🔴 | No (only sites with NC data) | `N3O.Umbraco.Extensions/Migrations/NestedContentToBlockListMigration.cs:143-161,181` |

**~37 bare `catch { }` blocks** swallow exceptions across the codebase (CSV parsing, localization, Auth0 user-sync, mapping, blob resolution). Most are control-flow fallbacks; the highest-risk are B-07, B-08, and the localization accessors (§3). Recommend `catch (Exception ex) { _logger.LogWarning(ex, …); }` at minimum.

---

## 2. Security & configuration (new unless noted)

> Hangfire dashboard auth and Export/Import gating are **[tracked + resolved]** (BLOCKER-10 #1/#2). The
> Platforms-Preview content-type condition (#3) is now **[resolved 2026-06-11]** (shared
> `N3O.Condition.WorkspaceVisibility` condition + `PlatformsPreviewController`; compile-verified, runtime
> pending). The per-node/per-user content-app gating for Export/Import (REVIEW_FINDINGS BLOCKER-5) was
> also restored via the same condition (verified end-to-end).

| ID | Finding | Sev | Location |
|---|---|---|---|
| **S-01** | **CORS wildcard pipeline-wide, all environments** — `AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()`. Every public endpoint (payments, giving, newsletters, storage, webhooks, sync, lookups) accepts any origin. Undermines the `[Authorize]` fixes. Restrict to a configurable allow-list. | 🔴 | `N3O.Umbraco.Extensions/Hosting/HostingComposer.cs:111-114` |
| **S-02** | **Hardcoded Imaging `HMACSecretKey` in committed `appsettings.json`** — forge signed media URLs if reused in prod. Rotate; move to env/Key Vault/user-secrets. | 🔴 | `DemoSite/DemoSite.Web/appsettings.json:140` |
| **S-03** | **`StorageController` unauthenticated** (`ApiController`, no `[Authorize]`). `POST tempUpload` (1 GB), `POST upload/{folderPath}`, `GET download/{folderName}/{filename}` all anonymous; `folderName`/`folderPath` not sanitized (traversal). Add backoffice auth + path validation. | 🔴 | `Storage/N3O.Umbraco.Storage/Controllers/StorageController.cs` |
| **S-05** | **`SyncExtensionsController.SyncData` unauthenticated** (`ApiController`) and the shared-secret compare is plain `!=` (timing attack). Use backoffice auth + `CryptographicOperations.FixedTimeEquals`. | 🟠 | `Sync/N3O.Umbraco.Sync.Extensions/Controllers/SyncExtensionsController.cs`, `Handlers/SyncDataHandler.cs:27` |
| **S-06** | **Webhook receiver has no framework-level signature verification** — any caller who knows/guesses a `hookId` can submit payloads; verification is left to each `IWebhookReceiver`. Add a mandatory `VerifySignature` step + rate limiting. | 🟡 | `Webhooks/N3O.Umbraco.Webhooks/Controllers/WebhooksController.cs` |
| **S-07** | **Stack trace leaked in 400 responses** (`JobProxyController` returns `ex.StackTrace`). Log server-side instead. | 🟡 | `Scheduler/N3O.Umbraco.Scheduler/Controller/JobProxyController.cs:43` |
| **S-08** | **`JobProxyController` custom `X-Api-Key` is a process-lifetime `Guid.NewGuid()`** — mismatches across instances/restarts, no TLS enforcement at this layer, no Umbraco auth. Use a config/Key Vault secret + framework auth. | 🟡 | `JobProxyController.cs:47-55`, `Services/TriggerKey.cs:10` |
| **S-09** | **`DevToolsController.GetConfiguration` exposes full `IConfiguration` debug view** unless env is exactly `Development` — leaks all secrets if env is `Staging`/`Test`. Gate harder / require auth / strip from release. | 🟡 | `N3O.Umbraco.Extensions/Hosting/Controllers/DevToolsController.cs:27-33` |
| **S-12** | **Open redirect** — logout `Redirect(returnUrl)` not validated as local. Use `LocalRedirect` / `Url.IsLocalUrl()`. | 🟠 | `Authentication/N3O.Umbraco.Authentication/Controllers/AuthenticationController.cs:40` |
| **S-13** | **Block preview HTML via `dangerouslySetInnerHTML`** without sanitization (replaced the old `<iframe>`) — stored XSS in backoffice if any block renders untrusted data. Sanitize server-side or restore iframe isolation. **[tracked as a flag]** | 🟡 | server `Blocks/…/Controllers/BlockPreviewBackofficeController.cs`; client `Blocks/…StaticAssets/ClientApp/src/block-preview-app.tsx:20` |
| **S-14** | **`ConnectMiddleware` SSRF risk** — `cdnPath` derived from request URL; the `..` guard misses URL-encoded variants and `//host` injection; no auth. Strict allow-list the path. | 🟡 | `Cloud/N3O.Umbraco.Cloud/Hosting/ConnectMiddleware.cs:28-30` + `Services/Cdn/CdnClient.cs` |
| **S-15** | **`PlatformsTemplatesMiddleware`** serves static files with a `!Contains("..")` check that isn't URL-decoded first (`%2E%2E` bypass). Decode before checking. | ⚪ | `Cloud/N3O.Umbraco.Cloud.Platforms/Hosting/Middleware/PlatformsTemplatesMiddleware.cs` |
| **S-16/17** | **`StagingMiddleware`** basic-auth uses plain `==` (timing) on a password stored in an Umbraco content node (readable by any editor); brute-force lockout is per-process `MemoryCache` (bypassed across instances). Use `FixedTimeEquals` + env/Key Vault secret + distributed counter. | ⚪ | `N3O.Umbraco.Extensions/Hosting/Middleware/StagingMiddleware.cs:114-115` |
| **S-18** | **No security response headers** (only HSTS in prod) — no CSP, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`. Compounds S-13. Add a header middleware. | ⚪ | `N3O.Umbraco.Cms/CmsStartup.cs:53-55` |
| **S-19** | **No request rate limiting** on public payment/giving/newsletter/webhook/upload endpoints (the merged "rate limiting" was Sentry event throttling only). Add ASP.NET Core `AddRateLimiter`. | 🟡 | `N3O.Umbraco.Cms/CmsStartup.cs` |
| **S-10** | **`TelemetryController` version endpoints anonymous** — version fingerprinting. Add auth or non-prod-only. | ⚪ | `Telemetry/N3O.Umbraco.Telemetry/Controllers/TelemetryController.cs` |
| **S-21** | **`Auth0M2MTokenAccessor` cache key embeds `ClientSecret` in plaintext** — leaks if the key is ever logged. Hash it (the `clientId+apiIdentifier` is already unique). | ⚪ | `Authentication/N3O.Umbraco.Authentication.Auth0/Services/Auth0M2MTokenAccessor.cs:21` |

**Confirmed non-issues:** Key Vault uses `DefaultAzureCredential` (no hardcoded creds); Sentry DSN is config-bound with `SendDefaultPii=false`; cookies are `HttpOnly`+`Secure` (cart/attribution cookies intentionally `HttpOnly=false`); antiforgery is native v17 now. Public payment controllers being on `ApiController` (no `[Authorize]`) is by design (donor-facing flows keyed by opaque entity IDs) — flagged for docs, not a defect.

---

## 3. Async / sync-over-async / performance (new unless noted)

**~15 `GetAwaiter().GetResult()` sites on per-request hot paths.** Each blocks a thread-pool thread on real
I/O and risks starvation/deadlock under load. The localization accessors are also wrapped in `catch { }`, so
a deadlock would surface as a silent fallback to the default culture.

| Area | Sites | Sev | Location |
|---|---|---|---|
| Block view-model rendering | `blockPipeline.RunAsync(...).GetAwaiter().GetResult()` per block per page | 🟠 | `Blocks/…/Services/BlockViewModelFactory.cs:35`, `Blocks/N3O.Umbraco.Blocks.Perplex/Services/PerplexBlockViewModelFactory.cs:32` |
| Localization settings (per request) | `ILanguageService.GetAllAsync/GetDefaultLanguageAsync` blocked; also an **unsynchronized lazy-init race** that can double-download from CDN | 🟠 | `Cloud/N3O.Umbraco.Cloud/Services/PublishedLocalizationSettingsAccessor.cs:28,43,44`; `N3O.Umbraco.Extensions/Localization/LocalizationSettingsAccessor.Environment.cs:27-28`; `Extensions/LocalizationServiceExtensions.cs:11-26` **[tracked MED]** |
| Lookups sync wrappers | 6 wrappers used across rendering (currency cookie, pricing, forex, visibility) | 🟠 | `N3O.Umbraco.Extensions/Lookups/Lookups.cs:51-80` |
| Forex / pricing / cart / checkout (per request) | `BaseToQuoteForexConverter.Convert`, `PriceCalculator.InCurrency`, `CartAccessor.Get`, `CheckoutAccessor.Get` | 🟠 | respective `Services/*.cs` |
| Content visibility / page mode (per content node) | `PlatformsPageContentVisibilityFilter.IsVisible` (CDN call), `PageModeAccessor.GetPageMode` (iterates `CanEditAsync`) | 🟠 | `Cloud/…/Services/PlatformsPageContentVisibilityFilter.cs:26`, `N3O.Umbraco.Extensions/Content/Editor/PageModeAccessor.cs:16` |
| Mappings (CDN download in sync map methods) | `CampaignContent.PopulateContributionInfo`, Create/Update`CampaignReqMapping`, `DonationFormStateReqMapping` | 🟡 | `Cloud/N3O.Umbraco.Cloud.Platforms/…` |
| FluentValidation blocking | `UKBankAccountReqValidator.AccountIsValid` blocks on `IsValidAsync` — use `MustAsync` | 🟡 | `Payments/N3O.Umbraco.Payments.DirectDebitUK/Models/UKBankAccountReq/UKBankAccountReqValidator.cs:56` |
| Email send | `AmazonSender.Send` wraps SES `SendAsync` (FluentEmail sync `ISender`) | 🟡 | `Email/N3O.Umbraco.Email.Amazon/Services/AmazonSender.cs:33-35` |
| Import parsing / blob | `BlobParser.TryParse`, `AngleSharp.OpenAsync` in `HtmlToText`/EditorJs | 🟡 | `Data/…/Parsing/Blob/BlobParser.cs:31`, `Search/…/HtmlToText.cs`, `Plugins/EditorJs/…/StringExtensions.cs` |
| Startup (low risk) | `AzureStartupStorage.GetStorageFolder`, `DevToolsController.Echo` | ⚪ | — |

**Other performance findings:**

| ID | Finding | Sev | Location |
|---|---|---|---|
| P-01 | **Payment-provider composers read `IContentCache` at singleton-DI-build time** (6 providers) — if first resolution happens before the published cache is populated, the API client is registered as `null` and never re-read (same class of bug as BLOCKER-11). Gate on `IRuntimeState.Run` or resolve lazily. | 🟠 | `Payments/*/…Composer.cs` (Bambora/GoCardless/Opayo/PayPal/Stripe/TotalProcessing) |
| P-02 | **`new HttpClient()` per call** in `UrlBlobResolver` (per resolve), `ClientFactory.Create` (per API call), payment composer factories — socket exhaustion / no DNS-TTL refresh. Use `IHttpClientFactory`. | 🟠 | `Data/…/Parsing/Blob/Resolvers/BlobResolver.Url.cs:19`, `Cloud/…/Clients/ClientFactory.cs:59`, payment composers |
| P-03 | **Unbounded static caches** — `Auth0M2MTokenAccessor` tokens, `StagingMiddleware.FailedLogins`, `CdnClient.Downloads`, `ImagePublisher` image cache. Add `SizeLimit` or use bounded cache. | 🟡 | respective files |
| P-04 | **Reflection on hot paths** — `OurAssemblies.GetTypes` per upsell calc (`UpsellOfferContentExtensions.cs:111`), `TypeResolver.Resolve` fallback scan per Hangfire job deserialization, `StaticLookups.GetAll<T>` scan per property lookup. Cache the resolved type in a static `ConcurrentDictionary`. | 🟡 | `Giving/…/UpsellOfferContentExtensions.cs:111`, `Extensions/Types/TypeResolver.cs:44`, `Lookups/StaticLookups.cs:36` |
| P-05 | **Unbounded in-memory accumulation** — `ProcessExportHandler` builds the whole export in a `MemoryStream`; `ContentHelper.GetAllPagedContent`/`GetDescendantsForContentOfType` load all pages into a `List`. Stream/cap for large trees. | ⚪ | `Data/…/ProcessExportHandler.cs`, `ContentHelper.cs:144` |
| P-06 | **`LookupsCollection.EnsureLoadedAsync` reload race** (no lock) — duplicate DB/CDN loads at reload boundaries (benign result, wasteful). | ⚪ | `Extensions/Lookups/LookupsCollection.cs:62-70` |

---

## 4. Build, packaging & dependencies (new unless noted)

> The per-plugin `build/*.targets` mechanism and the RCL alternative have their own document:
> **[`PACKAGING_RCL_RESEARCH.md`](PACKAGING_RCL_RESEARCH.md)**.

| ID | Finding | Sev | Location |
|---|---|---|---|
| **D-01** | **Hardcoded `.NET 5.0.17` `<HintPath>`** to a local `Program Files\dotnet\shared\…` path — fails on every other machine/CI. A correct `Microsoft.Extensions.Http 10.0.8` `PackageReference` already exists in the same file; delete the `<Reference>` block. | 🟠 | `N3O.Umbraco.Clients/N3O.Umbraco.Clients.csproj:45-48` |
| **D-02** | ✅ **RESOLVED (frontend/build seam).** A root **`Directory.Build.props`** now exists (sets `N3OHasFrontend`, `N3OFrontendWorkspace=$(…)src`, `StaticWebAssetBasePath=/`, excludes node_modules/dist/frontend). *(Central Package Management — no `Directory.Packages.props` — is still NOT adopted; the per-project package-version pinning aspect remains as a separate backend consideration.)* | ✅ | root `Directory.Build.props` |
| **D-03** | **Version placeholder `17.0.0`** on 114 packable projects (not CalVer); `<Description>TODO</Description>` on all; 6 projects have no `<Version>` (would publish `1.0.0`); 1 stale `<RepositoryUrl>` (`umbraco-extensions`). Stamp CalVer at pack time via CI. **[BLOCKER-09 tracked]** | 🟡 | all `*.csproj` |
| **D-04** | ✅ **RESOLVED.** The copy-pasted per-project `BuildClientApp`/`BuildReactRuntime` targets are gone; build is now centralized in the root **`Directory.Build.targets`** (`RestoreFrontendDependencies` → `npm ci` once in `src` + stamp; `BuildFrontendWorkspace` → single `npx turbo run build --env-mode=loose`; `BuildFrontend` → per-project copy of `frontend/*/dist/**` into `wwwroot/App_Plugins/...`). All gated on `N3OHasFrontend`. The old `!Exists(node_modules)` clean-install hazard is gone (`npm ci` runs once with a `.n3o-frontend-restored` stamp). | ✅ | root `Directory.Build.targets` |
| **D-05** | **Known-vulnerable / stale packages.** Build emits NU1902/NU1903 (MailKit, AngleSharp, `System.Security.Cryptography.Xml`, `Microsoft.Build.Tasks.Core`). `Markdig 1.1.3` doesn't exist on nuget.org (real latest `0.40.x`) — likely resolving to a very old build. `Flurl.Http.Newtonsoft 0.9.1` stale. OTel + Auth0 intra-family version skew (1.11.1 vs 1.11.2; 7.45.1 vs 7.46.0). | 🟡 | various `*.csproj` |
| **D-06** | **`Microsoft.CodeAnalysis.Workspaces.MSBuild 4.14.0` appears unused** (no call sites) — a ~30 MB Roslyn transitive tree and the likely reason `NU1605` is blanket-suppressed in 118 projects. Confirm with `dotnet nuget why`; removing it may let the suppression go too. | 🟡 | `N3O.Umbraco.Extensions.csproj` |
| **D-07** | **Commercial-license packages** — `EPPlus 8.5.4` (Polyform NonCommercial; needs a license for commercial/SaaS use) and `handsontable 12.3.0` (commercial v12+; free CE caps at v6). Confirm licenses or migrate (ClosedXML/NPOI; AG-Grid/TanStack). | 🟡 | `Data/N3O.Umbraco.Data.csproj`, `Cells.StaticAssets/frontend/<app>/package.json` |
| **D-08** | **No committed `packages.lock.json`** (NuGet) — enable `RestoreLockedMode` on CI to catch transitive drift. npm lockfiles ARE committed (good). | ⚪ | repo-wide |
| **D-09** | **Legacy csproj noise** — explicit `Debug|AnyCPU`/`Release|AnyCPU` `<DebugType>` groups (SDK defaults already match) in all projects; `NU1504` suppressed where CPM isn't even enabled. | ⚪ | all `*.csproj` |

---

## 5. Data layer / content & DB migration (new unless noted)

| ID | Finding | Sev | Location |
|---|---|---|---|
| **DL-01** | **NC→BlockList value-shape unverified vs v17.** Transform copies NC properties *flat* onto `contentData` rather than into the v17 `"values"` sub-key; items whose alias has no content-type match are **silently dropped** (`continue`, no warning). Dry-run on a real legacy DB before any live upgrade; log skipped aliases. **[BLOCKER-06 tracked, deepened]** | 🔴 | `NestedContentToBlockListMigration.cs:171-236,206` |
| **DL-02** | **Migration runs on every startup** as an auto-discovered `PackageMigrationPlan` (state-checked from DB each boot). Recommend an explicit opt-in flag + a backup→verify runbook rather than auto-run. (See also B-08 partial-commit risk.) | 🔴 | `N3ONestedContentMigrationPlan.cs`, `NestedContentToBlockListMigration.cs` |
| **DL-03** | **Doc mismatch:** trackers say the plan is "auto-discovered via `IDiscoverable`"; actually it's Umbraco's core `PackageMigrationPlan` discovery (not the N3O `IDiscoverable` convention). Add a startup log line confirming the plan ran; correct the doc. | 🟡 | — |
| **DL-04** | **`ImportQueue.CommitAsync` inserts a batch with no transaction** — partial commit on mid-loop failure. Wrap in `db.GetTransaction()` / Umbraco scope. | 🟡 | `Data/N3O.Umbraco.Data/Services/ImportQueue.cs:141-157` |
| **DL-05** | **`ProcessImportHandler` mixes raw NPoco `db` and `IContentService` on the same connection scope** — contention/deadlock risk under load; per-row status updates are intentionally incremental (document the partial-state behavior). | 🟡 | `Data/…/Handlers/Imports/ProcessImportHandler.cs:75-107` |
| **DL-06** | **`Repository<T>` SQL uses string interpolation** (`WHERE Type='{type}'`, `Id` value) — safe today (Guid + reflection-derived names) but should be parameterized; `SELECT *` loads the full `Json` column with no paging on `GetAllAsync`; deserializes each row twice. | 🟡 | `N3O.Umbraco.Extensions/Entities/Repository.cs:35,43,50-52,61` |
| **DL-07** | **`RawPropertyValues` write is obsolete** (CS0618) — v17 reads `BlockItemData.Values` (typed `BlockPropertyValue`). May not round-trip correctly depending on whether the setter syncs both. **[tracked CS0618]** (same file as B-01.) | 🟡 | `BlockItemDataExtensions.cs:47` |
| **DL-08** | **`PerplexBlockTypesService.CreateTypesAsync` silently skips data-type creation** (TODO: "removed in v17") — new Perplex block definitions get a content type but no data type, with no error. **[BLOCKER-02 tracked]** | 🟡 | `Blocks/N3O.Umbraco.Blocks.Perplex/Services/PerplexBlockTypesService.cs:35-37` |
| **DL-09** | **`DataComponent.EnsureDataTypeExistsAsync` doesn't set `EditorUiAlias`** → empty property-editor picker in the v17 data-type screen (editor still works). **[BLOCKER-11 follow-up]** | ⚪ | `Data/N3O.Umbraco.Data/DataComposer.cs:157-158` |
| **DL-10** | **`ImportsMigrationsComponent` plan name = table name** (`n3oImports`) — not namespaced; collision-prone as a `umbracoKeyValue` state key. Prefix with `N3O.Umbraco.Data.`. | ⚪ | `Data/…/UIBuilder/Imports/Migrations/ImportsMigrationsComponent.cs:32` |
| **DL-11** | **uSync Publisher `SyncContentHandler`** reflection-based; `Process` completion semantics unverified (could return before the pipeline finishes); `SyncItem.Name=""` hurts log correlation. Needs E2E with a real Publisher server. **[BLOCKER-05 caveat]** | 🟡 | `Sync/…/SyncContentHandler.cs:35-50` |

---

## 6. Frontend / ClientApp / React / Vite (new unless noted)

> Cleared: the shared React-runtime build is sound (see §0 note).

| ID | Finding | Sev | Location |
|---|---|---|---|
| **F-03** | **`Blazor.BackOffice` load check inverts without jQuery.** `$('script').filter(...)` on an empty jQuery set → `length===0`, so `0 !== 1` is true → `blazorIsLoaded()` returns "loaded" when it isn't → Blazor script never injected. Two-line native-DOM fix is documented in the file header but not applied. | 🟠 | `Blazor/N3O.Umbraco.Blazor.BackOffice/frontend/<app>/src/N3O.Umbraco.Blazor.BackOffice.ts:67-70` |
| **F-01/02** | **jQuery + Formstone (Cropper/Uploader).** Uploader fetches `code.jquery.com` jQuery 3.7.1 at runtime (CSP/offline risk); Formstone needs global jQuery, which v17 backoffice doesn't ship → upload silently no-ops. **[tracked — pending native-picker decision]** | 🟠 | `Plugins/Uploader/.../uploader.ts:108`, `Plugins/{Uploader,Cropper}/.../formstone/*` |
| **F-05** | **EditorJs media/link picker result shapes unverified vs v17** (`selection[0].url`, `result.link.url/unique` cast as-is) — image/link insertion likely yields `undefined`. Needs a live test (EditorJs isn't referenced by DemoSite). **[tracked]** | 🟡 | `Plugins/EditorJs/.../editor-js-app.tsx:196-229` |
| **F-06** | **Platforms-Preview reads non-existent `contentTypeAlias` off `UmbDocumentDetailModel`** → `previewHtml/undefined` API call. Read `contentType.alias` from the workspace context. **[BLOCKER-10 #3]** | 🟡 | `Cloud/…/frontend/<app>/src/platforms-preview-app.tsx:61-68` |
| **F-08** | **`setInterval` leak in Platforms-Preview** — inner 2 s interval created per `loadPreview`, never cleared. | ⚪ | `platforms-preview-app.tsx:132` |
| **F-24** | **EditorJs `sanitizer` config marked "Not working"** (TODO) — potential XSS via `raw`/`embed` blocks for untrusted editors. **Investigated (2026-06-16):** the global `sanitizer` option in EditorJS v2.x (`editorjs.umd.js` `ca.save()`) is **not applied during save()**; save() calls `dt(blocks, block => tool.sanitizeConfig)` — only each tool's `static sanitize` property is used. The global option only affects certain internal paste/clipboard flows. Fix: add `static get sanitize()` to each tool class that emits HTML (`UmbracoImageTool`, `EmbedWithUI`, built-in `Header`/`Paragraph`/`Quote`/`List`/`CodeTool`/`RawTool`). `UmbracoLinkTool` already returns `{ a: { href: true } }`. The TODO comment in editor-js-app.tsx now documents this finding. | 🟡 | `Plugins/EditorJs/.../editor-js-app.tsx` (sanitizer config); tool files `src/tools/` |
| **F-09** | **Committed Vite build outputs** under the per-plugin `<Project>/frontend/<app>/dist/` folders (not gitignored like Cms/Forms/Sync). See `NOT_REQUIRED_TO_RUN.md` Category 2. | 🟡 | repo-wide |
| **F-13/22** | ✅ **RESOLVED.** The copy-pasted `tsconfig.json`/`vite.config.ts` are gone — config is centralized in the shared **`@repo/build-config`** package (`src/frontend/build-config`): consumers extend the tsconfig **by name** (`./tsconfig`, `./tsconfig-react`) and call `n3oPluginConfig()` for Vite. Combined with the single npm + Turborepo workspace rooted at `src/` (root manifest `@n3oltd/client-root`), shared deps are hoisted and version-pinned once (root `overrides` + `syncpack`), so a version bump is one edit. | ✅ | `src/frontend/build-config` (`@repo/build-config`) + `src/` workspace root |
| **F-14** | **React listed under `dependencies` not `devDependencies`** in 9 React-shell plugins (it's external/build-only via the import map) — misleading for any future publish. | ⚪ | React-shell `package.json` files |
| **F-15/16/21** | **Asset/style inconsistencies** — Cropper loads cropperjs CSS by hardcoded `/App_Plugins/...` path (could `?inline`-import like Cells/Handsontable); inline hex colors instead of `--uui-*` tokens (won't theme/dark-mode). | ⚪ | Cropper/Uploader/Blocks/WelcomeDashboard `*.tsx`/`*.ts` |
| **F-20** | **`document.execCommand('copy')`** (deprecated) in Uploader; Cropper already uses `navigator.clipboard`. One-line fix. | ⚪ | `Plugins/Uploader/.../uploader.ts:249-251` |
| **F-17** | **5 plugins never runtime-tested** (not referenced by DemoSite): EditorJs, Cells, Blocks.Preview, Cloud.Platforms.Preview, Blazor.BackOffice — F-03/05/06/08/24 can't surface until they're added + exercised with fixtures. **[tracked]** | 🟡 | DemoSite.Web.csproj |

---

## 7. Code conventions & architecture (new)

- **AutoMapper: none found** — the codebase correctly uses `IMapDefinition`/`*Mapping` (87 files). ✅
- **`async void`: none.** ✅ · **File-scoped namespaces: adopted everywhere.** ✅ · **No God classes** (the only huge files are NSwag-generated clients).
- **One-type-per-file violations** (~16): composer+component bundled (`DataComposer.cs`, `PerplexBlocksComposer.cs`); the lookup+collection-in-one-file pattern is pervasive and inconsistent (either bless it as a convention or split); the worst are `Data/Attributes/CollectionAttribute.cs` (17 types) and `ValueAttribute.cs` (15 types).
- **Interfaces not in `*.I.cs`** (7 files): `ParseResult.cs`, `SpecialContent.cs`, `TemplateStyle.cs`, `WebhookEvent.cs`, `PageViewModel.cs`, `BlockViewModel.cs`, `PerplexBlockViewModel.cs`.
- **~150 bare `throw new Exception(...)`** as the universal error idiom (prefer `InvalidOperationException`/`ArgumentException`/domain types where callers must distinguish — e.g. Auth0 and webhook auth failures).
- **`throw new NotImplementedException()` in live code:** `ProfanityGuard.Add` (never called), `DataTypeParser.cs:50,54` (crashes if a subclass doesn't override — medium).
- **NRT not enabled** anywhere (consistent absence; high-value once the migration stabilizes).
- **Magic strings:** hardcoded `"editor"` Auth0 group alias (was `Constants.Security.EditorGroupAlias`, removed v17 — confirm the v17 alias) **[tracked]**; Contentment/Perplex package aliases as string literals; an internal Contentment type name resolved by reflection (`DataPickerValueConverter`) that may have moved in 6.1.4.
- **`TODO Migration Review` markers: 29 across 19 files** — categories: removed-API stubs (8), CS0618 deprecated (6), v17-replacement stubs/regressions (5: CampaignSending/OfferingSending/Bundler), Nested Content (4), v17 adaptation/magic-string (3), health-check (1), antiforgery (1), Hangfire (1). Plus ~8 standalone `// TODO` (notably `Repository.cs:96` concurrency, `CartBlockModule.cs:81` "Fix this").

---

## 8. Deprecated / obsolete API usage (CS0618 & sync-removed)

> The 5 CS0618 sites are **[tracked]**. Async-component/migration-base conversions are **done & verified**.

| API | Removal | Sites | Note |
|---|---|---|---|
| `IDataTypeService.GetDataType(int)` | U18 | `Data/.../ContentTypeExtensions.cs:41`, `Extensions/.../MembersAccessControl.cs:39,55` | Fixable via `IPropertyType.DataTypeKey` (Guid) + `GetAsync(key)`; the iterator case needs de-iterating. |
| `IContentService.GetPagedChildren(int,…)` | U19 | `ContentHelper.cs:60` | New overload needs `propertyAliases`/`loadTemplates`. |
| `IAuditService.GetLogs(int)` | U19 | `ContentMetadataConverter.LatestState.cs:23` | Async replacement requires making `IContentMetadataConverter.GetValue` async (ripples to 11 subclasses). |
| `BlockItemData.RawPropertyValues` (write) | TBD | `BlockItemDataExtensions.cs:47` | See DL-07. |
| `TypeResolver` pins assembly version `1.0.0.0` | — | `Extensions/Types/TypeResolver.cs` | Stored type names embed `1.0.0.0`; packages now build `17.0.0`. **Verify DB-stored type names still resolve** (Entities table, Scheduler TriggerKey). |
| `IContentService.GetById(int)` / `GetAncestors` / `GetRootContent` | not removed in v17 | many | v13-style; prefer Guid keys + `IDocumentNavigationQueryService`. Low urgency. |

---

## 9. Tests, CI/CD & DevOps (new)

| ID | Finding | Sev | Location |
|---|---|---|---|
| **T-01** | **Zero test projects** in the entire 120-project solution. "Green CI" = "it compiles". The riskiest paths (NC→BlockList transform, block converters, mappers, access-control filters) have no safety net — the session-5 duplicate-converter bug was caught by review, not a test. Start `N3O.Umbraco.Tests` (xUnit) covering at minimum the NC value transform and `PropertyConverter.BlockList`. | 🔴 | repo-wide |
| **T-02** | **`main-ci.yml` and `tag-ci.yml` pin .NET 8** — the solution targets `net10.0`; both **will fail** once v17 merges to `main`. `tag-ci.yml` also sets `run-tests: false` (publishes to nuget.org with no test gate). | 🔴 | `.github/workflows/{main,tag}-ci.yml` |
| **T-03** | **`actions/checkout@v6` doesn't exist** (latest v4) → `tag-ci.yml` job fails to resolve the action. | 🟠 | `.github/workflows/tag-ci.yml:27` |
| **T-04** | **`v17-ci.yml` triggers on branch `v17`, not `v17-Talha`** — all migration work has had **no CI** (no build gate, no MyGet publish). Add `v17-Talha`. | 🟠 | `.github/workflows/v17-ci.yml:5` |
| **T-05** | **No `actions/setup-node` in any workflow** — the 13 `BuildClientApp` targets (+ React runtime) run `npm ci`/`npm run build` during `dotnet build`. Unless the shared `n3oltd/actions` workflow installs Node, the build fails ("npm not found"). No `.nvmrc`/`engines` pins Node either. Verify against the reusable action. | 🔴 | `.github/workflows/*` |
| **T-06** | **Dependabot covers only NuGet** — 13 npm `ClientApp`s (Vite, React, `@umbraco-cms/backoffice`) get no update/security PRs. Add npm ecosystems (with `groups:` to limit PR volume). | 🟡 | `.github/dependabot.yml` |
| **T-07** | **`HomepageWarmup` uses `new HttpClient()`** and only handles 200/404 (other statuses retry to a 90 s timeout, then flips ready anyway). Use `IHttpClientFactory`. | 🟡 | `N3O.Umbraco.Extensions/Hosting/HomepageWarmup.cs:47-51` |
| **T-08** | **No README in most packable projects** though `<PackageReadmeFile>README.md</PackageReadmeFile>` is declared → `dotnet pack` will warn/fail. npm clients publish with `license: UNLICENSED`, empty repo URL, `description: TODO`. `umbraco-engage-client` has no publish job. | ⚪ | repo-wide |
| **T-09** | **`SentryInitializer` initializes the SDK in `IHostedService.StartAsync`** — startup exceptions before that aren't captured. Verify `UseSentry()` is wired at host-builder level. | ⚪ | `Monitoring/N3O.Umbraco.Monitoring.Sentry/Hosting/SentryInitializer.cs` |

---

## 10. U17 / .NET 10 modernization opportunities (the app runs — these are "better approaches")

| ID | Current | Modern v17 / .NET 10 | Value / effort |
|---|---|---|---|
| M-01 | `PluginController : UmbracoAuthorizedController` (v8-era `/umbraco/backoffice/api`) | **Management API** (`UmbracoManagementApiControllerBase`, `/management/api/v1`, OpenAPI, OpenIddict) | 🟠 High / Medium — adopt for new endpoints; route may break in v18. |
| M-02 | `IContentService` sync create/save/publish (~50 sites, 17 files) — sync surface slated for U18/19 removal | **`IContentEditingService` / `IContentPublishingService`** (async, key-based); `IDocumentNavigationQueryService` for traversal | 🟠 High / High — prioritize `ContentPublisher`, `StringLocalizer.ReadWrite`, `ImportQueue`. |
| M-04 | Newtonsoft.Json everywhere via a custom `IJsonProvider` + 30+ converters (~250 files) | **`System.Text.Json`** + source generators (AOT-safe) | 🟡 Med / Very high — incremental; new endpoints first. |
| M-05 | Hangfire for all background/recurring jobs (extra SQL schema, dashboard, jQuery) | **`IRecurringTask`** (U15+, `Umbraco.Cms.Infrastructure.BackgroundJobs`) for simple recurring work; keep Hangfire for queue/retry/deferred semantics | 🟡 Med / Very high — hybrid. |
| M-16 | Custom `N3O.Umbraco.Webhooks` (~47 files: storage, queue, dispatch, retry) | **Built-in Umbraco Webhooks** (U14+, `IWebhookService`, `WebhookEvent`, dashboard) | 🟡 Med / Medium — removes ~47 files if domain events map to Umbraco's contract (verify HMAC/retry parity). |
| M-06 | Custom `IContentCache` (`ConcurrentDictionary` over `IPublishedContentCache`, 107 files) | Platform `IPublishedContentCache` (typed) / Delivery API / .NET 9 `HybridCache` | ⚪ Low / Medium — mature; main risk is missed flush notifications (B-04). |
| M-11 | `DateTime.Now/UtcNow` in 7 files alongside NodaTime `IClock` (~20 files) | Consistent **`IClock`** or **`TimeProvider`** (.NET 8+, testable) | ⚪ Low / Low. |
| M-14 | No primary constructors; `new List<>()` over collection expressions; no `required` | C# 12 primary ctors, `[…]` collection expressions, `required` members | ⚪ Low / high-volume, cosmetic. |
| M-15 | `IHttpClientFactory` used well in most places; a few `new HttpClient()` leaks (P-02) | Add **`Microsoft.Extensions.Http.Resilience`** (Polly) on payment/CDN clients | 🟡 Med / Low — retry/circuit-breaker. |

**Sources consulted (v17 capabilities):** Umbraco docs — Management API, Content Editing/Publishing services (U14), `IRecurringTask`/`BackgroundJobs` (U15+), built-in Webhooks (U14+), `IDocumentNavigationQueryService` (already partially adopted here); .NET 10 `TimeProvider`, `System.Text.Json` source generators, `HybridCache`.

---

## How to act on this

Nothing here is committed. Suggested triage order: **§0 top-12 first** (security S-01/02/03, the two confirmed
crashes B-01/B-02, CI .NET-8 pin T-02), then the NC-migration safety work (DL-01/02, B-08) before any live-site
upgrade, then the sync-over-async hot paths (§3), then dependency hygiene (§4 D-01/D-02), then modernization
(§10) as capacity allows. The `git grep "TODO Migration Review"` convention (see `MIGRATION_PLAN.md`) already
tags 29 of these in-source.
