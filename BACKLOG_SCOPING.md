# Backlog Scoping — N3O.Umbraco v17 Migration

*Generated 2026-06-15. Read-only research scoping the **Backlog** section of `MIGRATION_PR_TRACKER.md`
(the "remaining migration work, pending / not yet scheduled" items). Produced by 7 parallel read-only
investigation agents over the code on `v17-Talha` + verified external licensing sources. **No code was
changed.** Each item carries a concrete scope, an achievability verdict, and what (if anything) blocks it.*

Achievability legend: ✅ Done · 🟢 Easy · 🟡 Moderate · 🔴 Hard · ⚠️ Decision-gated / external

---

## TL;DR

- **Two items to surface now (compliance / correctness):** EPPlus is asserting a **noncommercial** license inside commercial product code (#6, non-compliant as-is); and `Lazier<T>` has a **real DI scope-leak bug** (#4).
- **"Blocked by licensing" overstates two items:** MediatR (#3) is fine at the current 12.5.0 pin; EPPlus (#6) is a cheap fix either way.
- **One item is already complete:** `build/*.targets` removal (#1).
- **Largest genuine engineering lifts:** Newtonsoft→System.Text.Json (#1, ~3–5 eng-weeks) and the NSwag DTO de-dup (#5) — both gated on code-generation tooling, not hand edits.
- **Decision-gated (need product/owner input):** checkout/accounts drop (#7), EPPlus buy-vs-swap (#6), MediatR proactive-upgrade timing (#3).

### Tracker corrections found during this research
- **EPPlus version is `8.6.0`, not `8.5.4`** as the tracker states.
- **`build/*.targets` audit is already complete** — zero hand-authored targets remain.
- **MediatR is not actually "blocked by licensing"** — the pinned 12.5.0 is the last MIT/Apache version; only a v13+ upgrade would trigger the commercial license.

---

## 1. Deprecated-dependency audit (Newtonsoft + build.targets)

| Sub-item | Verdict | Detail |
|---|---|---|
| **`build/*.targets` removal** | ✅ Done | Zero hand-authored `build/*.targets`/`*.props` remain. The only `*.targets` matches are SDK auto-generated NuGet artifacts under `obj/`. RCL conversion completed this; the remaining `Forms`/`Maps.Google`/`Marketing`/`UIBuilder`/`Workflows` `.StaticAssets` are clean `Microsoft.NET.Sdk` pass-throughs. No action. |
| **Newtonsoft.Json → System.Text.Json** | 🔴 Hard (~3–5 eng-weeks) | See breakdown below. |
| **Other deprecated packages** | 🟢🟡 Easy–Moderate per item | See table below. |

### Newtonsoft.Json — scope
- **301** files with `using Newtonsoft`; **~215** files carrying `[JsonProperty]`/`JObject`/`JArray`/converters.
- **Root cause of the spread:** `IJsonProvider` (`N3O.Umbraco.Extensions/Json/JsonProvider.I.cs`) is **Newtonsoft-typed at its interface boundary** (`JsonSerializerSettings`, `JsonWriter`, `Formatting`). Every consumer of `IJsonProvider` is transitively coupled to Newtonsoft even with no direct `PackageReference`.
- **Load-bearing sites:**
  - Custom `JsonConverter`/`JsonContractResolver` subclasses in `N3O.Umbraco.Extensions/Json/` (`ByteSize`, `HtmlString`, `IPAddress`, `Lookup`, `PublishedContent`, `StorageToken`, …) — no direct STJ equivalents.
  - MVC formatters `OurJsonInputFormatter`/`OurJsonOutputFormatter` extend `Microsoft.AspNetCore.Mvc.NewtonsoftJson` types (injects Newtonsoft into the whole request/response pipeline).
  - `FlurlHttp.Clients.UseNewtonsoft()` global (`UtilitiesComposer.cs:13`) — all Flurl calls route through Newtonsoft.
  - ~8 Refit clients (TotalProcessing, Opayo, DirectDebitUK, PayPal, Bambora, Cloudflare CDN, Auth0, Currencylayer) set `NewtonsoftJsonContentSerializer`.
  - `N3O.Umbraco.Clients` — **entirely NSwag/Newtonsoft-generated** (dense `[JsonProperty]` + inline `JsonConvert`); migrating means re-generating with an STJ template.
  - `IAttributionAccessor` returns `JObject` (`AttributionCookie.cs:13`, `AttributionAccessor.cs:13`) — public API change.
  - `ImportReceiver.cs:80–113` parses webhooks via `JTokenType` — mechanical → `JsonElement.ValueKind`.
- **Important caveat:** Umbraco 17.3.5 itself ships Newtonsoft transitively (`Umbraco.Cms.Core`). It will **not** leave the dependency graph after this work — only N3O's own code stops depending on it directly.
- **Recommended phased approach:** (1) introduce an STJ-typed `IJsonProvider`; (2) ship Newtonsoft-backed + STJ-backed implementations side by side; (3) switch project-by-project; (4) regenerate NSwag clients with the STJ template; (5) drop the Newtonsoft packages.

### Other deprecated / notable packages

| Package | Project | Note |
|---|---|---|
| `Flurl.Http.Newtonsoft` 0.9.1 | Extensions | Tied to Newtonsoft migration; Flurl v4 has `UseSystemTextJson()`. |
| `NodaTime.Serialization.JsonNet` 3.2.1 | Extensions | Replace with `NodaTime.Serialization.SystemTextJson`. |
| `Refit.Newtonsoft.Json` 10.2.0 | Extensions | Refit 10 supports STJ; switch each client to `SystemTextJsonContentSerializer`. |
| `Microsoft.AspNetCore.Mvc.NewtonsoftJson` 10.0.7 | Extensions | Remove with the `OurJson*Formatter` rework. |
| `Humanizer.Core` 2.14.1 | Extensions | Pinned with a "bug in v3.0.10, wait for next release" comment — v3+ now stable; revisit. Low risk. |
| `Microsoft.CodeAnalysis.Workspaces.MSBuild` 4.14.0 | Extensions | Large Roslyn dep in a runtime lib — confirm it's actually needed. |
| `Perplex.ContentBlocks` 4.0.0-rc.3 | Blocks.Perplex | Pre-release; pin to stable when available (ties to BLOCKER-02). |
| `MailChimp.Net.V3` 5.5.0 | Newsletters.Mailchimp | Intermittently maintained; "V3" = the (still-current) Mailchimp API. Watch only. |

**No `System.Web`/Owin remnants** — the .NET 10 move is clean here.

---

## 2. Remaining Lit / legacy UIs → React

✅ **Mostly intentional; the genuine work is tiny.** No AngularJS or vanilla-JS UI remains in real source.
Five Lit surfaces exist:

| File | Type | Convert? | Verdict |
|---|---|---|---|
| `Cloud.Platforms.StaticAssets/Extensions/src/platforms-urls-info-app.ts` | `workspaceInfoApp` | **No — deliberate** | Pure Lit by design; React would nest a React-in-web-component-in-React. Leave. |
| `Cms/Extensions/N3O.Umbraco.DynamicListViews/src/dynamic-list-view.ts` | `workspaceView` | **No — deliberate** | Docs confirm DynamicListViews kept Lit on purpose (fights the WC-native backoffice). Leave. |
| `Cloud.Platforms.StaticAssets/Extensions/src/platforms-preview.ts` | React-mount shell | Optional tidy | Already mounts a React root; only the shadow-root plumbing is Lit. Low priority, non-blocking. |
| `Cloud.Platforms.Marketing.StaticAssets/.../segment-rule-telethon-on-air-display.js` | `engageSegmentRule` display | Yes (low pri) | ~10 lines. |
| `Cloud.Platforms.Marketing.StaticAssets/.../segment-rule-telethon-on-air-editor.js` | `engageSegmentRule` editor | Yes (low pri) | Small AngularJS→Lit port; ~30 lines. |

🟢 **Verdict: Easy, low value.** The only real work is the two `telethon-on-air-rule` components — **blocked on a prerequisite:** `Marketing.StaticAssets` has no `Extensions/`/Vite/TS build pipeline; stand that up first, then the rewrites are trivial.

---

## 3. Mediator upgrade / Wolverine licensing

⚠️ **"Blocked" framing is misleading — nothing is blocked today.**

- **In use:** `MediatR 12.5.0` — single `PackageReference` in `Mediator/N3O.Umbraco.Mediator/`. This is the **last MIT/Apache version**. v13.0+ went commercial (RPL-1.5 + LuckyPenny paid; community tier free for orgs under $5M revenue). **No licensing issue at the current pin.**
- **Coupling is extremely low.** A full N3O abstraction wraps MediatR; only **3 files** have `using MediatR` (`Mediator.cs`, `MediatorComposer.cs`, `ValidatorPipelineBehaviour.cs`). N3O's `IRequest<,>`/`IRequestHandler<,,>`/`IMediator` insulate **all** consumers. Usage: ~60 commands, ~20 queries, 55 handlers, 1 pipeline behavior, **0** MediatR notifications (notifications use Umbraco's own system). A swap touches **~5 files total, zero consumer changes.**
- **Options:**

  | Option | License | Swap effort | Note |
  |---|---|---|---|
  | Stay on MediatR 12.5.0 | MIT/Apache | None | Works today; no v13+ features; no future MIT upgrades. |
  | `martinothamar/Mediator` (v3.0.2) | MIT | Low–Medium | `ValueTask` vs `Task`; source-generator; registration changes. |
  | Wolverine (WolverineFx) | **MIT (core, verified)** | High | Full distributed message bus — architectural overkill, not a drop-in. |
  | Hand-rolled in-house mediator | MIT (N3O-owned) | Low | The abstraction is already so thin a simple handler-resolver could replace MediatR outright. |

- ⚠️ **Verdict: Achievable, low effort, not urgent.** Decision point is only *if/when* v13+ features are needed. Given the thin abstraction, a hand-rolled mediator is a realistic dependency-elimination option.

### Sources (verified)
- MediatR commercial launch — https://www.jimmybogard.com/automapper-and-mediatr-commercial-editions-launch-today/
- MediatR licensing update — https://www.jimmybogard.com/automapper-and-mediatr-licensing-update/
- Wolverine stays MIT (Jeremy Miller, Apr 2025) — https://jeremydmiller.com/2025/04/02/a-quick-note-about-jasperfxs-plans-for-marten-wolverine/
- JasperFx products/pricing — https://jasperfx.net/our-products/
- martinothamar/Mediator (MIT) — https://github.com/martinothamar/Mediator

---

## 4. Lifetime-scope hacks (`HttpContextAccessor` / `IUmbracoContextAccessor`)

🟡 **Mixed — prioritize by risk.** Recommended order:

| # | Site | Hack | Still needed? | Verdict | Risk |
|---|---|---|---|---|---|
| 1 | `Authentication.Auth0/.../UserDirectoryIdAccessor.cs:44` | `EnsureUmbracoContext` | Almost certainly not (`IMemberManager`/`IBackOfficeSecurityAccessor` don't need it in v17; always in an HTTP request) | 🟢 Easy | Very low |
| 2 | `Extensions/Robots/RobotsTxt.cs:32` | `EnsureUmbracoContext` | Unconfirmed (HybridCache-backed `IContentLocator` likely context-free) | 🟡 Needs runtime verification | Silent fail on robots regen |
| 3 | `Search/.../SitemapEntriesProvider.Content.cs:32` | `EnsureUmbracoContext` | Unconfirmed (but uses `IPublishedContent.AbsoluteUrl()` — see session-12 FINDING 2) | 🟡 Needs runtime verification | Silent fail on sitemap gen |
| 4 | `Scheduler/SchedulerComposer.cs:126` | `EnsureUmbracoContext` | Handler doesn't need it; mediator pipeline might | 🟡 Needs pipeline audit + startup test | Startup failure (high-visibility) |
| 5 | `Extensions/Utilities/Lazier.cs:7` | `Lazy<T>`→`Lazier<T>` global override via `CreateScope()` | Pattern needed; **scope is never disposed = real leak bug** | 🟡 Moderate | Latent resource/memory leak |
| 6 | `Validation/.../ExceptionMiddleware.cs:39` | `EnsureUmbracoContext` | Likely yes — catch-all for pre-Umbraco-middleware exceptions | 🔴 Hard | Removing can **mask the original exception** |
| — | `Monitoring.Sentry/.../SentryInitializer.cs:40,76` | ServiceProvider/`IHttpContextAccessor` stash | **Yes — Sentry SDK constraint** | Do not touch | N/A |
| — | `Extensions/Lookups/LookupsComposer.cs:30` | Lifetime TODO ("pull lifetime from interface") | Cosmetic, no runtime issue | 🟢 Easy if ever | None |

**Notes:**
- **#1** is the safe quick win.
- **#5 (`Lazier`)** is a genuine bug worth fixing regardless of migration timing: `CreateScope()` is called and the scope is never stored/disposed. Fix = switch consumers to `IServiceScopeFactory`-per-call (create + dispose), or store+dispose the scope. Requires auditing all `Lazy<T>` consumers (notably `ExceptionMiddleware`, `RegisterRecurringJobsComponent`).
- **#6** needs an architectural call (invariant-text fallback when content cache is unavailable) before the guard can go.

---

## 5. Cloud.Platforms de-duplication

🟡 **Only one genuine finding; the other five are false positives.**

| # | Finding | Real dup? | Verdict |
|---|---|---|---|
| 1 | `Clients/ContentClient.cs` + `Clients/PlatformsUmbracoClient.cs` emit **49 identical DTO types** in the same namespace `N3O.Umbraco.Cloud.Platforms.Clients` (`ContentReq`, `PublishedContent`, `ImageFormat`, `FileFormat`, etc.) | **Yes — NSwag generation problem** | 🔴 Hard |
| 2 | Hand-written `Cloud/Models/Published/*` vs generated `FeedbacksClient.cs` DTOs | No — different layers (domain-rich vs wire-format), different namespaces | N/A |
| 3 | `CdnClientExtensions` in `Cloud` vs `Cloud.Platforms` | No — same name, different methods/concerns | N/A |
| 4 | `PageViewModelExtensions` in `Cloud.Templates` vs `Cloud.Platforms.Templates` | No — additive, different layers | N/A |
| 5 | 6× `MergeModelsProvider` subclasses | No — framework pattern, unique logic each | N/A |
| 6 | `Cloud.Platforms.StaticAssets` + `Cloud.Platforms.Marketing.StaticAssets` empty RCL wrappers | No — intentional package granularity | N/A |

**Finding 1 detail:** Both files are NSwag-generated against two different API specs (Cloud Content API + Platforms Umbraco API) that share a large schema base, re-emitted into each. The build is clean today because the types are `partial` — ⚠️ **confirm how `CS0101` is being avoided before touching anything.** Fix requires NSwag config surgery: either `ExcludedTypeNames` on one spec + a shared partial file, or merge the specs at the NSwag invocation level. Must be re-applied on every regeneration → coordinate with whoever owns the NSwag step.

**Verdict: Hard, and it's maintenance-burden duplication rather than a quick edit. Everything else is intentional layering — leave alone.**

---

## 6. EPPlus license review

🔴 **Compliance issue — should not ship as-is.** (Installed version is **8.6.0**, not 8.5.4.)

- **Footprint is tiny:** 1 project (`N3O.Umbraco.Data`), **4 files** touching `OfficeOpenXml` (`DataComposer.cs`, `Services/ExcelWorkbook.cs`, `Services/ExcelWorksheetWriter.cs`, `Extensions/ExcelRangeExtensions.cs`). **Export-only** (.xlsx), multi-worksheet + table formatting + optional password encryption. All EPPlus types are hidden behind `IExcelWorkbook`/`IExcelWorkbookWriter` — none leak into public API.
- **The problem:** `DataComposer.cs:28` calls `ExcelPackage.License.SetNonCommercialOrganization("N3O")`. EPPlus's noncommercial (Polyform Noncommercial 1.0.0) license **does not cover for-profit company use** — even internal tooling. N3O is commercial → **the current assertion is non-compliant.**
- **Two routes:**

  | Route | Verdict | Effort / cost |
  |---|---|---|
  | (a) Buy commercial license | 🟢 Easy | One-line change to `SetCommercial(key)` (or `EPPlusLicense` env var); ~$329–$569/dev/yr by team size. Pure procurement. |
  | (b) Swap to ClosedXML (MIT) | 🟡 Achievable | ~1–5 days given the clean abstraction + export-only + no charts/pivots. **Verify password/encryption parity** in `ExcelWorkbook.cs`. Permanently removes the liability. |

- **Recommendation:** the swap is cheap for the footprint and removes the liability for good; buying is the trivial immediate fix if timeline dominates. **Either way, the current noncommercial assertion should not ship.**

### Sources (verified)
- License overview / pricing — https://epplussoftware.com/en/LicenseOverview/
- License FAQ (commercial use required) — https://www.epplussoftware.com/LicenseOverview/LicenseFAQ
- Community license scope — https://epplussoftware.com/en/Home/GettingStartedCommunityLicense
- Getting started / `License` API — https://github.com/EPPlusSoftware/EPPlus/wiki/Getting-Started

---

## 7. checkout / accounts deletion ("to discuss")

⚠️ **Product decision, not a migration verdict.** Five projects, not two:

| Project | Files | Referenced by (dependents) |
|---|---|---|
| `N3O.Umbraco.Accounts` | ~95 | `Giving.Checkout`, **`Payments`, `Cloud.Platforms`, `Cloud.Engage`** |
| `N3O.Umbraco.Giving.Cart` | ~50 | `Giving.Checkout` only |
| `N3O.Umbraco.Giving.Checkout` | ~90 | `Giving.Analytics` only |
| `N3O.Umbraco.Giving.Analytics` | ~5 | nothing (leaf) |
| (`N3O.Umbraco.Giving` core is referenced by Cart/Checkout but is **not** in scope to drop) | — | — |

- ✅ **`Cart` + `Checkout` + `Analytics` — clean removal as a set.** No external dependents (Analytics→Checkout→Cart self-contained). Drop 3 dirs + 3 `.sln` entries; nothing else breaks. This also removes the `Checkout→Accounts` reference, shrinking the blast radius on Accounts.
- 🔴 **`Accounts` — cascading.** Live `Payments`/`Cloud.Platforms`/`Cloud.Engage` consume `IAccount`/`IAddress`/`IConsent`. Not a clean cut — those types would need removing/inlining in each (3 `ProjectReference` lines + the code that uses them).
- Neither **DemoSite** nor **TestSite** references any of the five; none have had v17 backoffice work done; all five still carry `<Description>TODO</Description>` (untouched by the migration).

**What a human must decide:**
1. Are Checkout/Cart deployed on any live client site? (If not → low-risk drop.)
2. Is `Accounts` a live shared contract feeding the cloud integrations, or refactorable into slimmer per-project shapes?
3. Is the checkout/cart/accounts flow intended to move to the `Karakoram.*` microservice layer in v17 rather than the Umbraco package layer?

---

## Consolidated achievability matrix

| # | Backlog item | Verdict | Effort | Blocked by |
|---|---|---|---|---|
| 1a | build.targets removal | ✅ Done | — | — |
| 1b | Newtonsoft → STJ | 🔴 Hard | ~3–5 eng-weeks | NSwag regen; `IJsonProvider` API change |
| 1c | Other deprecated packages | 🟢🟡 Easy–Mod | per item | Mostly gated on 1b |
| 2 | Remaining Lit → React | 🟢 Easy / mostly deliberate | ~1 day + pipeline setup | Marketing.StaticAssets has no Vite/TS build |
| 3 | Mediator / Wolverine | ⚠️ Not urgent | ~5 files if done | Nothing today (12.5.0 is MIT) |
| 4 | Lifetime-scope hacks | 🟡 Mixed | per site | #2–4 need runtime verification; #6 needs design call |
| 5 | Cloud.Platforms dedup | 🔴 Hard (1 real finding) | NSwag config surgery | Owner of NSwag invocation |
| 6 | EPPlus license | 🔴 Compliance now | (a) 1 line / (b) 1–5 days | Buy-vs-swap decision |
| 7 | checkout/accounts drop | ⚠️ Decision-gated | Cart/Checkout/Analytics clean; Accounts cascades | Product direction |

---

*Research method: 7 parallel read-only agents (codebase + verified external licensing sources). All
licensing claims cite vendor sources; nothing was guessed. No files were modified.*
