# v17-Talha Branch Review — Findings & Action Tracker

> ⚠️ **Re-verified 2026-06-10 (session 14):** see **[`MIGRATION_AUDIT_2026-06-10.md`](MIGRATION_AUDIT_2026-06-10.md)** for current status. Corrections to this file: the "**5 CS0618**" count is actually **3** (`BlockItemDataExtensions.cs:47`, `ContentMetadataConverter.LatestState.cs:23`, `ContentTypeExtensions.cs:41`); the `BlockItemDataExtensions.FormatBlockData` ContentPicker dup-key item is best closed by **deleting** the method (zero callers); `Locator.All()` root-node change confirmed present.

*Created 2026-06-02 (session 4). Source: 14-agent deliberation review (13 Sonnet area/concern reviewers → Opus synthesis) of the full `v17-Talha` migration branch vs `main`.*

**Overall verdict: MAKE SENSE, WITH FIXES.** The Umbraco 13/.NET8 → 17.3.5/.NET10 migration is, in the large, a competent and correct adaptation — the bulk of changes are forced, well-reasoned responses to removed/renamed v17 APIs. It is **not mergeable as-is**: a small number of genuine defects crash at runtime or corrupt data, plus public-API removals that break external consuming sites.

Status legend: `[ ]` open · `[~]` in progress · `[x]` done this session · `[>]` deferred / needs decision/environment.

---

## A. Must-fix blockers

- `[x]` **BLOCKER-1 (CRITICAL) — duplicate `Umbraco.BlockList` property converter.** *(verified in code)* `PropertyConverter.NestedContent.cs:31` and `PropertyConverter.BlockList.cs:31` have byte-identical `IsConverter()` returning `Aliases.BlockList`. Two auto-discovered converters claim the same alias → `GetPropertyConverter()` throws on **every** Data import/export of a Block List property. **Fix:** delete `PropertyConverter.NestedContent.cs` (fold its `GetMaxValues` null-check into the BlockList one first if it differs).
- `[x]` **BLOCKER-2 (HIGH) — NC→BlockList migration writes invalid JSON for empty properties.** `NestedContentToBlockListMigration.cs` empty-path used anonymous-type key `@Umbraco_BlockList` (underscore) instead of dotted `"Umbraco.BlockList"`. **Fix:** emit a `JObject` with `["Umbraco.BlockList"]`.
- `[x]` **BLOCKER-3 (HIGH) — NC→BlockList migration not transactional.** Step 3 (flip editor alias) runs before Step 4 (rewrite values) with no transaction; mid-failure leaves a mixed/corrupt DB. **Fix:** wrap Steps 3+4 in `db.GetTransaction()`.
- `[x]` **BLOCKER-4 (HIGH) — `UrlInfo.AsUrl` wrong arg order in `TryGetRelocatedUrl`.** *(verified)* `UrlProvider.cs:83` & `:106` pass `(url, culture, null, false)` — culture in the *provider* slot. v17 signature is `AsUrl(url, provider, culture, isExternal)`. **Fix:** `AsUrl(url, Alias, culture, false)`.
- `[~]` **BLOCKER-5 (HIGH) — access-control regressions.** (1) Hangfire dashboard relaxed `SectionRequirement(Settings)/admin` → `RequireAuthenticatedUser()` — **RESOLVED separately** (BLOCKER-10(a): now requires `AuthorizationPolicies.SectionAccessSettings`). (2) Export/Import workspaceViews show on all docs to all users (lost `IExport/ImportContentFilter` gating) — **RESTORED + verified 2026-06-11.** (3) Platforms-Preview workspaceView shows on all doc types — **RESTORED 2026-06-11 (compile-verified; runtime pending).** For (2)/(3): v14+ removed `IContentAppFactory`, so per-node/per-user gating was lost when content apps became manifest `workspaceView`s. A shared configurable condition **`N3O.Condition.WorkspaceVisibility`** (in `N3O.Umbraco.ReactRuntime`, registered once) reads the document key and calls an authed `GET {endpoint}/{key}` → `{permitted}`: **Export** → `ExportVisibilityController` (Admin OR ExportUsers + `IExportContentFilter.IsFilter`/`AllowExports`); **Import** → `ImportVisibilityController` (Admin OR ImportUsers + `IImportContentFilter`); **Preview** → `PlatformsPreviewController` (content type composes `PlatformsConstants.Offerings.CompositionAlias`). Builds 0 errors (TestSite + Cloud.Platforms); Export/Import verified end-to-end in the browser, Preview compile-verified only. See `MIGRATION_AUDIT_2026-06-10.md` "Resolved 2026-06-11".
- `[>]` **BLOCKER-6 (HIGH) — `DataComposer.EnsureDataTypeExists` looks up by alias not name** → duplicate data types on upgrade + breaks UIBuilder import-field config. **Deferred — needs an upgrade/rename migration.**
- `[>]` **BLOCKER-7 (HIGH) — Bundling throws at render.** `IBundler`/tag-helpers throw `NotSupportedException` on any page using them. **Deferred — RR-10 delete-vs-replace decision pending.**

**Verification (2026-06-02):** all of section A blockers 1–4 + section B applied → **full solution build 0 errors; app boots clean** (no duplicate-converter / duplicate-alias / registration errors).

## B. NestedContent → BlockList replacement (DONE 2026-06-02, per instruction)

Replace local NestedContent usage with the Block List equivalent + migration where applicable. **Excluded (external API contract — must NOT change):** `Cloud.Platforms/Clients/ContentClient.cs`, `PlatformsUmbracoClient.cs`, `PropertyContentReqExtensions.cs` — generated NSwag clients whose `PropertyEditor.NestedContentMultiple/Single` mirror the remote Platforms API.

- `[x]` `Data/.../Converters/Properties/PropertyConverter.NestedContent.cs` — **deleted** (= BLOCKER-1); folded its (more correct) `GetMaxValues` Max-gated logic into `PropertyConverter.BlockList.cs` first.
- `[x]` `Data/.../Lookups/Content/PropertyType.Nested.cs` — **re-keyed** `Aliases.NestedContent` → `Aliases.BlockList` and `contentBuilder.Nested()` → `.BlockList()`. Kept lookup id `"nested"` + value/config models for schema back-compat. ⚠️ Runtime schema behaviour for Block List via this lookup needs validation with real content (couldn't exercise the Data schema API here).
- `[>]` `N3O.Umbraco.Extensions/Content/ContentHelper.cs` — `GetContentPropertiesForNestedContent[Element]` — **NOT changed: false positive.** These parse **Perplex ContentBlocks** child elements (read `ncContentTypeAlias` from Perplex's own block JSON), not Umbraco Nested Content; converting to Block List would be wrong. Flag: validate this parser against Perplex v4's actual storage format (review noted it's fragile).
- `[x]` `N3O.Umbraco.Extensions/Extensions/ContentHelperExtensions.Nested.cs` — 6 public `GetNestedContent(s)` now `[Obsolete(error:true)]` redirecting to `GetBlockList(...)` (compile-time error w/ message for consumers; no in-repo callers).
- `[x]` `N3O.Umbraco.Extensions/Extensions/ContentBuilderExtensions.cs` `Nested()` + `Content/Editor/PropertyBuilder.Nested.cs` `NestedPropertyBuilder` — `[Obsolete(error:true)]` → use `BlockList()` / `BlockListPropertyBuilder`.
- `[x]` `Giving/.../Webhooks/DonationItemReceiver.cs` — **live caller fixed**: `.Nested(PricingRules)` (would have thrown at runtime on donation webhook) → `.BlockList(PricingRules)`.
- `[x]` `N3O.Umbraco.Extensions/Migrations/NestedContentToBlockListMigration.cs` — BLOCKER-2 (empty-path dotted `Umbraco.BlockList` JObject) + BLOCKER-3 (`using var transaction` around Steps 3+4) applied. ⚠️ Per-item partial-commit + the NC→BlockList value-transform shape still need a real legacy-DB dry-run.

## C. Removed items — alternative provided? (your Q1)

- **Tier A — safe** (forced/internal/replaced): antiforgery; DynamicListViews `ContentSending`/`NodesRendering` (REST controller added); `Scheduler`/`WelcomeDashboard` IDashboard (→ Lit manifests); DataEditor/ConfigurationField attribute params; Konstrukt `GetContentSection` (fixed).
- **Tier B — alternative INCOMPLETE** (finish before merge): ~~Export/Import/Preview apps (dropped security gating)~~ **— gating RESTORED 2026-06-11 via shared `N3O.Condition.WorkspaceVisibility` (Export/Import verified; Preview compile-verified); see BLOCKER-5 above;** **`CampaignSending`/`OfferingSending` gutted to TODO stubs — embed-code + staging-URL display have NO Bellissima replacement (real functional regression)**; Telethon segment rule (descriptor not registered — BLOCKER-04); Perplex `CreateDataTypes` (silent no-op, no warning); Nested-Content helpers (only a runtime throw).
- **Tier C — NO alternative**: `PropertyTypeExtensions.IsNestedContent`; `PublishedPropertyTypeExtensions.GetNestedContentType`; `UmbracoPropertyInfo.IsNestedContent` (no `IsBlockList` counterpart — inconsistent); Auth0 `AutoRedirectLoginToExternalProvider` (UX change); `WelcomeDashboard` `Remove<ContentDashboard>` (silent no-op). `GetNestedPropertySchemaQuery/Handler` (removed this session — but always threw, low risk).

## D. Public-API / external-consumer breaks (your Q2 — shared library)

- **Compile-time breaks** for consumers: `UrlProvider` gained abstract `Alias`; `Locator` abstract contract changed; `KonstruktConfigurator`→`IConfigurator`; `IBlockPreviewer`/`DeserializeAndClean` now generic `BlockGridValue`; `IContentmentDataSource.Fields` type; removed `IsNestedContent`/`GetNestedContentType`; changed ctor signatures (ContentTypesDataSource, ContentHelper, NestedPropertyBuilder, plugin editors).
- **Runtime-only breaks (compile clean — most dangerous):** `ContentHelperExtensions.GetNestedContent(s)` (6 overloads) throw at call time; `ContentBuilderExtensions.Nested()` not even `[Obsolete]`; Bundling tag-helpers crash at render; `UmbracoPropertyInfo.IsNestedContent` removed with no `IsBlockList` replacement.
- **Action:** `[x]` added `[Obsolete(error:true)]` to the Nested-Content **helpers + builder + `Nested()` extension** (done this session — see B). `[>]` still: add `IsBlockList()` to `UmbracoPropertyInfo`; `[Obsolete(error:true)]` the remaining runtime-throw surfaces (Bundling tag-helpers); ship a consumer breaking-changes/migration guide covering all signature changes (`UrlProvider.Alias`, `Locator` abstract methods, `KonstruktConfigurator`→`IConfigurator`, `IBlockPreviewer`/`DeserializeAndClean` generics, `IContentmentDataSource.Fields`, Perplex `ContentBlocksValue`).

## E. Medium / Low

- `[>]` MED — plugin config fields (Cropper/Cells/Uploader) have no `propertyEditorSchema` manifest → data types unconfigurable in backoffice.
- `[>]` MED — `GetAwaiter().GetResult()` over async `ILanguageService` in localization accessors (deadlock risk).
- `[>]` MED — jQuery/Formstone CDN runtime dependency in Uploader/Cropper Lit (Cropper/Uploader decision).
- `[>]` MED — uSync `PublisherProcessor`: single-call completion unverified; `PublisherActionRequest.RequestId` left unset; `SyncItem.Name` empty. E2E test against a remote server.
- `[>]` MED — pre-release/unverified packages: Perplex rc.3; GMaps/Workflow/Umbraco.Code unverified; re-evaluate `ExcludeLegacyUmbracoBackOffice` strip target; Forms v17 license.
- `[>]` MED — ContentPicker duplicate-key bug in `BlockItemDataExtensions` (pre-existing; add else/early-return).
- `[>]` LOW — `Locator.All()` now includes root nodes (silent behavior change).
- `[>]` Housekeeping — commit `N3ONestedContentMigrationPlan.cs` (untracked); remove `InspectUsync` debug project + `package.manifest.bak`.
